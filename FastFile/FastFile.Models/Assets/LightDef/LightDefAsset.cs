using FastFile.Models.Assets.Image;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.LightDef;

public sealed class LightDefAsset : BaseAsset
{
    public const int SerializedSize = 0x10;

    public XAssetType Type => XAssetType.LightDef;

    public byte[] RootBytes { get; init; } = [];

    // 0x00: XString. EBOOT 0x1089d0 stores root+0x00 into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }

    // 0x04: GfxImage* child. EBOOT 0x1089e0..0x1089f0 advances to root+0x04 and calls the image pointer wrapper.
    public XPointer<GfxImageAsset> ImagePointer { get; init; }
    public GfxImageAsset? Image { get; init; }

    // 0x08/0x0C: copied by the 0x10 root Load_Stream; no PS3 consumer name proven yet.
    public uint Unknown08 { get; init; }
    public uint Unknown0C { get; init; }
}
