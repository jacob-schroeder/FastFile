using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.MapEnts;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class MapEnts() : BaseAsset(XAssetType.MapEnts)
{
    public const int RootSize = 0x2C;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"MapEnts 0x{Offset:X8}";
}
