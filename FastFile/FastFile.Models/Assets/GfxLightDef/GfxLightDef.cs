using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.GfxLightDef;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class GfxLightDef() : BaseAsset(XAssetType.LightDef)
{
    public const int RootSize = 0x10;

    [XField(Offset = 0x00, Count = RootSize)]
    public byte[] RootBytes { get; set; } = new byte[RootSize];

    public override string? GetDisplayName => $"LightDef 0x{Offset:X8}";
}
