using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.GameWorldSp;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class GameWorldSp() : BaseAsset(XAssetType.GameMapSp)
{
    public const int RootSize = 0x38;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"GameMapSp 0x{Offset:X8}";
}
