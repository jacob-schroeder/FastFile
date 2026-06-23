using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class MenuFileAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public int MenuCount { get; init; }
    public XPointer<XPointer<MenuDefAsset>[]> MenusPointer { get; init; }
    public IReadOnlyList<MenuDefReference> Menus { get; init; } = [];
}

public sealed record MenuDefReference(
    int Index,
    XPointer<MenuDefAsset> Pointer,
    MenuDefAsset? Menu);
