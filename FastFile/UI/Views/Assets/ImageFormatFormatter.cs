using System;
using System.Globalization;
using FastFile.ModelsOLD.Assets.Material;

namespace UI.Views.Assets;

internal static class ImageFormatFormatter
{
    public static string Format(int format)
    {
        return GetName(format) is { } name
            ? $"{name} (0x{format:X8})"
            : $"Unknown (0x{format:X8}, {format.ToString(CultureInfo.CurrentCulture)})";
    }

    public static ImagePixelFormat Resolve(int format)
    {
        return StripGcmFlags(format) switch
        {
            GfxImageFormats.D3dFormatA8R8G8B8 => ImagePixelFormat.Bgra32,
            GfxImageFormats.D3dFormatX8R8G8B8 => ImagePixelFormat.Bgrx32,
            GfxImageFormats.D3dFormatR5G6B5 => ImagePixelFormat.Rgb565,
            GfxImageFormats.D3dFormatA1R5G5B5 => ImagePixelFormat.A1Rgb555,
            GfxImageFormats.D3dFormatA4R4G4B4 => ImagePixelFormat.Argb4444,
            GfxImageFormats.D3dFormatA8 => ImagePixelFormat.Alpha8,
            GfxImageFormats.D3dFormatL8 => ImagePixelFormat.Luminance8,
            GfxImageFormats.D3dFormatA8L8 => ImagePixelFormat.AlphaLuminance8,
            GfxImageFormats.D3dFormatDxt1Le or GfxImageFormats.D3dFormatDxt1Be or GfxImageFormats.GcmFormatDxt1 => ImagePixelFormat.Bc1,
            GfxImageFormats.D3dFormatDxt3Le or GfxImageFormats.D3dFormatDxt3Be or GfxImageFormats.GcmFormatDxt23 => ImagePixelFormat.Bc2,
            GfxImageFormats.D3dFormatDxt5Le or GfxImageFormats.D3dFormatDxt5Be or GfxImageFormats.GcmFormatDxt45 => ImagePixelFormat.Bc3,
            GfxImageFormats.GcmFormatA8R8G8B8 => ImagePixelFormat.Bgra32,
            _ => ImagePixelFormat.Unknown
        };
    }

    private static string? GetName(int format)
    {
        return StripGcmFlags(format) switch
        {
            GfxImageFormats.D3dFormatA8R8G8B8 => "D3DFMT_A8R8G8B8",
            GfxImageFormats.D3dFormatX8R8G8B8 => "D3DFMT_X8R8G8B8",
            GfxImageFormats.D3dFormatR5G6B5 => "D3DFMT_R5G6B5",
            GfxImageFormats.D3dFormatA1R5G5B5 => "D3DFMT_A1R5G5B5",
            GfxImageFormats.D3dFormatA4R4G4B4 => "D3DFMT_A4R4G4B4",
            GfxImageFormats.D3dFormatA8 => "D3DFMT_A8",
            GfxImageFormats.D3dFormatL8 => "D3DFMT_L8",
            GfxImageFormats.D3dFormatA8L8 => "D3DFMT_A8L8",
            GfxImageFormats.D3dFormatDxt1Le or GfxImageFormats.D3dFormatDxt1Be => "D3DFMT_DXT1",
            GfxImageFormats.D3dFormatDxt3Le or GfxImageFormats.D3dFormatDxt3Be => "D3DFMT_DXT3",
            GfxImageFormats.D3dFormatDxt5Le or GfxImageFormats.D3dFormatDxt5Be => "D3DFMT_DXT5",
            GfxImageFormats.GcmFormatA8R8G8B8 => "CELL_GCM_TEXTURE_A8R8G8B8",
            GfxImageFormats.GcmFormatDxt1 => "CELL_GCM_TEXTURE_COMPRESSED_DXT1",
            GfxImageFormats.GcmFormatDxt23 => "CELL_GCM_TEXTURE_COMPRESSED_DXT23",
            GfxImageFormats.GcmFormatDxt45 => "CELL_GCM_TEXTURE_COMPRESSED_DXT45",
            _ => null
        };
    }

    private static int StripGcmFlags(int format)
    {
        return format is >= 0x80 and <= 0xFF
            ? format & 0x9F
            : format;
    }
}

internal enum ImagePixelFormat
{
    Unknown,
    Bgra32,
    Bgrx32,
    Rgb565,
    A1Rgb555,
    Argb4444,
    Alpha8,
    Luminance8,
    AlphaLuminance8,
    Bc1,
    Bc2,
    Bc3
}
