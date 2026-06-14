using FastFile.Models.Assets.Material;
using System.Numerics;

namespace FastFile.Logic.Assets.Readers;

internal static class Ps3GfxImagePayloadSize
{
    // PS3 0x4c7448 -> 0x357760 builds the format key from GfxImage+0x00/+0x04,
    // calls the mip-chain helper at 0x357628, then aligns the total to 0x80
    // and expands multi-face payloads by 6 when GfxImage+0x03 is non-zero.
    internal static int ComputeByteCount(GfxImage image)
    {
        if (image.LevelCount == 0)
            return FirstPositivePlatformControlWord(image);

        var formatKey = BuildFormatKey(image.FormatByte, image.TextureFlags);
        var totalBytes = AccumulateMipChainByteCount(
            formatKey,
            Math.Max(1, (int)image.Width),
            Math.Max(1, (int)image.Height),
            Math.Max(1, (int)image.Depth),
            firstLevel: 0,
            levelCount: image.LevelCount);

        var alignedBytes = Align(totalBytes, GfxImage.EBOOT_PAYLOAD_ALIGNMENT);
        if (image.MultiFaceControl != 0)
            alignedBytes = Align(checked(alignedBytes * 6), GfxImage.EBOOT_PAYLOAD_ALIGNMENT);

        return alignedBytes > 0
            ? alignedBytes
            : FirstPositivePlatformControlWord(image);
    }

    internal static int BuildFormatKey(byte formatByte, int textureFlags)
    {
        return unchecked((textureFlags << 8) | NormalizeFormatByte(formatByte));
    }

    internal static int AccumulateMipChainByteCount(
        int formatKey,
        int width,
        int height,
        int depth,
        int firstLevel,
        int levelCount)
    {
        if (levelCount <= 0 || firstLevel >= levelCount)
            return 0;

        width = ShiftMipDimension(width, firstLevel);
        height = ShiftMipDimension(height, firstLevel);
        depth = ShiftMipDimension(depth, firstLevel);

        var totalBytes = 0;
        for (var level = firstLevel; level < levelCount; level++)
        {
            totalBytes = checked(totalBytes + ComputeMipLevelByteCount(formatKey, width, height, depth));
            width = Math.Max(1, width >> 1);
            height = Math.Max(1, height >> 1);
            depth = Math.Max(1, depth >> 1);
        }

        return totalBytes;
    }

    internal static int ComputeMipLevelByteCount(
        int formatKey,
        int width,
        int height,
        int depth)
    {
        // PS3 0x3573a8 switches on the full format key. For the common MW2
        // image families we can preserve the exact byte-count behavior by
        // grouping on the resolved CELL_GCM format id in the low byte.
        return GetResolvedCellGcmFormat(formatKey) switch
        {
            GfxImageFormats.GcmFormatDxt1 => checked(GetCompressedBlockWidth(width) * GetCompressedBlockWidth(height) * depth * 8),
            GfxImageFormats.GcmFormatDxt23 or GfxImageFormats.GcmFormatDxt45 => checked(GetCompressedBlockWidth(width) * GetCompressedBlockWidth(height) * depth * 16),
            GfxImageFormats.GcmFormatA8R8G8B8 => checked(width * height * depth * 4),
            _ => 0
        };
    }

    private static int GetResolvedCellGcmFormat(int formatKey)
    {
        return formatKey & 0xFF;
    }

    internal static byte NormalizeFormatByte(byte formatByte)
    {
        // PS3 0x357760:
        //   rlwinm r3,r9,25,2,31
        //   rotlwi r3,r3,7
        // This normalizes alternate CELL_GCM byte encodings like 0xA6 -> 0x86
        // before the size switch at 0x3573a8.
        var rotated = BitOperations.RotateLeft((uint)formatByte, 25) & 0x3FFFFFFF;
        return (byte)BitOperations.RotateLeft(rotated, 7);
    }

    private static int GetCompressedBlockWidth(int dimension)
    {
        return (dimension + 3) >> 2;
    }

    private static int ShiftMipDimension(int dimension, int mipLevel)
    {
        if (mipLevel <= 0)
            return Math.Max(1, dimension);

        var shifted = dimension >> mipLevel;
        return shifted > 0 ? shifted : 1;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private static int FirstPositivePlatformControlWord(GfxImage image)
    {
        for (var i = 0; i < image.PlatformControlWords.Length; i++)
        {
            if (image.PlatformControlWords[i] > 0)
                return image.PlatformControlWords[i];
        }

        return 0;
    }
}
