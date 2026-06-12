using FastFile.Models.Archive;
using FastFile.Models.Zone;

namespace FastFile.Tests;

public sealed class BoneyardLoadGoldenTests
{
    [Fact]
    public void BoneyardLoadParsesToCompletion()
    {
        var golden = GoldenFastFileFixture.ReadOfficialFastFile("mp_boneyard_load.ff");
        var reader = golden.ZoneReader;
        var header = reader.GetHeader();
        var assetList = reader.GetAssetList();
        var assets = assetList.Assets;

        Assert.Equal(XFILE_VERSION.Mw2, golden.FastFileHeader.Version);
        Assert.Equal(header.Size + 0x24, reader.GetSourcePosition());
        Assert.Equal(header.BlockSize, reader.GetWrittenBlockSizes());
        Assert.Equal(assetList.AssetCount, assets.Length);
        Assert.All(assets, asset => Assert.NotNull(asset.XAssetPtr.Value));
    }
}
