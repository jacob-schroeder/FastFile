using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.AddonMapEnts;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class AddonMapEnts() : BaseAsset(XAssetType.AddonMapEnts)
{
    public const int RootSize = 0x24;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"AddonMapEnts 0x{Offset:X8}";
}
