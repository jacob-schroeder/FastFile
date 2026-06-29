using FastFile.Models.Assets.Image;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.LightDef;

public sealed class LightDefAsset : BaseAsset
{
    public const int SerializedSize = 0x10;

    public XAssetType Type => XAssetType.LightDef;

    // 0x00: XString. EBOOT 0x1089d0 stores root+0x00 into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }

    // 0x04: GfxLightImage.image. EBOOT 0x1089e0..0x1089f0 advances to root+0x04 and calls the image pointer wrapper.
    public XPointer<GfxImageAsset> ImagePointer { get; init; }
    public GfxImageAsset? Image { get; init; }

    // 0x08: GfxLightImage.samplerState. Xbox GfxLightImage/GfxLightDef loader family matches PS3 root shape.
    public byte SamplerState { get; init; }
    public byte[] Pad09To0B { get; init; } = [];

    // 0x0C: Xbox-correlated GfxLightDef.lmapLookupStart. PS3 loader copies it as the root tail int32.
    public uint LmapLookupStart { get; init; }
}
