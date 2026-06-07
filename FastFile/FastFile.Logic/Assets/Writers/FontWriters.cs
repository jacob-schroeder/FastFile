using FastFile.Models.Assets.Fonts;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private static void WriteFontAsset(XFileWriterContext context, FontAsset asset)
    {
        context.WritePointerRaw(asset.NamePtr, PointerResolutionKind.Direct, "Font.Name");
        context.WriteInt32(asset.PixelHeight);
        context.WriteInt32(asset.GlyphCount);
        context.WritePointerRaw(asset.Material, PointerResolutionKind.Alias, "Font.Material");
        context.WritePointerRaw(asset.GlowMaterial, PointerResolutionKind.Alias, "Font.GlowMaterial");
        context.WritePointerRaw(asset.Glyphs, PointerResolutionKind.Direct, "Font.Glyphs");

        WriteQueuedLargeString(context, asset.NamePtr);
        WriteQueuedMaterial(context, asset.Material);
        WriteQueuedMaterial(context, asset.GlowMaterial);
        WriteQueuedFontGlyphs(context, asset.Glyphs, asset.GlyphCount);
    }

    private static void WriteQueuedFontGlyphs(
        XFileWriterContext context,
        ZonePointer<FontGlyph[]>? pointer,
        int glyphCount)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedFontGlyphs(context, pointer, glyphCount)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var count = Math.Max(0, glyphCount);
        for (var i = 0; i < count; i++)
            WriteFontGlyph(context, i < pointer.Result.Length ? pointer.Result[i] : null);
    }

    private static void WriteFontGlyph(XFileWriterContext context, FontGlyph? glyph)
    {
        context.WriteUInt16(glyph?.Letter ?? 0);
        context.WriteByte(glyph?.X0 ?? 0);
        context.WriteByte(glyph?.Y0 ?? 0);
        context.WriteByte(glyph?.Dx ?? 0);
        context.WriteByte(glyph?.PixelWidth ?? 0);
        context.WriteByte(glyph?.PixelHeight ?? 0);
        context.WriteByte(glyph?.Padding ?? 0);
        context.WriteFloat(glyph?.S0 ?? 0.0f);
        context.WriteFloat(glyph?.T0 ?? 0.0f);
        context.WriteFloat(glyph?.S1 ?? 0.0f);
        context.WriteFloat(glyph?.T1 ?? 0.0f);
    }
}
