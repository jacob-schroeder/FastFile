using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Image;

public sealed class GfxImageAsset : BaseAsset
{
    public const int SerializedSize = 0x50;

    public XAssetType Type => XAssetType.Image;

    public byte[] RootBytes { get; init; } = [];

    public byte Format { get; init; }
    public byte LevelCount { get; init; }
    public byte Unknown02 { get; init; }
    public byte MultiFaceControl { get; init; }
    public uint TextureFlags { get; init; }
    public ushort Width { get; init; }
    public ushort Height { get; init; }
    public ushort Depth { get; init; }
    public byte MapType { get; init; }
    public byte TextureSemantic { get; init; }
    public XPointerReference PayloadPointer { get; init; }
    public int PayloadByteCount { get; init; }
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
}
