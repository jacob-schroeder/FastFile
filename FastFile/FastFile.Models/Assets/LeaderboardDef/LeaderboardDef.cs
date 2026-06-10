using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.LeaderboardDef;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class LeaderboardDef() : BaseAsset(XAssetType.LeaderboardDef)
{
    public const int RootSize = 0x18;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"LeaderboardDef 0x{Offset:X8}";
}
