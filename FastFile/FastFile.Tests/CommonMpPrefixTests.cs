using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.Material;
using FastFile.Models.Archive;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Tests;

public sealed class CommonMpPrefixTests
{
    [Fact]
    public void CommonMpXAssetListUsesAlignedLargeTables()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((_, _) => false);
        var assetList = reader.GetAssetList();

        Assert.Equal(1222, assetList.ScriptStringCount);
        Assert.Equal(10091, assetList.AssetCount);

        Assert.Equal(XFILE_BLOCK.LARGE, assetList.ScriptStringsPtr.Address?.Block);
        Assert.Equal(0x0, assetList.ScriptStringsPtr.Address?.Offset);

        Assert.Equal(XFILE_BLOCK.LARGE, assetList.AssetsPtr.Address?.Block);
        Assert.Equal(0x5EB0, assetList.AssetsPtr.Address?.Offset);

        Assert.Equal(XAssetType.Techset, assetList.Assets[0].Type);
        Assert.Equal(XAssetType.Material, assetList.Assets[1].Type);
        Assert.Equal(XFILE_BLOCK.LARGE, assetList.Assets[0].XAssetPtr.PatchAddress?.Block);
        Assert.Equal(0x5EB4, assetList.Assets[0].XAssetPtr.PatchAddress?.Offset);
    }

    [Fact]
    public void CommonMpFirstMaterialUsesPs3ImagePayloadSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 1);
        var assetList = reader.GetAssetList();

        var material = Assert.IsType<Material>(assetList.Assets[1].XAssetPtr.Value);
        Assert.Equal("ammo_counter_beltbullet_mp", material.Info.Name);
        Assert.Equal(",2d", material.TechniqueSet.Value?.Name);

        var texture = Assert.Single(material.TextureTable.Value ?? []);
        var image = Assert.IsType<GfxImage>(texture.Info.Image?.Value);

        Assert.Equal("ammo_counter_beltbullet_mp", image.Name);
        Assert.NotNull(image.LoadDef);
        Assert.True(image.LoadDef.IsResolved);
        Assert.Equal(PointerKind.Inline, image.LoadDef.Kind);
        Assert.Equal(XFILE_BLOCK.PHYSICAL, image.LoadDef.Address?.Block);
        Assert.NotNull(image.LoadDef.Value);
        Assert.NotEmpty(image.LoadDef.Value!.Data);
        Assert.Equal(image.LoadDef.Value.ResourceSize, image.LoadDef.Value.Data.Length);
    }

    [Fact]
    public void CommonMpPrefixThroughAsset384SurvivesForwardMaterialImageAliases()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 384);
        var assetList = reader.GetAssetList();

        Assert.NotNull(assetList.Assets[40].XAssetPtr.Value);
        Assert.IsType<Material>(assetList.Assets[40].XAssetPtr.Value);
        Assert.NotNull(assetList.Assets[384].XAssetPtr.Value);
    }

    [Fact]
    public void CommonMpFont492UsesPs3FontWrapperSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 492);
        var assetList = reader.GetAssetList();

        var font = Assert.IsType<FontAsset>(assetList.Assets[492].XAssetPtr.Value);

        Assert.Equal("fonts/bigDevFont", font.Name);
        Assert.Equal(24, font.PixelHeight);
        Assert.Equal(96, font.GlyphCount);
        Assert.Equal(PointerKind.Insert, font.Material.Kind);
        Assert.Equal(PointerKind.Insert, font.GlowMaterial.Kind);
        Assert.Equal(PointerKind.Inline, font.Glyphs.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, font.Glyphs.Address?.Block);
        Assert.NotNull(font.Material.Value);
        Assert.NotNull(font.GlowMaterial.Value);
        Assert.NotNull(font.Glyphs.Value);
        Assert.Equal(font.GlyphCount, font.Glyphs.Value!.Length);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}
