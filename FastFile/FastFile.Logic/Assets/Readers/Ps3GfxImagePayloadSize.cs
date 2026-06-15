using FastFile.Models.Assets.Material;
using System.Numerics;

namespace FastFile.Logic.Assets.Readers;

internal static class Ps3GfxImagePayloadSize
{
    private enum Ps3FormatFamily
    {
        Unknown,
        Linear8,
        Linear16,
        Linear32,
        Block8,
        Block16
    }

    // PS3 0x4c7448 -> 0x357760 builds the format key from GfxImage+0x00/+0x04,
    // calls the mip-chain helper at 0x357628, then aligns the total to 0x80
    // and expands multi-face payloads by 6 when GfxImage+0x03 is non-zero.
    internal static int ComputeByteCount(GfxImage image)
    {
        if (image.LevelCount == 0)
            return FirstPositiveCardMemoryWord(image);

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
            : FirstPositiveCardMemoryWord(image);
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
        var family = ResolvePs3FormatFamily(formatKey);

        return family switch
        {
            Ps3FormatFamily.Linear8 => checked(width * height * depth),
            Ps3FormatFamily.Linear16 => checked(width * height * depth * 2),
            Ps3FormatFamily.Linear32 => checked(width * height * depth * 4),
            Ps3FormatFamily.Block8 => checked(GetCompressedBlockWidth(width) * GetCompressedBlockWidth(height) * depth * 8),
            Ps3FormatFamily.Block16 => checked(GetCompressedBlockWidth(width) * GetCompressedBlockWidth(height) * depth * 16),
            _ => 0
        };
    }

    private static Ps3FormatFamily ResolvePs3FormatFamily(int formatKey)
    {
        // PS3 0x3573a8 switches on the exact full format key assembled by
        // 0x357760 from `(flags << 8) | normalizedFormat`.
        switch (formatKey)
        {
            case unchecked((int)0x01AAE485):
            case unchecked((int)0x01AAE490):
            case unchecked((int)0x01AAE49C):
            case unchecked((int)0x01AAE49E):
            case unchecked((int)0x00AAFE9F):
                return Ps3FormatFamily.Linear32;

            case unchecked((int)0x01AAE492):
            case unchecked((int)0x01AAAB8B):
                return Ps3FormatFamily.Linear16;

            case unchecked((int)0x01A9FF81):
            case unchecked((int)0x0156FF81):
                return Ps3FormatFamily.Linear8;

            case unchecked((int)0x01A9AA86):
            case unchecked((int)0x01AA5686):
            case unchecked((int)0x0156AA86):
            case unchecked((int)0x01AAE486):
                return Ps3FormatFamily.Block8;

            case unchecked((int)0x01AAE488):
                return Ps3FormatFamily.Block16;

            default:
                return GetResolvedCellGcmFormat(formatKey) switch
                {
                    GfxImageFormats.GcmFormatA8R8G8B8 => Ps3FormatFamily.Linear32,
                    // common_mp material "gradient_center" uses PS3 format
                    // byte 0x8B. The PS3 size helper path treats it as an
                    // uncompressed 16-bit texel family.
                    0x8B => Ps3FormatFamily.Linear16,
                    GfxImageFormats.GcmFormatDxt1 => Ps3FormatFamily.Block8,
                    GfxImageFormats.GcmFormatDxt23 or GfxImageFormats.GcmFormatDxt45 => Ps3FormatFamily.Block16,
                    _ => Ps3FormatFamily.Unknown
                };
        }
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

    private static int FirstPositiveCardMemoryWord(GfxImage image)
    {
        for (var i = 0; i < image.CardMemoryPlatformWords.Length; i++)
        {
            if (image.CardMemoryPlatformWords[i] > 0)
                return image.CardMemoryPlatformWords[i];
        }

        return 0;
    }
}
