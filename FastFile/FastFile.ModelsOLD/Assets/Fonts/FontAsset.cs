using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;

namespace FastFile.ModelsOLD.Assets.Fonts;

[XStruct(Block = XFILE_BLOCK.TEMP, Size = RootSize)]
public sealed class FontAsset() : BaseAsset(XAssetType.Font)
{
    public const int RootSize = 0x18;
    public const int GlyphSize = 0x18;

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int PixelHeight { get; set; }

    [XField(Offset = 0x08)]
    public int GlyphCount { get; set; }

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        OffsetIsAliasCell = true)]
    public XPointer<Material.Material> Material { get; set; } // Alias

    [XField(Offset = 0x10)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        OffsetIsAliasCell = true)]
    public XPointer<Material.Material> GlowMaterial { get; set; } // Alias

    [XField(Offset = 0x14)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(GlyphCount))]
    public XPointer<FontGlyph[]> Glyphs { get; set; } // Direct

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name) ? Type.ToString() : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = FontAsset.GlyphSize)]
public sealed class FontGlyph
{
    [XField(Offset = 0x00)]
    public ushort Letter { get; set; }

    [XField(Offset = 0x02)]
    public byte X0 { get; set; }

    [XField(Offset = 0x03)]
    public byte Y0 { get; set; }

    [XField(Offset = 0x04)]
    public byte Dx { get; set; }

    [XField(Offset = 0x05)]
    public byte PixelWidth { get; set; }

    [XField(Offset = 0x06)]
    public byte PixelHeight { get; set; }

    [XField(Offset = 0x07)]
    public byte Padding { get; set; }

    [XField(Offset = 0x08)]
    public float S0 { get; set; }

    [XField(Offset = 0x0C)]
    public float T0 { get; set; }

    [XField(Offset = 0x10)]
    public float S1 { get; set; }

    [XField(Offset = 0x14)]
    public float T1 { get; set; }
}
