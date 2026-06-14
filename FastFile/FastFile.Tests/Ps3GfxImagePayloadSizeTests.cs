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
    [InlineData(0x8B, 16, 16, 1, 512)]
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

    [Fact]
    public void ComputeByteCount_FallsBackToFirstPositiveCardMemoryWord()
    {
        var image = new GfxImage
        {
            LevelCount = 0,
            CardMemoryPlatformWords = [0, 0x050002D0]
        };

        var size = Ps3GfxImagePayloadSize.ComputeByteCount(image);

        Assert.Equal(0x050002D0, size);
    }

    [Fact]
    public void ComputeByteCount_UsesObserved16BitPs3FamilyForFormat8B()
    {
        var image = new GfxImage
        {
            FormatByte = 0x8B,
            LevelCount = 8,
            Width = 128,
            Height = 64,
            Depth = 1
        };

        var size = Ps3GfxImagePayloadSize.ComputeByteCount(image);

        Assert.Equal(0x5580, size);
    }
}
