using FastFile.Models.Assets.GameMap;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.GameMap;

public sealed class GameWorldMpLoader
{
    public GameWorldMpAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level GameWorldMp pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            GameWorldMpAsset gameWorld = ReadGameWorldMp(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return gameWorld;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static GameWorldMpAsset ReadGameWorldMp(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, GameWorldMpAsset.SerializedSize, out XBlockAddress rootAddress, "GameWorldMp");
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"GameWorldMp pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        XPointer<GGlassData> glassDataPointer = ReadNonNullInlineCell<GGlassData>(rootCursor);

        if (rootCursor.Offset != GameWorldMpAsset.SerializedSize)
            throw new InvalidDataException($"GameWorldMp consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GameWorldMpAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  GameWorldMp.header source=0x{sourceOffset:X}..0x{sourceOffset + GameWorldMpAsset.SerializedSize:X} " +
            $"root={rootAddress} name=0x{namePointer.Raw:X8}/{namePointer.Untyped.Type} " +
            $"glassData=0x{glassDataPointer.Raw:X8}/{glassDataPointer.Untyped.Type} bytes={PreviewBytes(rootBytes, 16)}");

        string? name;
        GGlassData? glassData;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            glassData = ReadGlassData(cursor, glassDataPointer.Untyped, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  GameWorldMp root source=0x{sourceOffset:X} name={name ?? "<null>"} " +
            $"glassData=0x{glassDataPointer.Raw:X8} pieces={glassData?.GlassPieces.Count ?? 0} " +
            $"glassNames={glassData?.GlassNames.Count ?? 0} blocks={context.Blocks.DescribePositions()}");

        return new GameWorldMpAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            GlassDataPointer = glassDataPointer,
            GlassData = glassData
        };
    }

    private static GGlassData? ReadGlassData(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Raw == 0)
            return null;

        int sourceOffset = cursor.Offset;
        XBlockAddress glassDataAddress = PatchNonNullInlineCell(pointer, alignment: 4, context, "GameWorldMp.glassData");
        byte[] rootBytes = context.Blocks.Load(cursor, GGlassData.SerializedSize, out XBlockAddress loadedAddress, "G_GlassData");
        if (loadedAddress != glassDataAddress)
            throw new InvalidDataException($"G_GlassData pointer patched to {glassDataAddress}, but root loaded at {loadedAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, glassDataAddress);
        XPointer<GGlassPiece[]> glassPiecesPointer = ReadNonNullInlineCell<GGlassPiece[]>(rootCursor);
        int pieceCount = rootCursor.ReadInt32();
        ushort damageToWeaken = rootCursor.ReadUInt16();
        ushort damageToDestroy = rootCursor.ReadUInt16();
        int glassNameCount = rootCursor.ReadInt32();
        XPointer<GGlassName[]> glassNamesPointer = ReadNonNullInlineCell<GGlassName[]>(rootCursor);
        byte[] pad14To7F = rootCursor.ReadBytes(0x6C);

        if (rootCursor.Offset != GGlassData.SerializedSize)
            throw new InvalidDataException($"G_GlassData consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GGlassData.SerializedSize:X}.");

        ValidateCount(pieceCount, "G_GlassData.pieceCount");
        ValidateCount(glassNameCount, "G_GlassData.glassNameCount");

        context.Diagnostics.Trace(
            $"    G_GlassData.header source=0x{sourceOffset:X}..0x{sourceOffset + GGlassData.SerializedSize:X} " +
            $"root={glassDataAddress} pieces=0x{glassPiecesPointer.Raw:X8}/{glassPiecesPointer.Untyped.Type} count={pieceCount} " +
            $"damageToWeaken={damageToWeaken} damageToDestroy={damageToDestroy} " +
            $"glassNames=0x{glassNamesPointer.Raw:X8}/{glassNamesPointer.Untyped.Type} count={glassNameCount} " +
            $"pad14To7F={PreviewBytes(pad14To7F, 32)} bytes={PreviewBytes(rootBytes, 32)}");

        IReadOnlyList<GGlassPiece> glassPieces = ReadGlassPieces(
            cursor,
            glassPiecesPointer.Untyped,
            pieceCount,
            context);
        IReadOnlyList<GGlassName> glassNames = ReadGlassNames(
            cursor,
            glassNamesPointer.Untyped,
            glassNameCount,
            context);

        return new GGlassData
        {
            Offset = glassDataAddress.Offset,
            GlassPiecesPointer = glassPiecesPointer,
            GlassPieces = glassPieces,
            PieceCount = pieceCount,
            DamageToWeaken = damageToWeaken,
            DamageToDestroy = damageToDestroy,
            GlassNameCount = glassNameCount,
            GlassNamesPointer = glassNamesPointer,
            GlassNames = glassNames,
            Pad14To7F = pad14To7F
        };
    }

    private static IReadOnlyList<GGlassPiece> ReadGlassPieces(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, GGlassPiece.SerializedSize, 4, context, "G_GlassData.glassPieces", out XBlockAddress piecesAddress);
        var rowCursor = new FastFileCursor(bytes, piecesAddress);
        var pieces = new GGlassPiece[count];
        for (int i = 0; i < pieces.Length; i++)
        {
            int rowOffset = rowCursor.Offset;
            XBlockAddress rowAddress = piecesAddress.Add(rowOffset);
            pieces[i] = new GGlassPiece
            {
                Offset = rowAddress.Offset,
                DamageTaken = rowCursor.ReadUInt16(),
                CollapseTime = rowCursor.ReadUInt16(),
                LastStateChangeTime = rowCursor.ReadInt32(),
                PackedImpactDir = rowCursor.ReadUInt16(),
                PackedImpactPos = rowCursor.ReadUInt16()
            };
        }

        return pieces;
    }

    private static IReadOnlyList<GGlassName> ReadGlassNames(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, GGlassName.SerializedSize, 4, context, "G_GlassData.glassNames", out XBlockAddress namesAddress);
        var rowCursor = new FastFileCursor(bytes, namesAddress);
        var names = new GGlassName[count];
        for (int i = 0; i < names.Length; i++)
        {
            int rowOffset = rowCursor.Offset;
            XBlockAddress rowAddress = namesAddress.Add(rowOffset);
            XPointer<string> nameStrPointer = context.PointerReader.ReadPointer<string>(rowCursor, XPointerResolutionMode.Direct);
            ushort name = rowCursor.ReadUInt16();
            ushort pieceCount = rowCursor.ReadUInt16();
            XPointer<ushort[]> pieceIndicesPointer = ReadNonNullInlineCell<ushort[]>(rowCursor);
            string? nameStr = context.PointerReader.LoadXString(cursor, nameStrPointer);
            IReadOnlyList<ushort> pieceIndices = ReadUInt16Array(
                cursor,
                pieceIndicesPointer.Untyped,
                pieceCount,
                2,
                context,
                $"G_GlassData.glassNames[{i}].pieceIndices");

            if (i < 4 || i == names.Length - 1)
            {
                context.Diagnostics.Trace(
                    $"      G_GlassName[{i}] row={rowAddress} nameStr=0x{nameStrPointer.Raw:X8}/{nameStrPointer.Untyped.Type} " +
                    $"name={name} pieceCount={pieceCount} pieceIndices=0x{pieceIndicesPointer.Raw:X8}/{pieceIndicesPointer.Untyped.Type} " +
                    $"value={nameStr ?? "<null>"}");
            }

            names[i] = new GGlassName
            {
                Offset = rowAddress.Offset,
                NameStrPointer = nameStrPointer,
                NameStr = nameStr,
                Name = name,
                PieceCount = pieceCount,
                PieceIndicesPointer = pieceIndicesPointer,
                PieceIndices = pieceIndices
            };
        }

        return names;
    }

    private static IReadOnlyList<ushort> ReadUInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, sizeof(ushort), alignment, context, memberName);
        var valueCursor = new FastFileCursor(bytes);
        var values = new ushort[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = valueCursor.ReadUInt16();

        return values;
    }

    private static byte[] LoadInlineArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int stride,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        return LoadInlineArray(cursor, pointer, count, stride, alignment, context, memberName, out _);
    }

    private static byte[] LoadInlineArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int stride,
        int alignment,
        FastFileLoadContext context,
        string memberName,
        out XBlockAddress targetAddress)
    {
        ValidateCount(count, memberName);

        if (pointer.Raw == 0)
        {
            if (count != 0)
                throw new InvalidDataException($"{memberName} is null with non-zero count {count}.");

            targetAddress = context.Blocks.CurrentAddress;
            return [];
        }

        int byteCount = checked(count * stride);
        int sourceStart = cursor.Offset;
        targetAddress = PatchNonNullInlineCell(pointer, alignment, context, memberName);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress loadedAddress, memberName);
        if (loadedAddress != targetAddress)
            throw new InvalidDataException($"{memberName} pointer patched to {targetAddress}, but payload loaded at {loadedAddress}.");

        context.Diagnostics.Trace(
            $"      {memberName}.load source=0x{sourceStart:X}..0x{cursor.Offset:X} " +
            $"ptr=0x{pointer.Raw:X8}/{pointer.Type} count={count} stride=0x{stride:X} align={alignment} " +
            $"bytes=0x{byteCount:X} target={targetAddress} preview={PreviewBytes(bytes, 32)}");
        return bytes;
    }

    private static XPointer<T> ReadNonNullInlineCell<T>(FastFileCursor cursor)
    {
        int cellOffset = cursor.Offset;
        int raw = cursor.ReadInt32();
        return new XPointer<T>(raw, XPointerResolutionMode.Direct, cursor.AddressAt(cellOffset));
    }

    private static XBlockAddress PatchNonNullInlineCell(
        XPointerReference pointer,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        if (pointer.Raw == 0)
            throw new InvalidDataException($"{memberName} is null and cannot be patched.");

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"{memberName} pointer 0x{pointer.Raw:X8} has no destination cell address to patch.");

        if (alignment > 0)
            context.Blocks.AlignCurrent(alignment);

        XBlockAddress targetAddress = context.Blocks.CurrentAddress;
        context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(targetAddress));
        return targetAddress;
    }

    private static void ValidateCount(int count, string memberName)
    {
        if (count < 0 || count > 0x100000)
            throw new InvalidDataException($"{memberName} has invalid count {count}.");
    }

    private static string PreviewBytes(ReadOnlySpan<byte> bytes, int maxBytes)
    {
        if (bytes.IsEmpty)
            return "<empty>";

        int headCount = Math.Min(bytes.Length, maxBytes);
        string head = Convert.ToHexString(bytes[..headCount]);
        if (bytes.Length <= maxBytes)
            return head;

        int tailCount = Math.Min(bytes.Length - headCount, maxBytes);
        string tail = Convert.ToHexString(bytes[^tailCount..]);
        return $"{head}...{tail}";
    }

}
