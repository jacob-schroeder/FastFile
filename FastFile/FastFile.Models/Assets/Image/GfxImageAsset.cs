using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Image;

public sealed class GfxImageAsset : BaseAsset
{
    public const int SerializedSize = 0x50;

    public XAssetType Type => XAssetType.Image;

    public byte Format { get; init; }
    public byte LevelCount { get; init; }
    public byte DimensionCount { get; init; }
    public byte MultiFaceControl { get; init; }
    public uint TextureFlags { get; init; }
    public ushort Width { get; init; }
    public ushort Height { get; init; }
    public ushort Depth { get; init; }
    public byte PixelDataBlock { get; init; }
    public byte Pad0F { get; init; }
    public uint RenderTargetPitch { get; init; }
    public uint PixelsOffset { get; init; }
    public byte MapType { get; init; }
    public byte TextureSemantic { get; init; }
    public byte Category { get; init; }
    public byte Pad1B { get; init; }
    public uint CardMemory { get; init; }
    public ushort BaseWidth { get; init; }
    public ushort BaseHeight { get; init; }
    public ushort BaseDepth { get; init; }
    public byte BaseLevelCount { get; init; }
    public byte Pad27 { get; init; }
    public XPointerReference PayloadPointer { get; init; }
    public IReadOnlyList<GfxImageStreamData> StreamData { get; init; } = [];
    public int? StreamImageIndex { get; init; }
    public int PayloadByteCount { get; init; }
    public IReadOnlyList<byte> PayloadBytes { get; init; } = [];
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed record GfxImageStreamData(
    ushort Width,
    ushort Height,
    uint LevelSizeAndOffset)
{
    public const int SerializedSize = 0x08;
    public const int EntryCount = 4;

    public int LevelMarker => (int)(LevelSizeAndOffset >> 26);
    public int CumulativeByteCount => (int)(LevelSizeAndOffset & 0x03ffffff);
    public bool HasStreamingData => Width != 0 || Height != 0 || LevelSizeAndOffset != 0;
}
