using FastFile.Models.Assets.Tracers;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class TracerAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute NameAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE
    };

    private static readonly XPointerFieldAttribute MaterialHandleAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
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
        if (value is not TracerDef tracer)
            return false;

        Load_TracerDef(tracer, context);
        return true;
    }

    // PS3 0x10e148: Load_TracerDef. Body reads 0x70 bytes, pushes block 4,
    // resolves name at +0x00, resolves MaterialHandle at +0x04, then pops.
    private static void Load_TracerDef(
        TracerDef tracer,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerValue(tracer.NamePtr, NameAttribute, tracer);

            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.ResolvePointerValue(tracer.Material, MaterialHandleAttribute, tracer);
            });
        });
    }
}
