using FastFile.LogicOLD.Assets.Readers;
using FastFile.ModelsOLD.Assets.Material;

namespace FastFile.TestsOLD;

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

    [Theory]
    [InlineData(unchecked((int)0x01AAE485), 1024)]
    [InlineData(unchecked((int)0x01AAE490), 1024)]
    [InlineData(unchecked((int)0x01AAE49C), 1024)]
    [InlineData(unchecked((int)0x01AAE49E), 1024)]
    [InlineData(unchecked((int)0x00AAFE9F), 1024)]
    [InlineData(unchecked((int)0x01AAE492), 512)]
    [InlineData(unchecked((int)0x01AAAB8B), 512)]
    [InlineData(unchecked((int)0x01A9FF81), 256)]
    [InlineData(unchecked((int)0x0156FF81), 256)]
    [InlineData(unchecked((int)0x01A9AA86), 128)]
    [InlineData(unchecked((int)0x01AA5686), 128)]
    [InlineData(unchecked((int)0x0156AA86), 128)]
    [InlineData(unchecked((int)0x01AAE486), 128)]
    [InlineData(unchecked((int)0x01AAE488), 256)]
    public void ComputeMipLevelByteCount_UsesExactPs3FormatKeyTable(
        int formatKey,
        int expectedSize)
    {
        var size = Ps3GfxImagePayloadSize.ComputeMipLevelByteCount(
            formatKey,
            width: 16,
            height: 16,
            depth: 1);

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

    [Fact]
    public void ComputeByteCount_UsesExactPs3FormatKeyForDistortionRippleTrail()
    {
        var image = new GfxImage
        {
            FormatByte = 0x9E,
            TextureFlags = 0x0001AAE4,
            LevelCount = 9,
            Width = 256,
            Height = 64,
            Depth = 1,
            CardMemoryPlatformWords = [unchecked((int)0x01000040), 0x00010900]
        };

        var size = Ps3GfxImagePayloadSize.ComputeByteCount(image);

        Assert.Equal(0x15580, size);
    }
}
