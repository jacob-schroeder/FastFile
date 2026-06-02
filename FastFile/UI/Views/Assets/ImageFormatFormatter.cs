using System;
using System.Globalization;

namespace UI.Views.Assets;

internal static class ImageFormatFormatter
{
    public const int D3dFormatA8R8G8B8 = 21;
    public const int D3dFormatX8R8G8B8 = 22;
    public const int D3dFormatR5G6B5 = 23;
    public const int D3dFormatA1R5G5B5 = 25;
    public const int D3dFormatA4R4G4B4 = 26;
    public const int D3dFormatA8 = 28;
    public const int D3dFormatL8 = 50;
    public const int D3dFormatA8L8 = 51;
    public const int D3dFormatDxt1Le = 0x31545844;
    public const int D3dFormatDxt3Le = 0x33545844;
    public const int D3dFormatDxt5Le = 0x35545844;
    public const int D3dFormatDxt1Be = 0x44585431;
    public const int D3dFormatDxt3Be = 0x44585433;
    public const int D3dFormatDxt5Be = 0x44585435;
    public const int GcmFormatA8R8G8B8 = 0x85;
    public const int GcmFormatDxt1 = 0x86;
    public const int GcmFormatDxt23 = 0x87;
    public const int GcmFormatDxt45 = 0x88;

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
            D3dFormatA8R8G8B8 => ImagePixelFormat.Bgra32,
            D3dFormatX8R8G8B8 => ImagePixelFormat.Bgrx32,
            D3dFormatR5G6B5 => ImagePixelFormat.Rgb565,
            D3dFormatA1R5G5B5 => ImagePixelFormat.A1Rgb555,
            D3dFormatA4R4G4B4 => ImagePixelFormat.Argb4444,
            D3dFormatA8 => ImagePixelFormat.Alpha8,
            D3dFormatL8 => ImagePixelFormat.Luminance8,
            D3dFormatA8L8 => ImagePixelFormat.AlphaLuminance8,
            D3dFormatDxt1Le or D3dFormatDxt1Be or GcmFormatDxt1 => ImagePixelFormat.Bc1,
            D3dFormatDxt3Le or D3dFormatDxt3Be or GcmFormatDxt23 => ImagePixelFormat.Bc2,
            D3dFormatDxt5Le or D3dFormatDxt5Be or GcmFormatDxt45 => ImagePixelFormat.Bc3,
            GcmFormatA8R8G8B8 => ImagePixelFormat.Bgra32,
            _ => ImagePixelFormat.Unknown
        };
    }

    private static string? GetName(int format)
    {
        return StripGcmFlags(format) switch
        {
            D3dFormatA8R8G8B8 => "D3DFMT_A8R8G8B8",
            D3dFormatX8R8G8B8 => "D3DFMT_X8R8G8B8",
            D3dFormatR5G6B5 => "D3DFMT_R5G6B5",
            D3dFormatA1R5G5B5 => "D3DFMT_A1R5G5B5",
            D3dFormatA4R4G4B4 => "D3DFMT_A4R4G4B4",
            D3dFormatA8 => "D3DFMT_A8",
            D3dFormatL8 => "D3DFMT_L8",
            D3dFormatA8L8 => "D3DFMT_A8L8",
            D3dFormatDxt1Le or D3dFormatDxt1Be => "D3DFMT_DXT1",
            D3dFormatDxt3Le or D3dFormatDxt3Be => "D3DFMT_DXT3",
            D3dFormatDxt5Le or D3dFormatDxt5Be => "D3DFMT_DXT5",
            GcmFormatA8R8G8B8 => "CELL_GCM_TEXTURE_A8R8G8B8",
            GcmFormatDxt1 => "CELL_GCM_TEXTURE_COMPRESSED_DXT1",
            GcmFormatDxt23 => "CELL_GCM_TEXTURE_COMPRESSED_DXT23",
            GcmFormatDxt45 => "CELL_GCM_TEXTURE_COMPRESSED_DXT45",
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
