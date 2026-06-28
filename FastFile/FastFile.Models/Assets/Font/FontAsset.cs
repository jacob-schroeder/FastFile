using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Font;

public sealed class FontAsset : BaseAsset
{
    public const int SerializedSize = 0x18;
    public const int GlyphSerializedSize = 0x18;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public int PixelHeight { get; init; }
    public int GlyphCount { get; init; }
    public XPointer<MaterialAsset> MaterialPointer { get; init; }
    public MaterialAsset? Material { get; init; }
    public XPointer<MaterialAsset> GlowMaterialPointer { get; init; }
    public MaterialAsset? GlowMaterial { get; init; }
    public XPointer<FontGlyph[]> GlyphsPointer { get; init; }
    public IReadOnlyList<FontGlyph> Glyphs { get; init; } = [];
}

public sealed record FontGlyph(
    ushort Letter,
    byte X0,
    byte Y0,
    byte Dx,
    byte PixelWidth,
    byte PixelHeight,
    byte Padding,
    float S0,
    float T0,
    float S1,
    float T1);
