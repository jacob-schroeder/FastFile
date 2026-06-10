using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.UiMap;

// Root body has not been verified in the current EBOOT traces.
[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class UiMap() : BaseAsset(XAssetType.UiMap)
{
    public const int RootSize = 0x00;

    public override string? GetDisplayName => $"UiMap 0x{Offset:X8}";
}
