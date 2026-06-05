using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace UI.Views.Assets;

internal static class GfxImageDecoder
{
    public static ImageDecodeResult Decode(GfxImage image)
    {
        var loadDef = image.LoadDef;
        if (loadDef is null || loadDef.Kind == PointerKind.Null)
        {
            return new ImageDecodeResult(null, "No load definition", "No pixel data pointer is present.");
        }

        if (loadDef.Kind == PointerKind.Offset)
        {
            return new ImageDecodeResult(null, "Offset/streamed", "Pixel data is referenced by an offset pointer or streamed data slot.");
        }

        if (!loadDef.IsResolved || loadDef.Result is null)
        {
            return new ImageDecodeResult(null, "Unresolved", "Pixel data was not resolved by the image reader.");
        }

        if (image.Width == 0 || image.Height == 0)
        {
            return new ImageDecodeResult(null, ImageFormatFormatter.Format(loadDef.Result.Format), "Image dimensions are empty.");
        }

        var data = loadDef.Result.Data;
        if (data.Length == 0)
        {
            return new ImageDecodeResult(null, ImageFormatFormatter.Format(loadDef.Result.Format), "The load definition does not contain pixel bytes.");
        }

        var pixelFormat = ImageFormatFormatter.Resolve(loadDef.Result.Format);
        var formatName = ImageFormatFormatter.Format(loadDef.Result.Format);
        if (pixelFormat == ImagePixelFormat.Unknown)
        {
            return new ImageDecodeResult(null, formatName, "This texture format is not decoded yet.");
        }

        var width = image.Width;
        var height = image.Height;
        var expectedSize = GetTopMipSize(width, height, pixelFormat);
        if (expectedSize > data.Length)
        {
            return new ImageDecodeResult(
                null,
                formatName,
                $"Inline data is too short for the top mip. Expected {expectedSize:N0} bytes, found {data.Length:N0}.");
        }

        var pixels = new byte[width * height * 4];
        DecodePixels(data.AsSpan(0, expectedSize), pixels, width, height, pixelFormat, loadDef.Result.Format);
        return new ImageDecodeResult(CreateBitmap(pixels, width, height), formatName, "Decoded from inline pixel data.");
    }

    private static int GetTopMipSize(int width, int height, ImagePixelFormat format)
    {
        return format switch
        {
            ImagePixelFormat.Bc1 => Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * 8,
            ImagePixelFormat.Bc2 or ImagePixelFormat.Bc3 => Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * 16,
            ImagePixelFormat.Bgra32 or ImagePixelFormat.Bgrx32 => width * height * 4,
            ImagePixelFormat.Rgb565 or ImagePixelFormat.A1Rgb555 or ImagePixelFormat.Argb4444 or ImagePixelFormat.AlphaLuminance8 => width * height * 2,
            ImagePixelFormat.Alpha8 or ImagePixelFormat.Luminance8 => width * height,
            _ => 0
        };
    }

    private static void DecodePixels(
        ReadOnlySpan<byte> data,
        byte[] pixels,
        int width,
        int height,
        ImagePixelFormat format,
        int rawFormat)
    {
        switch (format)
        {
            case ImagePixelFormat.Bc1:
                DecodeBc1(data, pixels, width, height, IsGcmFormat(rawFormat));
                break;
            case ImagePixelFormat.Bc2:
                DecodeBc2(data, pixels, width, height, IsGcmFormat(rawFormat));
                break;
            case ImagePixelFormat.Bc3:
                DecodeBc3(data, pixels, width, height, IsGcmFormat(rawFormat));
                break;
            case ImagePixelFormat.Bgra32:
                DecodeBgra32(data, pixels, IsGcmFormat(rawFormat));
                break;
            case ImagePixelFormat.Bgrx32:
                DecodeBgrx32(data, pixels);
                break;
            case ImagePixelFormat.Rgb565:
                DecodeRgb565(data, pixels);
                break;
            case ImagePixelFormat.A1Rgb555:
                DecodeA1Rgb555(data, pixels);
                break;
            case ImagePixelFormat.Argb4444:
                DecodeArgb4444(data, pixels);
                break;
            case ImagePixelFormat.Alpha8:
                DecodeAlpha8(data, pixels);
                break;
            case ImagePixelFormat.Luminance8:
                DecodeLuminance8(data, pixels);
                break;
            case ImagePixelFormat.AlphaLuminance8:
                DecodeAlphaLuminance8(data, pixels);
                break;
        }
    }

    private static void DecodeBc1(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, bool bigEndianBlocks)
    {
        var blockIndex = 0;
        var blockColumns = Math.Max(1, (width + 3) / 4);
        var blockRows = Math.Max(1, (height + 3) / 4);

        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                DecodeBcColorBlock(data.Slice(blockIndex, 8), pixels, width, height, blockX, blockY, allowTransparent: true, bigEndianBlocks);
                blockIndex += 8;
            }
        }
    }

    private static void DecodeBc2(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, bool bigEndianBlocks)
    {
        var blockIndex = 0;
        var blockColumns = Math.Max(1, (width + 3) / 4);
        var blockRows = Math.Max(1, (height + 3) / 4);

        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                var alpha = data.Slice(blockIndex, 8);
                DecodeBcColorBlock(data.Slice(blockIndex + 8, 8), pixels, width, height, blockX, blockY, allowTransparent: false, bigEndianBlocks);
                ApplyBc2Alpha(alpha, pixels, width, height, blockX, blockY);
                blockIndex += 16;
            }
        }
    }

    private static void DecodeBc3(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, bool bigEndianBlocks)
    {
        var blockIndex = 0;
        var blockColumns = Math.Max(1, (width + 3) / 4);
        var blockRows = Math.Max(1, (height + 3) / 4);

        for (var blockY = 0; blockY < blockRows; blockY++)
        {
            for (var blockX = 0; blockX < blockColumns; blockX++)
            {
                var alpha = data.Slice(blockIndex, 8);
                DecodeBcColorBlock(data.Slice(blockIndex + 8, 8), pixels, width, height, blockX, blockY, allowTransparent: false, bigEndianBlocks);
                ApplyBc3Alpha(alpha, pixels, width, height, blockX, blockY);
                blockIndex += 16;
            }
        }
    }

    private static void DecodeBcColorBlock(
        ReadOnlySpan<byte> block,
        byte[] pixels,
        int width,
        int height,
        int blockX,
        int blockY,
        bool allowTransparent,
        bool bigEndianBlocks)
    {
        var color0 = ReadUInt16(block, 0, bigEndianBlocks);
        var color1 = ReadUInt16(block, 2, bigEndianBlocks);
        var indices = ReadUInt32(block, 4, bigEndianBlocks);
        var colors = BuildBcColors(color0, color1, allowTransparent);

        for (var y = 0; y < 4; y++)
        {
            var targetY = blockY * 4 + y;
            if (targetY >= height)
            {
                continue;
            }

            for (var x = 0; x < 4; x++)
            {
                var targetX = blockX * 4 + x;
                if (targetX >= width)
                {
                    continue;
                }

                var colorIndex = (int)((indices >> (2 * (y * 4 + x))) & 0x3);
                SetPixel(pixels, width, targetX, targetY, colors[colorIndex]);
            }
        }
    }

    private static BgraColor[] BuildBcColors(ushort color0, ushort color1, bool allowTransparent)
    {
        var c0 = Rgb565ToColor(color0);
        var c1 = Rgb565ToColor(color1);
        var colors = new BgraColor[4];

        colors[0] = c0;
        colors[1] = c1;
        if (!allowTransparent || color0 > color1)
        {
            colors[2] = new BgraColor(
                (byte)((2 * c0.B + c1.B) / 3),
                (byte)((2 * c0.G + c1.G) / 3),
                (byte)((2 * c0.R + c1.R) / 3),
                255);
            colors[3] = new BgraColor(
                (byte)((c0.B + 2 * c1.B) / 3),
                (byte)((c0.G + 2 * c1.G) / 3),
                (byte)((c0.R + 2 * c1.R) / 3),
                255);
            return colors;
        }

        colors[2] = new BgraColor(
            (byte)((c0.B + c1.B) / 2),
            (byte)((c0.G + c1.G) / 2),
            (byte)((c0.R + c1.R) / 2),
            255);
        colors[3] = new BgraColor(0, 0, 0, 0);
        return colors;
    }

    private static void ApplyBc2Alpha(ReadOnlySpan<byte> block, byte[] pixels, int width, int height, int blockX, int blockY)
    {
        for (var y = 0; y < 4; y++)
        {
            var alphaRow = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(y * 2, 2));
            var targetY = blockY * 4 + y;
            if (targetY >= height)
            {
                continue;
            }

            for (var x = 0; x < 4; x++)
            {
                var targetX = blockX * 4 + x;
                if (targetX >= width)
                {
                    continue;
                }

                var alpha4 = (alphaRow >> (x * 4)) & 0xF;
                pixels[(targetY * width + targetX) * 4 + 3] = (byte)(alpha4 * 17);
            }
        }
    }

    private static void ApplyBc3Alpha(ReadOnlySpan<byte> block, byte[] pixels, int width, int height, int blockX, int blockY)
    {
        var alpha0 = block[0];
        var alpha1 = block[1];
        var palette = new byte[8];
        palette[0] = alpha0;
        palette[1] = alpha1;

        if (alpha0 > alpha1)
        {
            palette[2] = (byte)((6 * alpha0 + alpha1) / 7);
            palette[3] = (byte)((5 * alpha0 + 2 * alpha1) / 7);
            palette[4] = (byte)((4 * alpha0 + 3 * alpha1) / 7);
            palette[5] = (byte)((3 * alpha0 + 4 * alpha1) / 7);
            palette[6] = (byte)((2 * alpha0 + 5 * alpha1) / 7);
            palette[7] = (byte)((alpha0 + 6 * alpha1) / 7);
        }
        else
        {
            palette[2] = (byte)((4 * alpha0 + alpha1) / 5);
            palette[3] = (byte)((3 * alpha0 + 2 * alpha1) / 5);
            palette[4] = (byte)((2 * alpha0 + 3 * alpha1) / 5);
            palette[5] = (byte)((alpha0 + 4 * alpha1) / 5);
            palette[6] = 0;
            palette[7] = 255;
        }

        ulong indices = 0;
        for (var i = 0; i < 6; i++)
        {
            indices |= (ulong)block[2 + i] << (8 * i);
        }

        for (var y = 0; y < 4; y++)
        {
            var targetY = blockY * 4 + y;
            if (targetY >= height)
            {
                continue;
            }

            for (var x = 0; x < 4; x++)
            {
                var targetX = blockX * 4 + x;
                if (targetX >= width)
                {
                    continue;
                }

                var alphaIndex = (int)((indices >> (3 * (y * 4 + x))) & 0x7);
                pixels[(targetY * width + targetX) * 4 + 3] = palette[alphaIndex];
            }
        }
    }

    private static void DecodeBgra32(ReadOnlySpan<byte> data, byte[] pixels, bool isGcmFormat)
    {
        for (var sourceIndex = 0; sourceIndex < pixels.Length; sourceIndex += 4)
        {
            if (isGcmFormat)
            {
                pixels[sourceIndex] = data[sourceIndex + 3];
                pixels[sourceIndex + 1] = data[sourceIndex + 2];
                pixels[sourceIndex + 2] = data[sourceIndex + 1];
                pixels[sourceIndex + 3] = data[sourceIndex];
                continue;
            }

            pixels[sourceIndex] = data[sourceIndex];
            pixels[sourceIndex + 1] = data[sourceIndex + 1];
            pixels[sourceIndex + 2] = data[sourceIndex + 2];
            pixels[sourceIndex + 3] = data[sourceIndex + 3];
        }
    }

    private static void DecodeBgrx32(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var sourceIndex = 0; sourceIndex < pixels.Length; sourceIndex += 4)
        {
            pixels[sourceIndex] = data[sourceIndex];
            pixels[sourceIndex + 1] = data[sourceIndex + 1];
            pixels[sourceIndex + 2] = data[sourceIndex + 2];
            pixels[sourceIndex + 3] = 255;
        }
    }

    private static void DecodeRgb565(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < pixels.Length / 4; pixelIndex++)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pixelIndex * 2, 2));
            SetPixel(pixels, pixelIndex, Rgb565ToColor(value));
        }
    }

    private static void DecodeA1Rgb555(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < pixels.Length / 4; pixelIndex++)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pixelIndex * 2, 2));
            var alpha = (value & 0x8000) == 0 ? (byte)0 : (byte)255;
            var r = Expand5((value >> 10) & 0x1F);
            var g = Expand5((value >> 5) & 0x1F);
            var b = Expand5(value & 0x1F);
            SetPixel(pixels, pixelIndex, new BgraColor(b, g, r, alpha));
        }
    }

    private static void DecodeArgb4444(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < pixels.Length / 4; pixelIndex++)
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pixelIndex * 2, 2));
            var a = Expand4((value >> 12) & 0xF);
            var r = Expand4((value >> 8) & 0xF);
            var g = Expand4((value >> 4) & 0xF);
            var b = Expand4(value & 0xF);
            SetPixel(pixels, pixelIndex, new BgraColor(b, g, r, a));
        }
    }

    private static void DecodeAlpha8(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < data.Length; pixelIndex++)
        {
            SetPixel(pixels, pixelIndex, new BgraColor(255, 255, 255, data[pixelIndex]));
        }
    }

    private static void DecodeLuminance8(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < data.Length; pixelIndex++)
        {
            var luminance = data[pixelIndex];
            SetPixel(pixels, pixelIndex, new BgraColor(luminance, luminance, luminance, 255));
        }
    }

    private static void DecodeAlphaLuminance8(ReadOnlySpan<byte> data, byte[] pixels)
    {
        for (var pixelIndex = 0; pixelIndex < pixels.Length / 4; pixelIndex++)
        {
            var luminance = data[pixelIndex * 2];
            var alpha = data[pixelIndex * 2 + 1];
            SetPixel(pixels, pixelIndex, new BgraColor(luminance, luminance, luminance, alpha));
        }
    }

    private static BgraColor Rgb565ToColor(ushort color)
    {
        return new BgraColor(
            Expand5(color & 0x1F),
            Expand6((color >> 5) & 0x3F),
            Expand5((color >> 11) & 0x1F),
            255);
    }

    private static void SetPixel(byte[] pixels, int width, int x, int y, BgraColor color)
    {
        SetPixel(pixels, y * width + x, color);
    }

    private static void SetPixel(byte[] pixels, int pixelIndex, BgraColor color)
    {
        var index = pixelIndex * 4;
        pixels[index] = color.B;
        pixels[index + 1] = color.G;
        pixels[index + 2] = color.R;
        pixels[index + 3] = color.A;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool bigEndian)
    {
        return bigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2))
            : BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool bigEndian)
    {
        return bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4))
            : BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static byte Expand4(int value)
    {
        return (byte)((value << 4) | value);
    }

    private static byte Expand5(int value)
    {
        return (byte)((value << 3) | (value >> 2));
    }

    private static byte Expand6(int value)
    {
        return (byte)((value << 2) | (value >> 4));
    }

    private static bool IsGcmFormat(int format)
    {
        return format is >= 0x80 and <= 0xFF;
    }

    private static Bitmap CreateBitmap(byte[] pixels, int width, int height)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        using var framebuffer = bitmap.Lock();
        var sourceRowBytes = width * 4;

        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(
                pixels,
                y * sourceRowBytes,
                IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes),
                sourceRowBytes);
        }

        return bitmap;
    }

    private readonly record struct BgraColor(byte B, byte G, byte R, byte A);
}
