namespace UI.Models;

using Avalonia;

public sealed class AssetDetailTabItem
{
    public int AssetId { get; init; }

    public string Title { get; init; } = string.Empty;

    public object Content { get; init; } = new();

    public bool IsSelected { get; set; }

    public string HeaderBackground => IsSelected ? "#3A3D42" : "#252629";

    public string HeaderBorderBrush => IsSelected ? "#DB5860" : "#4E5157";

    public Thickness HeaderBorderThickness => new(1, 1, 1, 0);
}
