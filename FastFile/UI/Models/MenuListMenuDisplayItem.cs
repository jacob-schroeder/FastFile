using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using System.Globalization;

namespace UI.Models;

public sealed class MenuListMenuDisplayItem
{
    private const string ExternalPointerText = "[EXTERNAL]";
    private const string NullPointerText = "[NULL]";
    private const string UnresolvedPointerText = "[UNRESOLVED]";

    public MenuListMenuDisplayItem(int index, ZonePointer<MenuDef>? menuPointer)
    {
        Index = index + 1;

        if (menuPointer is null)
        {
            SetUnavailableValue(NullPointerText);
            return;
        }

        Pointer = FormatPointer(menuPointer);

        if (!menuPointer.IsResolved || menuPointer.Result is null)
        {
            SetUnavailableValue(Pointer);
            return;
        }

        var menu = menuPointer.Result;
        Name = FormatStringPointer(menu.Window?.NamePtr, menu.Window?.Name, "(unnamed menu)");
        ItemCount = menu.ItemCount.ToString("N0", CultureInfo.CurrentCulture);
        Font = FormatStringPointer(menu.FontPtr, menu.Font, string.Empty);
        Fullscreen = menu.Fullscreen == 0 ? "No" : "Yes";
        Rect = FormatRectangle(menu.Window?.Rect);
    }

    public int Index { get; }

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
            PointerKind.Offset => ExternalPointerText,
            PointerKind.Null => NullPointerText,
            PointerKind.Inline => pointer.Raw is -1 or -2 ? "Inline" : UnresolvedPointerText,
            _ => UnresolvedPointerText
        };
    }

    private static string FormatStringPointer(ZonePointer<string>? pointer, string? value, string emptyValue)
    {
        if (pointer is { Kind: PointerKind.Offset })
        {
            return ExternalPointerText;
        }

        return string.IsNullOrWhiteSpace(value)
            ? emptyValue
            : value;
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
