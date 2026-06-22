using FastFile.ModelsOLD.Archive;
using FastFile.ModelsOLD.Assets.StringTables;
using FastFile.ModelsOLD.Assets.TechniqueSet;
using FastFile.ModelsOLD.Assets.Weapons;
using FastFile.ModelsOLD.Zone;

namespace FastFile.TestsOLD;

public sealed class PatchMpGoldenTests
{
    [Fact]
    public void PatchMpMatchesGoldenStreamAndAssetInvariants()
    {
        var golden = GoldenFastFileFixture.ReadPatchMp();
        var reader = golden.ZoneReader;
        var header = reader.GetHeader();
        var assetList = reader.GetAssetList();
        var assets = assetList.Assets;
        var writtenSizes = reader.GetWrittenBlockSizes();

        Assert.Equal(XFILE_VERSION.Mw2, golden.FastFileHeader.Version);
        Assert.Equal(0x170000, golden.Zone.Length);
        Assert.Equal(0x16549D, reader.GetSourcePosition());
        Assert.Equal(header.Size + 0x24, reader.GetSourcePosition());

        Assert.Equal(0x3B4, header.BlockSize[(int)XFILE_BLOCK.TEMP]);
        Assert.Equal(0x153ACE, header.BlockSize[(int)XFILE_BLOCK.LARGE]);
        Assert.Equal(0x1000, header.BlockSize[(int)XFILE_BLOCK.XFILE_BLOCK_VERTEX]);
        Assert.Equal(header.BlockSize, writtenSizes);

        Assert.Equal(11, assetList.ScriptStringCount);
        Assert.Equal(431, assetList.AssetCount);
        Assert.Equal(431, assets.Length);
        Assert.Equal(431, assets.Count(asset => !string.IsNullOrWhiteSpace(asset.XAssetPtr.Value?.GetDisplayName)));

        AssertAssetTypeCount(assets, XAssetType.Techset, 18);
        AssertAssetTypeCount(assets, XAssetType.MenuFile, 10);
        AssertAssetTypeCount(assets, XAssetType.Localize, 355);
        AssertAssetTypeCount(assets, XAssetType.Weapon, 7);
        AssertAssetTypeCount(assets, XAssetType.RawFile, 36);
        AssertAssetTypeCount(assets, XAssetType.StringTable, 3);
        AssertAssetTypeCount(assets, XAssetType.StructuredDataDef, 2);

        AssertTechsetName(assets, 408, ",m_l_sm_r0c0n0s0p0");
        AssertTechsetName(assets, 409, ",m_l_sm_r0c0n0sf0p0");
        AssertTechsetName(assets, 410, ",m_l_sm_b0c0n0s0");
        AssertTechsetName(assets, 411, ",effect_zfeather_add_nofog");
        AssertTechsetName(assets, 412, ",distortion_scale_zfeather");

        var model1887 = Assert.IsType<WeaponVariantDef>(assets[418].XAssetPtr.Value);
        Assert.Equal("model1887_mp", model1887.InternalNamePtr.Value);
        Assert.Equal("WEAPON_MODEL1887", model1887.DisplayNamePtr.Value);
        Assert.Equal("model1887_mp", model1887.GetDisplayName);
        Assert.NotNull(model1887.WeaponDef);

        var challengeTable = Assert.IsType<StringTable>(assets[2].XAssetPtr.Value);
        Assert.Equal("mp/allchallengestable.csv", challengeTable.Name);
        Assert.Equal(24, challengeTable.ColumnCount);
        Assert.Equal(480, challengeTable.RowCount);
        Assert.Equal(11520, challengeTable.CellCount);
        Assert.Contains(
            challengeTable.Strings,
            cell => cell.PointerString == "WEAPON_MODEL1887" && cell.Hash == unchecked((int)0xBC5F27AC));
    }

    [Fact]
    public void PatchMpReportsMonotonicReadProgressToCompletion()
    {
        var progress = new List<(int Read, int Total)>();

        GoldenFastFileFixture.ReadPatchMp((read, total) => progress.Add((read, total)));

        Assert.NotEmpty(progress);
        Assert.Equal(progress[0].Total, progress[^1].Read);
        Assert.All(progress, item => Assert.True(item.Total > 0));

        for (var i = 1; i < progress.Count; i++)
        {
            Assert.Equal(progress[0].Total, progress[i].Total);
            Assert.True(
                progress[i].Read >= progress[i - 1].Read,
                $"Progress regressed from {progress[i - 1].Read} to {progress[i].Read} at event {i}.");
        }
    }

    private static void AssertAssetTypeCount(IReadOnlyList<XAsset> assets, XAssetType type, int expected)
    {
        Assert.Equal(expected, assets.Count(asset => asset.Type == type));
    }

    private static void AssertTechsetName(IReadOnlyList<XAsset> assets, int index, string expected)
    {
        var techset = Assert.IsType<MaterialTechniqueSet>(assets[index].XAssetPtr.Value);
        Assert.Equal(expected, techset.Name);
        Assert.Equal(expected, techset.GetDisplayName);
    }
}
