namespace UI.Models;

public sealed class AssetGroupDisplayItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public DisplayItem[] Assets { get; set; } = [];

    public bool IsExpanded { get; set; } = true;

    public bool IsCollapsed => !IsExpanded;
}
