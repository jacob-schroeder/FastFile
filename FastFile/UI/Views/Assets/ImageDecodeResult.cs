using Avalonia.Media.Imaging;

namespace UI.Views.Assets;

internal sealed class ImageDecodeResult(
    Bitmap? bitmap,
    string format,
    string status)
{
    public Bitmap? Bitmap { get; } = bitmap;

    public string Format { get; } = format;

    public string Status { get; } = status;

    public bool HasBitmap => Bitmap is not null;
}
