using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.ImpactFx;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class FxImpactTable() : BaseAsset(XAssetType.ImpactFx)
{
    public const int RootSize = 0x08;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"ImpactFx 0x{Offset:X8}";
}
