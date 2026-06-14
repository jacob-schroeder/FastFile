using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Archive;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using System.Reflection;

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

    [Fact]
    public void CommonMpSound493UsesPs3SoundWrapperSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 493);
        var assetList = reader.GetAssetList();

        var sound = Assert.IsType<SndAliasList>(assetList.Assets[493].XAssetPtr.Value);

        Assert.Equal("ab_defeat_music", sound.AliasName);
        Assert.Equal(1, sound.Count);
        Assert.Equal(PointerKind.Inline, sound.Aliases.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, sound.Aliases.Address?.Block);

        var alias = Assert.Single(sound.Aliases.Value ?? []);
        Assert.Equal(1, alias.SoundFileCount);
        Assert.NotNull(alias.SoundFiles.Value);
        Assert.Single(alias.SoundFiles.Value!);
    }

    [Fact]
    public void CommonMpSoundPrefixThrough510HasValidMaterializedPointers()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 520);
        var assetList = reader.GetAssetList();
        var blockSizes = reader.GetHeader().BlockSize;
        var issues = new List<string>();

        for (var i = 493; i <= 510; i++)
        {
            var sound = Assert.IsType<SndAliasList>(assetList.Assets[i].XAssetPtr.Value);
            if (string.IsNullOrWhiteSpace(sound.AliasName))
            {
                issues.Add(
                    $"asset[{i}].AliasNamePtr: raw=0x{sound.AliasNamePtr.Raw:X8} kind={sound.AliasNamePtr.Kind} " +
                    $"target={FormatAddress(sound.AliasNamePtr.Address)} patch={FormatAddress(sound.AliasNamePtr.PatchAddress)} " +
                    $"value=\"{sound.AliasName}\"");
            }

            ValidateMaterializedPointers($"asset[{i}]", sound, blockSizes, issues);
        }

        Assert.True(issues.Count == 0, string.Join(Environment.NewLine, issues.Take(20)));
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

    private static void ValidateMaterializedPointers(
        string path,
        object? value,
        IReadOnlyList<int> blockSizes,
        List<string> issues,
        HashSet<object>? visited = null)
    {
        if (value is null or string)
            return;

        if (value is Array array)
        {
            for (var i = 0; i < array.Length; i++)
                ValidateMaterializedPointers($"{path}[{i}]", array.GetValue(i), blockSizes, issues, visited);

            return;
        }

        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!visited.Add(value))
            return;

        if (value is FastFile.Models.Zone.Pointer pointer)
        {
            ValidatePointer(path, pointer, blockSizes, issues);
            return;
        }

        foreach (var prop in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.GetIndexParameters().Length != 0)
                continue;

            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                continue;
            }

            ValidateMaterializedPointers($"{path}.{prop.Name}", propValue, blockSizes, issues, visited);
        }
    }

    private static void ValidatePointer(
        string path,
        FastFile.Models.Zone.Pointer pointer,
        IReadOnlyList<int> blockSizes,
        List<string> issues)
    {
        if (pointer.IsNull)
            return;

        if (!pointer.IsResolved || pointer.Address is null)
        {
            issues.Add($"{path}: unresolved raw=0x{pointer.Raw:X8} kind={pointer.Kind}");
            return;
        }

        ValidateAddress($"{path}.target", pointer.Address.Value, blockSizes, issues);

        if (pointer.PatchAddress is { } patchAddress)
            ValidateAddress($"{path}.patch", patchAddress, blockSizes, issues);

        if (pointer.GetType().GetGenericArguments().FirstOrDefault() == typeof(string))
        {
            var value = pointer.GetType().GetProperty(nameof(XPointer<string>.Value))?.GetValue(pointer);
            if (value is null)
                issues.Add($"{path}: string raw=0x{pointer.Raw:X8} resolved to {FormatAddress(pointer.Address)} but Value was null.");
        }
    }

    private static void ValidateAddress(
        string path,
        XBlockAddress address,
        IReadOnlyList<int> blockSizes,
        List<string> issues)
    {
        var blockIndex = (int)address.Block;
        if (blockIndex < 0 || blockIndex >= blockSizes.Count)
        {
            issues.Add($"{path}: invalid block {address.Block}.");
            return;
        }

        if (address.Offset < 0 || address.Offset >= blockSizes[blockIndex])
        {
            issues.Add(
                $"{path}: {address.Block}:0x{address.Offset:X} outside block size 0x{blockSizes[blockIndex]:X}.");
        }
    }

    private static string FormatAddress(XBlockAddress? address)
    {
        return address is { } value
            ? $"{value.Block}:0x{value.Offset:X}"
            : "<null>";
    }
}
