using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.MpType;

// Root body has not been verified in the current EBOOT traces.
[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class MpType() : BaseAsset(XAssetType.MpType)
{
    public const int RootSize = 0x00;

    public override string? GetDisplayName => $"MpType 0x{Offset:X8}";
}
