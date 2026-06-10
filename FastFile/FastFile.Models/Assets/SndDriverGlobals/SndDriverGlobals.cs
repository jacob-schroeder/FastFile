using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.SndDriverGlobals;

// Root body has not been verified in the current EBOOT traces.
[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class SndDriverGlobals() : BaseAsset(XAssetType.SndDriverGlobals)
{
    public const int RootSize = 0x00;

    public override string? GetDisplayName => $"SndDriverGlobals 0x{Offset:X8}";
}
