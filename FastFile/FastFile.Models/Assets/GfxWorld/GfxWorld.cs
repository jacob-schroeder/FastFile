using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.GfxWorld;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class GfxWorld() : BaseAsset(XAssetType.GfxMap)
{
    public const int RootSize = 0x288;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"GfxMap 0x{Offset:X8}";
}
