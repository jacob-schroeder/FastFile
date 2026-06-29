using FastFile.Models.Assets;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets;

public sealed class XAssetListReader
{
    private const int XAssetListSize = 0x10;
    private const int XAssetSize = 0x08;

    public XAssetListSnapshot Read(FastFileCursor cursor, FastFileLoadContext context)
    {
        int rootOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, XAssetListSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        int scriptStringCount = rootCursor.ReadInt32();
        XPointerReference scriptStringsReference = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        int assetCount = rootCursor.ReadInt32();
        XPointerReference assetsReference = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);

        if (rootCursor.Offset != XAssetListSize)
            throw new InvalidDataException($"XAssetList root consumed 0x{rootCursor.Offset:X} bytes instead of 0x{XAssetListSize:X}.");

        context.Blocks.Push(XFileBlockType.LARGE);
        IReadOnlyList<XScriptStringEntry> scriptStrings;
        IReadOnlyList<XAssetEntry> assets;

        try
        {
            scriptStrings = !context.PointerReader.HasInlinePayload(scriptStringsReference)
                ? ValidateSkippedScriptStringArray(scriptStringsReference, scriptStringCount, context)
                : ReadScriptStrings(cursor, scriptStringsReference, scriptStringCount, context);

            assets = !context.PointerReader.HasInlinePayload(assetsReference)
                ? ValidateSkippedAssetArray(assetsReference, assetCount, context)
                : ReadAssets(cursor, assetsReference, assetCount, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        var snapshot = new XAssetListSnapshot(
            SerializedOffset: rootOffset,
            ScriptStringCount: scriptStringCount,
            ScriptStringsPointer: scriptStringsReference.AsPointer<XPointer<string>[]>(),
            ScriptStrings: scriptStrings,
            AssetCount: assetCount,
            AssetsPointer: assetsReference.AsPointer<XAssetEntry[]>(),
            Assets: assets);

        foreach (XAssetEntry asset in assets)
            context.Assets.Add(new XAsset
            {
                Type = asset.Type,
                Asset = asset.AssetPointer
            });

        return snapshot;
    }

    private static IReadOnlyList<XScriptStringEntry> ReadScriptStrings(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative script string count {count}.");

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        int pointerTableSourceOffset = cursor.Offset;
        byte[] pointerTable = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress pointerTableAddress);
        var tableCursor = new FastFileCursor(pointerTable, pointerTableAddress);

        var pointerOffsets = new int[count];
        var pointers = new XPointerReference[count];

        for (int i = 0; i < count; i++)
        {
            pointerOffsets[i] = pointerTableSourceOffset + i * sizeof(int);
            pointers[i] = context.PointerReader.ReadCell(tableCursor, XPointerOffsetMode.Direct);
        }

        var entries = new XScriptStringEntry[count];
        for (int i = 0; i < entries.Length; i++)
        {
            string? value = context.PointerReader.LoadXString(cursor, pointers[i]);
            XBlockAddress pointerCell = pointers[i].CellAddress
                ?? throw new InvalidDataException($"Script string pointer {i} has no destination cell address.");

            entries[i] = new XScriptStringEntry(i, pointerOffsets[i], pointerCell, pointers[i].AsPointer<string>(), value);
        }

        return entries;
    }

    private static IReadOnlyList<XAssetEntry> ReadAssets(
        FastFileCursor cursor,
        XPointerReference assetsPointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative asset count {count}.");

        context.PointerReader.PatchInlinePointerCell(assetsPointer, alignment: 4);
        int assetTableSourceOffset = cursor.Offset;
        byte[] assetTable = context.Blocks.Load(cursor, checked(count * XAssetSize), out XBlockAddress assetTableAddress);
        var tableCursor = new FastFileCursor(assetTable, assetTableAddress);

        var assets = new XAssetEntry[count];
        for (int i = 0; i < assets.Length; i++)
        {
            int offset = assetTableSourceOffset + i * XAssetSize;
            int rowStart = tableCursor.Offset;
            var type = (XAssetType)tableCursor.ReadInt32();
            XPointerReference pointer = context.PointerReader.ReadCell(tableCursor, XPointerOffsetMode.AliasCell);

            if (tableCursor.Offset - rowStart != XAssetSize)
                throw new InvalidDataException($"XAsset row consumed 0x{tableCursor.Offset - rowStart:X} bytes instead of 0x{XAssetSize:X}.");

            XBlockAddress assetPointerCell = pointer.CellAddress
                ?? throw new InvalidDataException($"XAsset row {i} pointer has no destination cell address.");
            assets[i] = new XAssetEntry(i, offset, assetPointerCell, type, pointer.AsPointer<BaseAsset>());
        }

        return assets;
    }

    private static IReadOnlyList<XScriptStringEntry> ValidateSkippedScriptStringArray(
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative script string count {count}.");

        context.PointerReader.ValidateOffsetPointerRange<XPointer<string>[]>(pointer, checked(count * sizeof(int)), "XAssetList.scriptStrings");
        return [];
    }

    private static IReadOnlyList<XAssetEntry> ValidateSkippedAssetArray(
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative asset count {count}.");

        context.PointerReader.ValidateOffsetPointerRange<XAssetEntry[]>(pointer, checked(count * XAssetSize), "XAsset[]");
        return [];
    }
}
