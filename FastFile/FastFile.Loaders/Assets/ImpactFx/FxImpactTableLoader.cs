using FastFile.Loaders.Assets.Fx;
using FastFile.Models.Assets.Fx;
using FastFile.Models.Assets.ImpactFx;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.ImpactFx;

public sealed class FxImpactTableLoader
{
    private readonly FxEffectDefLoader _fxLoader = new();

    public FxImpactTableAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level ImpactFx pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            FxImpactTableAsset table = ReadFxImpactTable(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return table;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private FxImpactTableAsset ReadFxImpactTable(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, FxImpactTableAsset.SerializedSize, out XBlockAddress rootAddress);
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"ImpactFx pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XString namePointer = ReadXStringPointer(rootCursor);
        XPointer<FxImpactEntry[]> entriesPointer = ReadPointer<FxImpactEntry[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != FxImpactTableAsset.SerializedSize)
            throw new InvalidDataException($"ImpactFx root consumed 0x{rootCursor.Offset:X} bytes instead of 0x{FxImpactTableAsset.SerializedSize:X}.");

        string? name;
        IReadOnlyList<FxImpactEntry> entries;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            entries = ReadFxImpactEntryArray(cursor, entriesPointer.Untyped, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  ImpactFx root source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} entries=0x{entriesPointer.Raw:X8} " +
            $"loadedEntries={entries.Count} blocks={context.Blocks.DescribePositions()}");

        return new FxImpactTableAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            EntriesPointer = entriesPointer,
            Entries = entries
        };
    }

    private IReadOnlyList<FxImpactEntry> ReadFxImpactEntryArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return [];

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"ImpactFx entries pointer 0x{pointer.Raw:X8} has no destination cell address.");

        context.Blocks.AlignCurrent(4);
        XBlockAddress entriesAddress = context.Blocks.CurrentAddress;
        context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(entriesAddress));
        byte[] entryBytes = context.Blocks.Load(
            cursor,
            checked(FxImpactTableAsset.EntryCount * FxImpactEntry.SerializedSize));

        var entries = new FxImpactEntry[FxImpactTableAsset.EntryCount];
        var entryCursor = new FastFileCursor(entryBytes, entriesAddress);
        for (int i = 0; i < entries.Length; i++)
        {
            int entryOffset = entryCursor.Offset;
            XBlockAddress entryAddress = entriesAddress.Add(entryOffset);
            IReadOnlyList<XPointer<FxEffectDefAsset>> surfacePointers = ReadFxEffectDefPointerBand(
                entryCursor,
                FxImpactEntry.SurfaceEffectCount);
            IReadOnlyList<XPointer<FxEffectDefAsset>> fleshPointers = ReadFxEffectDefPointerBand(
                entryCursor,
                FxImpactEntry.FleshEffectCount);

            if (entryCursor.Offset - entryOffset != FxImpactEntry.SerializedSize)
                throw new InvalidDataException($"FxImpactEntry consumed 0x{entryCursor.Offset - entryOffset:X} bytes instead of 0x{FxImpactEntry.SerializedSize:X}.");

            IReadOnlyList<FxEffectDefAsset?> surfaceEffects = ReadFxEffectDefPointers(cursor, surfacePointers, context);
            IReadOnlyList<FxEffectDefAsset?> fleshEffects = ReadFxEffectDefPointers(cursor, fleshPointers, context);

            entries[i] = new FxImpactEntry
            {
                Offset = entryAddress.Offset,
                SurfaceEffectPointers = surfacePointers,
                SurfaceEffects = surfaceEffects,
                FleshEffectPointers = fleshPointers,
                FleshEffects = fleshEffects
            };
        }

        context.Diagnostics.Trace(
            $"    ImpactFx entries sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} target={entriesAddress} " +
            $"count={entries.Length} blocks={context.Blocks.DescribePositions()}");

        return entries;
    }

    private static IReadOnlyList<XPointer<FxEffectDefAsset>> ReadFxEffectDefPointerBand(
        FastFileCursor cursor,
        int count)
    {
        var pointers = new XPointer<FxEffectDefAsset>[count];
        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadPointer<FxEffectDefAsset>(cursor, XPointerResolutionMode.AliasCell);

        return pointers;
    }

    private IReadOnlyList<FxEffectDefAsset?> ReadFxEffectDefPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<FxEffectDefAsset>> pointers,
        FastFileLoadContext context)
    {
        var effects = new FxEffectDefAsset?[pointers.Count];
        for (int i = 0; i < effects.Length; i++)
            effects[i] = _fxLoader.LoadFromPointer(cursor, pointers[i].Untyped, context);

        return effects;
    }

    private static XString ReadXStringPointer(FastFileCursor cursor)
    {
        return ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        XPointerResolutionMode mode)
    {
        int cellOffset = cursor.Offset;
        return new XPointer<T>(cursor.ReadInt32(), mode, cursor.AddressAt(cellOffset));
    }
}
