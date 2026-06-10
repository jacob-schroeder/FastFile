using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.XAnim;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class XAnimParts() : BaseAsset(XAssetType.XAnim)
{
    public const int RootSize = 0x58;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"XAnim 0x{Offset:X8}";
}
