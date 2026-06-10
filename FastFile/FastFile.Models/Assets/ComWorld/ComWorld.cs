using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.ComWorld;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class ComWorld() : BaseAsset(XAssetType.ComMap)
{
    public const int RootSize = 0x10;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"ComMap 0x{Offset:X8}";
}
