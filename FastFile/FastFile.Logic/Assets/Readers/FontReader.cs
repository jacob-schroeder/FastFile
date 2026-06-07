using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class FontReader
{
    public static FontAsset Read(ref XFileReadContext context)
    {
        var asset = new FontAsset
        {
            Offset = context.Position,
            NamePtr = context.ReadDirectPointer<string>("Font.Name"),
            PixelHeight = context.ReadInt32(),
            GlyphCount = context.ReadInt32(),
            Material = MaterialReader.ReadMaterialPointerField(ref context),
            GlowMaterial = MaterialReader.ReadMaterialPointerField(ref context),
        };

        asset.Glyphs = context.ReadDirectPointer<FontGlyph[]>("Font.Glyphs");

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);
        MaterialReader.ResolveMaterialPointer(ref context, asset.Material);
        MaterialReader.ResolveMaterialPointer(ref context, asset.GlowMaterial);
        context.ResolvePointerInBlock(asset.Glyphs, XFILE_BLOCK.LARGE, (ref XFileReadContext pointerContext, ZonePointer<FontGlyph[]> pointer) =>
        {
            var glyphs = new FontGlyph[Math.Max(0, asset.GlyphCount)];
            for (var i = 0; i < glyphs.Length; i++)
                glyphs[i] = ReadGlyph(ref pointerContext);

            pointer.SetResult(glyphs);
        });

        return asset;
    }

    private static FontGlyph ReadGlyph(ref XFileReadContext context)
    {
        return new FontGlyph
        {
            Letter = context.ReadUInt16(),
            X0 = context.ReadByte(),
            Y0 = context.ReadByte(),
            Dx = context.ReadByte(),
            PixelWidth = context.ReadByte(),
            PixelHeight = context.ReadByte(),
            Padding = context.ReadByte(),
            S0 = context.ReadFloat(),
            T0 = context.ReadFloat(),
            S1 = context.ReadFloat(),
            T1 = context.ReadFloat(),
        };
    }
}
