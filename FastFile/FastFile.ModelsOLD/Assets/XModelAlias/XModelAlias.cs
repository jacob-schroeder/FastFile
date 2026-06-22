using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.XModelAlias;

// Root body has not been verified in the current EBOOT traces.
[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class XModelAlias() : BaseAsset(XAssetType.XModelAlias)
{
    public const int RootSize = 0x00;

    public override string? GetDisplayName => $"XModelAlias 0x{Offset:X8}";
}
