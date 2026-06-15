using Avalonia.Media.Imaging;

namespace UI.Views.Assets;

internal sealed class ImageDecodeResult(
    Bitmap? bitmap,
    string format,
    string status,
    byte[]? bgraPixels = null,
    int width = 0,
    int height = 0)
{
    public Bitmap? Bitmap { get; } = bitmap;

    public string Format { get; } = format;

    public string Status { get; } = status;

    public byte[]? BgraPixels { get; } = bgraPixels;

    public int Width { get; } = width;

    public int Height { get; } = height;

    public bool HasBitmap => Bitmap is not null;
}
