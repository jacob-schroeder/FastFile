using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Assets.TechniqueSet;
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

    [Fact]
    public void CommonMpFirstFx5528UsesPs3FxEffectDefSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5528);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Fx, assetList.Assets[5528].Type);

        var fx = Assert.IsType<FxEffectDef>(assetList.Assets[5528].XAssetPtr.Value);
        Assert.False(string.IsNullOrWhiteSpace(fx.Name));
        Assert.True(fx.ElemDefCount > 0);
        Assert.True(fx.ElemDefs.IsResolved);
        Assert.Equal(XFILE_BLOCK.LARGE, fx.ElemDefs.Address?.Block);

        var elemDefs = Assert.IsType<FxElemDef[]>(fx.ElemDefs.Value);
        Assert.Equal(fx.ElemDefCount, elemDefs.Length);

        foreach (var elem in elemDefs)
        {
            Assert.True(Enum.IsDefined(elem.ElemType));
            AssertFxSampleArray(elem.VelSamples, elem.VelSampleCount);
            AssertFxSampleArray(elem.VisSamples, elem.VisStateSampleCount);
            AssertFxVisualBranch(elem);
            AssertFxExtendedBranch(elem);
        }
    }

    [Fact]
    public void CommonMpTechset5529DefersForwardShaderLiteralOffsets()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5529);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Techset, assetList.Assets[5529].Type);

        var techset = Assert.IsType<MaterialTechniqueSet>(assetList.Assets[5529].XAssetPtr.Value);
        Assert.Equal("mc_l_sm_r0c0n0sf0", techset.Name);

        var shaderArgs = techset.Techniques
            .Select(pointer => pointer.Value)
            .OfType<MaterialTechnique>()
            .SelectMany(technique => technique.Passes)
            .SelectMany(pass => pass.Args.Value ?? []);

        Assert.Contains(
            shaderArgs,
            argument =>
                (argument.Type == MaterialShaderArgumentType.MTL_ARG_LITERAL_VERTEX_CONST ||
                 argument.Type == MaterialShaderArgumentType.MTL_ARG_LITERAL_PIXEL_CONST) &&
                argument.Argument.LiteralConst is
                {
                    Kind: PointerKind.Offset,
                    Address.Block: XFILE_BLOCK.LARGE
                });
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

    private static void AssertFxSampleArray<T>(
        XPointer<T[]> pointer,
        int expectedCount)
    {
        if (pointer.IsNull)
            return;

        Assert.True(pointer.IsResolved);
        Assert.Equal(XFILE_BLOCK.LARGE, pointer.Address?.Block);
        Assert.NotNull(pointer.Value);
        Assert.Equal(expectedCount, pointer.Value!.Length);
    }

    private static void AssertFxVisualBranch(FxElemDef elem)
    {
        if (elem.ElemType == FxElemType.Decal)
        {
            Assert.NotNull(elem.MarkVisualArray);
            Assert.Null(elem.VisualArray);
            AssertFxArrayPointer(elem.MarkVisualArray!, elem.VisualCountValue);
            return;
        }

        if (elem.VisualCount > 1)
        {
            Assert.NotNull(elem.VisualArray);
            Assert.Null(elem.MarkVisualArray);
            AssertFxArrayPointer(elem.VisualArray!, elem.VisualCountValue);

            foreach (var visual in elem.VisualArray.Value ?? [])
                AssertFxVisualUnion(visual, elem.ElemType);

            return;
        }

        Assert.Null(elem.VisualArray);
        Assert.Null(elem.MarkVisualArray);
        AssertFxVisualUnion(elem.Visuals, elem.ElemType);
    }

    private static void AssertFxVisualUnion(
        FxElemDefVisuals visual,
        FxElemType elemType)
    {
        switch (elemType)
        {
            case FxElemType.Model:
                AssertFxAliasVisualPointer(visual.Model);
                break;

            case FxElemType.OmniLight:
            case FxElemType.SpotLight:
                Assert.Null(visual.Material);
                Assert.Null(visual.Model);
                Assert.Null(visual.EffectDef);
                Assert.Null(visual.SoundName);
                break;

            case FxElemType.Sound:
                Assert.NotNull(visual.SoundName);
                Assert.True(visual.SoundName!.IsResolved);
                break;

            case FxElemType.Runner:
                Assert.NotNull(visual.EffectDef);
                Assert.True(visual.EffectDef!.NamePtr.IsResolved);
                break;

            default:
                AssertFxAliasVisualPointer(visual.Material);
                break;
        }
    }

    private static void AssertFxAliasVisualPointer<T>(XPointer<T>? pointer)
    {
        Assert.NotNull(pointer);

        // Prefix parses can see a forward alias cell before the owning asset has
        // materialized. The loader branch is still proven by creating the typed
        // pointer; deferred offset aliases resolve once the target insert exists.
        Assert.True(pointer!.IsResolved || pointer.Kind == PointerKind.Offset);
    }

    private static void AssertFxExtendedBranch(FxElemDef elem)
    {
        if (elem.Extended.IsNull)
        {
            Assert.Null(elem.Extended.Value);
            return;
        }

        Assert.True(elem.Extended.IsResolved);
        var extended = Assert.IsType<FxElemExtendedDef>(elem.Extended.Value);

        switch (elem.ElemType)
        {
            case FxElemType.Trail:
                Assert.Equal(FxElemExtendedDefKind.Trail, extended.Kind);
                Assert.NotNull(extended.TrailDef);
                AssertFxSampleArray(extended.TrailDef!.Verts, extended.TrailDef.VertCount);
                AssertFxSampleArray(extended.TrailDef.Inds, extended.TrailDef.IndCount);
                break;

            case FxElemType.SparkFountain:
                Assert.Equal(FxElemExtendedDefKind.SparkFountain, extended.Kind);
                Assert.NotNull(extended.SparkFountainDef);
                break;

            default:
                Assert.Equal(FxElemExtendedDefKind.Unknown, extended.Kind);
                Assert.NotNull(extended.UnknownDef);
                break;
        }
    }

    private static void AssertFxArrayPointer<T>(
        XPointer<T[]> pointer,
        int expectedCount)
    {
        Assert.True(pointer.IsResolved);
        Assert.Equal(XFILE_BLOCK.LARGE, pointer.Address?.Block);
        Assert.NotNull(pointer.Value);
        Assert.Equal(expectedCount, pointer.Value!.Length);
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
