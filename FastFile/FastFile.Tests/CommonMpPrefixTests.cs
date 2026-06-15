using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.GfxLightDef;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Assets.XAnim;
using FastFile.Models.Assets.XModels;
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

    [Fact]
    public void CommonMpPrefixThrough5535LoadsFxNestedXModelPayloads()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5535);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Fx, assetList.Assets[5530].Type);
        Assert.Equal(XAssetType.Fx, assetList.Assets[5531].Type);

        var models = assetList.Assets
            .Take(5536)
            .Select(asset => asset.XAssetPtr.Value)
            .OfType<FxEffectDef>()
            .SelectMany(EnumerateResolvedFxModels)
            .ToArray();

        Assert.NotEmpty(models);
        Assert.Contains(models, model => model.PartClassification.IsResolved);

        foreach (var model in models)
        {
            AssertXModelArrayPointer(model.BoneNames, model.BoneNameCount);
            AssertXModelBytePointer(model.ParentList, model.ParentCount);
            AssertXModelArrayPointer(model.Quats, model.QuatComponentCount);
            AssertXModelArrayPointer(model.Trans, model.PartCount);
            AssertXModelBytePointer(model.PartClassification, model.BoneNameCount);
            AssertXModelArrayPointer(model.BaseMat, model.BoneNameCount);
            AssertXModelArrayPointer(model.InvHighMipRadius, model.MaterialHandleCount);
        }
    }

    [Fact]
    public void CommonMpFirstWeapon5536ResolvesPs3NestedPointerWrappers()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5536);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Weapon, assetList.Assets[5536].Type);

        var weapon = Assert.IsType<WeaponVariantDef>(assetList.Assets[5536].XAssetPtr.Value);
        Assert.Equal("sentry_minigun_mp", weapon.InternalName);
        Assert.Equal(PointerKind.Inline, weapon.WeaponDefPtr.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, weapon.WeaponDefPtr.Address?.Block);

        Assert.NotNull(weapon.WeaponDef);
        var def = weapon.WeaponDef;

        Assert.Equal(PointerKind.Inline, def.SoundAliases[0].Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, def.SoundAliases[0].Address?.Block);
        Assert.Equal(PointerKind.Inline, def.MaterialPointersA[0].Kind);

        var reticleMaterial = Assert.IsType<Material>(def.MaterialPointersA[0].Value);
        Assert.Equal("gfx/reticle/mg42_cross.tga", reticleMaterial.Info.Name);
        Assert.Equal(",2d", reticleMaterial.TechniqueSet.Value?.Name);

        Assert.Equal(PointerKind.Inline, def.Tracer.Kind);
        Assert.Equal(XFILE_BLOCK.TEMP, def.Tracer.Address?.Block);

        var tracer = Assert.IsType<TracerDef>(def.Tracer.Value);
        Assert.Equal(PointerKind.Inline, tracer.NamePtr.Kind);
        Assert.False(string.IsNullOrWhiteSpace(tracer.Name));
        Assert.Equal(PointerKind.Insert, tracer.Material.Kind);
        var tracerMaterial = Assert.IsType<Material>(tracer.Material.Value);
        Assert.False(string.IsNullOrWhiteSpace(tracerMaterial.Info.Name));
    }

    [Fact]
    public void CommonMpFirstXModel5538UsesPs3SurfacePayloadOrder()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5538);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.XModel, assetList.Assets[5538].Type);

        var model = Assert.IsType<XModel>(assetList.Assets[5538].XAssetPtr.Value);
        Assert.Equal("sentry_minigun", model.Name);

        AssertXModelArrayPointer(model.BoneNames, model.BoneNameCount);
        AssertXModelBytePointer(model.ParentList, model.ParentCount);
        AssertXModelArrayPointer(model.Quats, model.QuatComponentCount);
        AssertXModelArrayPointer(model.Trans, model.PartCount);
        AssertXModelBytePointer(model.PartClassification, model.BoneNameCount);
        AssertXModelArrayPointer(model.BaseMat, model.BoneNameCount);
        AssertXModelArrayPointer(model.InvHighMipRadius, model.MaterialHandleCount);

        Assert.Equal(PointerKind.Insert, model.PhysPreset.Kind);
        Assert.Equal(XFILE_BLOCK.TEMP, model.PhysPreset.Address?.Block);
        var physPreset = Assert.IsType<PhysPreset>(model.PhysPreset.Value);
        Assert.False(string.IsNullOrWhiteSpace(physPreset.Name));
        Assert.True(model.PhysCollmap.IsNull);

        var modelSurfs = model.LodInfo
            .Select(lod => lod.ModelSurfs.Value)
            .OfType<XModelSurfs>()
            .ToArray();

        Assert.NotEmpty(modelSurfs);
        Assert.Contains(modelSurfs, surfs => surfs.Surfs.Value is { Length: > 0 });

        foreach (var surfs in modelSurfs)
        {
            Assert.False(string.IsNullOrWhiteSpace(surfs.Name));
            AssertXModelArrayPointer(surfs.Surfs, surfs.NumSurfs);

            foreach (var surface in surfs.Surfs.Value ?? [])
            {
                Assert.Equal(surface.VertCount * 0x10, surface.VertexByteCount);
                Assert.Equal(surface.TriCount * 3, surface.TriIndexCount);

                AssertSurfaceArrayPointer(surface.VertInfo.VertsBlend, surface.VertInfo.BlendVertCount);
                AssertSurfaceBytePointer(surface.Verts0, surface.VertexByteCount);
                AssertSurfaceBytePointer(surface.Verts1, surface.VertexByteCount);
                AssertSurfaceArrayPointer(surface.VertList, surface.VertListCount);
                AssertSurfaceArrayPointer(surface.TriIndices, surface.TriIndexCount);
            }
        }
    }

    [Fact]
    public void CommonMpFirstXAnim5546UsesPs3XAnimPartsSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5547);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.XAnim, assetList.Assets[5546].Type);
        Assert.Equal(XAssetType.Fx, assetList.Assets[5547].Type);

        var anim = Assert.IsType<XAnimParts>(assetList.Assets[5546].XAssetPtr.Value);
        Assert.Equal("minigun_spin_loop", anim.Name);

        Assert.Equal(PointerKind.Inline, anim.Names.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, anim.Names.Address?.Block);
        Assert.NotNull(anim.Names.Value);
        Assert.Equal(anim.BoneNameCount, anim.Names.Value!.Length);

        Assert.Equal(PointerKind.Inline, anim.Notify.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, anim.Notify.Address?.Block);
        Assert.NotNull(anim.Notify.Value);
        Assert.Equal(anim.NotifyCount, anim.Notify.Value!.Length);

        Assert.Equal(PointerKind.Inline, anim.DataByte.Kind);
        Assert.NotNull(anim.DataByte.Value);
        Assert.Equal(anim.DataByteCount, anim.DataByte.Value!.Length);

        Assert.True(anim.DeltaPart.IsNull);
        Assert.NotNull(assetList.Assets[5547].XAssetPtr.Value);
    }

    [Fact]
    public void CommonMpFx5558LoadsNestedPhysCollmapSemantics()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5570);
        var assetList = reader.GetAssetList();
        var blockSizes = reader.GetHeader().BlockSize;

        Assert.Equal(XAssetType.Fx, assetList.Assets[5558].Type);
        var fx = Assert.IsType<FxEffectDef>(assetList.Assets[5558].XAssetPtr.Value);
        Assert.Equal("explosions/sentry_gun_explosion", fx.Name);

        var collmaps = EnumerateResolvedFxModels(fx)
            .Select(model => model.PhysCollmap.Value)
            .OfType<PhysCollmap>()
            .ToArray();

        Assert.NotEmpty(collmaps);

        var issues = new List<string>();
        foreach (var collmap in collmaps)
        {
            AssertPhysCollmap(collmap);
            ValidateMaterializedPointers($"physCollmap[{collmap.Name}]", collmap, blockSizes, issues);
        }

        Assert.True(issues.Count == 0, string.Join(Environment.NewLine, issues.Take(20)));
    }

    [Fact]
    public void CommonMpPrefixThroughWeapon5606ParsesCurrentWeaponPath()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5606);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Weapon, assetList.Assets[5606].Type);
        Assert.NotNull(assetList.Assets[5606].XAssetPtr.Value);
        Assert.IsType<WeaponVariantDef>(assetList.Assets[5606].XAssetPtr.Value);
    }

    [Fact]
    public void CommonMpPrefixThroughFx5665ParsesCurrentFxPath()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5665);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Fx, assetList.Assets[5665].Type);
        var fx = Assert.IsType<FxEffectDef>(assetList.Assets[5665].XAssetPtr.Value);

        Assert.Equal("props/throwingknife_geotrail", fx.Name);

        var elemDefs = Assert.IsType<FxElemDef[]>(fx.ElemDefs.Value);
        Assert.Equal(2, elemDefs.Length);
        Assert.All(elemDefs, elem => Assert.Equal(FxElemType.Trail, elem.ElemType));
    }

    [Fact]
    public void CommonMpPrefixThroughWeapon5756ParsesCurrentWeaponPath()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 5756);
        var assetList = reader.GetAssetList();

        Assert.Equal(XAssetType.Weapon, assetList.Assets[5756].Type);

        var weapon = Assert.IsType<WeaponVariantDef>(assetList.Assets[5756].XAssetPtr.Value);
        Assert.Equal("gl_ak47_mp", weapon.InternalName);
        Assert.NotNull(weapon.WeaponDefPtr.Value);
    }

    [Fact]
    public void CommonMpAllAssetsThrough10090ParsesCurrentPs3LoaderCoverage()
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "common_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        Assert.Equal(XFILE_VERSION.Mw2, fastFileReader.ParseHeader().Version);

        var zone = fastFileReader.UnpackZone();
        var reader = new XFileReader(zone).ReadAssetPrefix((index, _) => index <= 10090);
        var assetList = reader.GetAssetList();

        var light = Assert.IsType<GfxLightDef>(assetList.Assets[10059].XAssetPtr.Value);
        Assert.Equal("light_point_linear", light.Name);
        Assert.Equal(unchecked((int)0x62000000), light.Unknown8);
        Assert.Equal(1, light.UnknownC);
        Assert.Equal(PointerKind.Insert, light.Image.Kind);
        Assert.Equal(XFILE_BLOCK.TEMP, light.Image.Address?.Block);
        Assert.Equal(",falloff_linear", light.Image.Value?.Name);

        var rawFile = Assert.IsType<RawFile>(assetList.Assets[10090].XAssetPtr.Value);
        Assert.Equal("common_mp", rawFile.Name);
        Assert.NotEmpty(rawFile.Buffer);
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

    private static IEnumerable<XModel> EnumerateResolvedFxModels(FxEffectDef fx)
    {
        foreach (var elem in fx.ElemDefs.Value ?? [])
        {
            foreach (var visual in EnumerateFxVisuals(elem))
            {
                if (visual.Model?.Value is { } model)
                    yield return model;
            }
        }
    }

    private static IEnumerable<FxElemDefVisuals> EnumerateFxVisuals(FxElemDef elem)
    {
        if (elem.ElemType == FxElemType.Decal)
            yield break;

        if (elem.VisualCount > 1)
        {
            foreach (var visual in elem.VisualArray?.Value ?? [])
                yield return visual;

            yield break;
        }

        yield return elem.Visuals;
    }

    private static void AssertXModelArrayPointer<T>(
        XPointer<T[]> pointer,
        int expectedCount)
    {
        if (pointer.IsNull)
            return;

        Assert.True(pointer.IsResolved);
        Assert.NotNull(pointer.Value);
        Assert.Equal(XFILE_BLOCK.LARGE, pointer.Address?.Block);
        Assert.Equal(expectedCount, pointer.Value!.Length);
    }

    private static void AssertXModelBytePointer(
        XPointer<byte[]> pointer,
        int expectedCount)
    {
        if (pointer.IsNull)
            return;

        Assert.True(pointer.IsResolved);
        Assert.NotNull(pointer.Value);
        Assert.Equal(XFILE_BLOCK.LARGE, pointer.Address?.Block);
        Assert.Equal(expectedCount, pointer.Value!.Length);
    }

    private static void AssertSurfaceArrayPointer<T>(
        XPointer<T[]> pointer,
        int expectedCount)
    {
        if (pointer.IsNull)
            return;

        Assert.True(pointer.IsResolved);
        Assert.NotNull(pointer.Value);
        AssertSurfacePayloadBlock(pointer.Address);
        Assert.Equal(expectedCount, pointer.Value!.Length);
    }

    private static void AssertSurfaceBytePointer(
        XPointer<byte[]> pointer,
        int expectedCount)
    {
        if (pointer.IsNull)
            return;

        Assert.True(pointer.IsResolved);
        Assert.NotNull(pointer.Value);
        AssertSurfacePayloadBlock(pointer.Address);
        Assert.Equal(expectedCount, pointer.Value!.Length);
    }

    private static void AssertSurfacePayloadBlock(XBlockAddress? address)
    {
        Assert.NotNull(address);
        Assert.True(
            address!.Value.Block is XFILE_BLOCK.LARGE or XFILE_BLOCK.PHYSICAL or XFILE_BLOCK.XFILE_BLOCK_VERTEX,
            $"Expected surface payload in LARGE, PHYSICAL, or VERTEX, got {FormatAddress(address)}.");
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

    private static void AssertPhysCollmap(PhysCollmap collmap)
    {
        Assert.True(collmap.NamePtr.IsResolved);
        Assert.True(collmap.Geoms.IsResolved);
        Assert.Equal(XFILE_BLOCK.LARGE, collmap.Geoms.Address?.Block);
        Assert.NotNull(collmap.Geoms.Value);
        Assert.Equal(collmap.Count, collmap.Geoms.Value!.Length);

        foreach (var geom in collmap.Geoms.Value)
        {
            if (geom.BrushWrapper.IsNull)
                continue;

            Assert.True(geom.BrushWrapper.IsResolved);
            Assert.NotNull(geom.BrushWrapper.Value);

            var wrapper = geom.BrushWrapper.Value!;
            Assert.Equal(wrapper.Brush.NumSides, wrapper.PlaneCount);

            Assert.True(wrapper.Brush.Sides.IsNull || wrapper.Brush.Sides.IsResolved);
            if (!wrapper.Brush.Sides.IsNull)
            {
                Assert.NotNull(wrapper.Brush.Sides.Value);
                Assert.Equal(wrapper.Brush.NumSides, wrapper.Brush.Sides.Value!.Length);
            }

            Assert.True(wrapper.Brush.BaseAdjacentSide.IsNull || wrapper.Brush.BaseAdjacentSide.IsResolved);
            if (!wrapper.Brush.BaseAdjacentSide.IsNull)
            {
                Assert.NotNull(wrapper.Brush.BaseAdjacentSide.Value);
                Assert.Equal(wrapper.TotalEdgeCount, wrapper.Brush.BaseAdjacentSide.Value!.Length);
            }

            Assert.True(wrapper.Planes.IsNull || wrapper.Planes.IsResolved);
            if (!wrapper.Planes.IsNull)
            {
                Assert.NotNull(wrapper.Planes.Value);
                Assert.Equal(wrapper.PlaneCount, wrapper.Planes.Value!.Length);
            }
        }
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
