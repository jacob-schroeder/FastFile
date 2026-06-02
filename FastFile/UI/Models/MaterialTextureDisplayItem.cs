using Avalonia.Media.Imaging;

namespace UI.Models;

public sealed class MaterialTextureDisplayItem
{
    public string Index { get; init; } = string.Empty;

    public string Semantic { get; init; } = string.Empty;

    public string ImageName { get; init; } = string.Empty;

    public string ImageSize { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Pointer { get; init; } = string.Empty;

    public string NameHash { get; init; } = string.Empty;

    public string SampleState { get; init; } = string.Empty;

    public Bitmap? Preview { get; init; }

    public bool HasPreview => Preview is not null;

    public bool HasNoPreview => Preview is null;
}
