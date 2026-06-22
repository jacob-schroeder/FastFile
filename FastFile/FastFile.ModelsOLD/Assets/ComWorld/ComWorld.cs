using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.ComWorld;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class ComWorld() : BaseAsset(XAssetType.ComMap)
{
    public const int RootSize = 0x10;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"ComMap 0x{Offset:X8}";
}
