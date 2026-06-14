using FastFile.Models.Assets.Fonts;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

public sealed class FontAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute FontMaterialWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute FontGlyphArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FontAsset.GlyphCount)
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        if (value is not FontAsset font)
            return false;

        Load_Font(font, context);
        return true;
    }

    private static void Load_Font(
        FontAsset font,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(font, nameof(FontAsset.NamePtr));
            Load_FontMaterialPtr(font.Material, context, font);
            Load_FontMaterialPtr(font.GlowMaterial, context, font);
            context.ResolvePointerValue(font.Glyphs, FontGlyphArrayAttribute, font);
        });
    }

    private static void Load_FontMaterialPtr(
        XPointer<MaterialAsset> pointer,
        IXAssetReaderContext context,
        FontAsset owner)
    {
        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(pointer, FontMaterialWrapperAttribute, owner);
        });
    }
}
