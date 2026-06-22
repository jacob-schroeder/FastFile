using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.ColMapSp;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class ColMapSp() : BaseAsset(XAssetType.ColMapSp)
{
    public const int RootSize = 0x100;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"ColMapSp 0x{Offset:X8}";
}
