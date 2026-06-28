using FastFile.Loaders.Assets.Material;
using FastFile.Models.Assets.Font;
using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Font;

public sealed class FontLoader
{
    private readonly MaterialLoader _materialLoader = new();

    public FontAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level Font pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            FontAsset font = ReadFont(cursor, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return font;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private FontAsset ReadFont(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, FontAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XString namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        int pixelHeight = rootCursor.ReadInt32();
        int glyphCount = rootCursor.ReadInt32();
        XPointer<MaterialAsset> materialPointer = context.PointerReader.ReadPointer<MaterialAsset>(rootCursor, XPointerResolutionMode.AliasCell);
        XPointer<MaterialAsset> glowMaterialPointer = context.PointerReader.ReadPointer<MaterialAsset>(rootCursor, XPointerResolutionMode.AliasCell);
        XPointer<FontGlyph[]> glyphsPointer = context.PointerReader.ReadPointer<FontGlyph[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != FontAsset.SerializedSize)
            throw new InvalidDataException($"Font consumed 0x{rootCursor.Offset:X} bytes instead of 0x{FontAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  Font root source=0x{offset:X} name=0x{namePointer.Raw:X8} pixelHeight={pixelHeight} glyphCount={glyphCount} " +
            $"material=0x{materialPointer.Raw:X8} glowMaterial=0x{glowMaterialPointer.Raw:X8} glyphs=0x{glyphsPointer.Raw:X8} " +
            $"blocks={context.Blocks.DescribePositions()}");

        string? name;
        MaterialAsset? material;
        MaterialAsset? glowMaterial;
        IReadOnlyList<FontGlyph> glyphs;

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            material = ReadMaterialPointer(cursor, materialPointer.Untyped, context);
            glowMaterial = ReadMaterialPointer(cursor, glowMaterialPointer.Untyped, context);
            glyphs = ReadGlyphArray(cursor, glyphsPointer.Untyped, glyphCount, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new FontAsset
        {
            Offset = offset,
            NamePointer = namePointer,
            Name = name,
            PixelHeight = pixelHeight,
            GlyphCount = glyphCount,
            MaterialPointer = materialPointer,
            Material = material,
            GlowMaterialPointer = glowMaterialPointer,
            GlowMaterial = glowMaterial,
            GlyphsPointer = glyphsPointer,
            Glyphs = glyphs
        };
    }

    private MaterialAsset? ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MaterialAsset.SerializedSize, "Font Material");
            return null;
        }

        return _materialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private static IReadOnlyList<FontGlyph> ReadGlyphArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int glyphCount,
        FastFileLoadContext context)
    {
        if (glyphCount < 0)
            throw new InvalidDataException($"Invalid negative Font glyph count {glyphCount}.");

        int byteCount = checked(glyphCount * FontAsset.GlyphSerializedSize);
        if (pointer.Type == PointerType.Null)
            return [];

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "FontGlyph[]");
            return [];
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return [];

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress glyphAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] glyphBytes = context.Blocks.Load(cursor, byteCount);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(glyphAddress));

        var glyphCursor = new FastFileCursor(glyphBytes, glyphAddress);
        var glyphs = new FontGlyph[glyphCount];
        for (int i = 0; i < glyphs.Length; i++)
            glyphs[i] = ReadGlyph(glyphCursor);

        context.Diagnostics.Trace(
            $"    Font glyphs sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} count={glyphCount} target={glyphAddress} " +
            $"blocks={context.Blocks.DescribePositions()}");

        return glyphs;
    }

    private static FontGlyph ReadGlyph(FastFileCursor cursor)
    {
        int start = cursor.Offset;
        var glyph = new FontGlyph(
            cursor.ReadUInt16(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor));

        if (cursor.Offset - start != FontAsset.GlyphSerializedSize)
            throw new InvalidDataException($"FontGlyph consumed 0x{cursor.Offset - start:X} bytes instead of 0x{FontAsset.GlyphSerializedSize:X}.");

        return glyph;
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }
}
