using FastFile.Logic.Assets.Readers;
using FastFile.Models.Assets.Material;

namespace FastFile.Tests;

public sealed class Ps3GfxImagePayloadSizeTests
{
    [Fact]
    public void BuildFormatKey_CombinesFlagsAndFormatByteLikePs3Wrapper()
    {
        var formatKey = Ps3GfxImagePayloadSize.BuildFormatKey(0x85, 0x01AAE4);

        Assert.Equal(unchecked((int)0x01AAE485), formatKey);
    }

    [Fact]
    public void NormalizeFormatByte_MatchesPs3RotationForAlternateEncodings()
    {
        Assert.Equal(0x86, Ps3GfxImagePayloadSize.NormalizeFormatByte(0xA6));
    }

    [Theory]
    [InlineData(GfxImageFormats.GcmFormatA8R8G8B8, 16, 16, 1, 1024)]
    [InlineData(GfxImageFormats.GcmFormatDxt1, 16, 16, 1, 128)]
    [InlineData(GfxImageFormats.GcmFormatDxt23, 16, 16, 1, 256)]
    [InlineData(GfxImageFormats.GcmFormatDxt45, 16, 16, 1, 256)]
    public void ComputeMipLevelByteCount_UsesExpectedCellGcmFamilies(
        byte formatByte,
        int width,
        int height,
        int depth,
        int expectedSize)
    {
        var formatKey = Ps3GfxImagePayloadSize.BuildFormatKey(formatByte, 0);

        var size = Ps3GfxImagePayloadSize.ComputeMipLevelByteCount(formatKey, width, height, depth);

        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void ComputeByteCount_AccumulatesMipChainAlignsAndExpandsCubeFaces()
    {
        var image = new GfxImage
        {
            FormatByte = GfxImageFormats.GcmFormatDxt1,
            LevelCount = 2,
            Width = 16,
            Height = 16,
            Depth = 1,
            MultiFaceControl = 1
        };

        var size = Ps3GfxImagePayloadSize.ComputeByteCount(image);

        Assert.Equal(1536, size);
    }
}
