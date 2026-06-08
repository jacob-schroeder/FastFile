using FastFile.Models.Data;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Fonts;

public sealed class FontAsset() : BaseAsset(XAssetType.Font)
{
    public const int RootSize = 0x18;
    public const int GlyphSize = 0x18;

    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public int PixelHeight { get; set; }
    public int GlyphCount { get; set; }
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<MaterialAsset> Material { get; set; } = new(0);
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<MaterialAsset> GlowMaterial { get; set; } = new(0);
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(GlyphCount))]
    public DirectPointer<FontGlyph[]> Glyphs { get; set; } = new(0);

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name) ? Type.ToString() : Name;
}

public sealed class FontGlyph
{
    public ushort Letter { get; set; }
    public byte X0 { get; set; }
    public byte Y0 { get; set; }
    public byte Dx { get; set; }
    public byte PixelWidth { get; set; }
    public byte PixelHeight { get; set; }
    public byte Padding { get; set; }
    public float S0 { get; set; }
    public float T0 { get; set; }
    public float S1 { get; set; }
    public float T1 { get; set; }
}
