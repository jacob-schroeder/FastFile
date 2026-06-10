using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.FxWorld;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class FxWorld() : BaseAsset(XAssetType.FxMap)
{
    public const int RootSize = 0x74;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"FxMap 0x{Offset:X8}";
}
