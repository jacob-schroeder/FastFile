using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.GameWorldMp;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class GameWorldMp() : BaseAsset(XAssetType.GameMapMp)
{
    public const int RootSize = 0x08;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"GameMapMp 0x{Offset:X8}";
}
