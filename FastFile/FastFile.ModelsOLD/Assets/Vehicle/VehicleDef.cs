using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.Vehicle;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed partial class VehicleDef() : BaseAsset(XAssetType.Vehicle)
{
    public const int RootSize = 0x2D0;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"Vehicle 0x{Offset:X8}";
}
