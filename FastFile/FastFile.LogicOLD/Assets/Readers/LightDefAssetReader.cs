using FastFile.ModelsOLD.Assets.GfxLightDef;
using FastFile.ModelsOLD.Assets.Material;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.LogicOLD.Assets.Readers;

public sealed class LightDefAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute CStringAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString
    };

    private static readonly XPointerFieldAttribute GfxImageWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        if (value is not GfxLightDef light)
            return false;

        Load_GfxLightDef(light, context);
        return true;
    }

    // PS3 0x1089a0 body: Load_Stream 0x10, PushStreamPos(4),
    // Load_XString at +0x00, then Load_GfxImagePtr at +0x04.
    private static void Load_GfxLightDef(
        GfxLightDef light,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerValue(light.NamePtr, CStringAttribute, light);

            if (light.Image.IsNull)
                return;

            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.ResolvePointerValue(light.Image, GfxImageWrapperAttribute, light);
            });
        });
    }
}
