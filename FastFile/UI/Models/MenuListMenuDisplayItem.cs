using FastFile.ModelsOLD.Assets.Menu;
using FastFile.ModelsOLD.Assets.Menu.Elements;
using FastFile.ModelsOLD.Data;
using System.Globalization;
using FastFile.ModelsOLD.Zone;

namespace UI.Models;

public sealed class MenuListMenuDisplayItem
{
    private const string OffsetPointerText = "[OFFSET]";
    private const string NullPointerText = "[NULL]";
    private const string UnresolvedPointerText = "[UNRESOLVED]";

    public MenuListMenuDisplayItem(int index, XPointer<MenuDef>? menuPointer)
    {
        Index = index + 1;

        if (menuPointer is null)
        {
            SetUnavailableValue(NullPointerText);
            return;
        }

        Pointer = FormatPointer(menuPointer);

        if (!menuPointer.IsResolved || menuPointer.Value is null)
        {
            SetUnavailableValue(Pointer);
            return;
        }

        var menu = menuPointer.Value;
        Menu = menu;
        Name = FormatStringPointer(menu.Window?.NamePtr, menu.Window?.Name, "(unnamed menu)");
        ItemCount = menu.ItemCount.ToString("N0", CultureInfo.CurrentCulture);
        Font = FormatStringPointer(menu.FontPtr, menu.Font, string.Empty);
        Fullscreen = menu.Fullscreen == 0 ? "No" : "Yes";
        Rect = FormatRectangle(menu.Window?.Rect);
    }

    public int Index { get; }

    public MenuDef? Menu { get; private set; }

    public bool CanOpen => Menu is not null;

    public string Name { get; private set; } = string.Empty;

    public string ItemCount { get; private set; } = string.Empty;

    public string Font { get; private set; } = string.Empty;

    public string Fullscreen { get; private set; } = string.Empty;

    public string Rect { get; private set; } = string.Empty;

    public string Pointer { get; private set; } = string.Empty;

    private void SetUnavailableValue(string value)
    {
        Name = value;
        ItemCount = value;
        Font = value;
        Fullscreen = value;
        Rect = value;
        Pointer = value;
    }

    private static string FormatPointer(Pointer pointer)
    {
        return pointer.Kind switch
        {
            PointerKind.Offset => OffsetPointerText,
            PointerKind.Null => NullPointerText,
            PointerKind.Inline => "Inline",
            PointerKind.Insert => "Insert",
            _ => UnresolvedPointerText
        };
    }

    private static string FormatStringPointer(XPointer<string>? pointer, string? value, string emptyValue)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return pointer is { Kind: PointerKind.Offset }
            ? OffsetPointerText
            : emptyValue;
    }

    private static string FormatRectangle(RectangleDef? rect)
    {
        return rect is null
            ? string.Empty
            : string.Create(
                CultureInfo.CurrentCulture,
                $"{rect.X:0.##}, {rect.Y:0.##}, {rect.W:0.##}, {rect.H:0.##}");
    }
}
