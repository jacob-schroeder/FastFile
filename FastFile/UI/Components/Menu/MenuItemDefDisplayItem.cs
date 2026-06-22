using FastFile.ModelsOLD.Assets.Menu.Elements;
using FastFile.ModelsOLD.Data;
using System.Globalization;
using FastFile.ModelsOLD.Zone;
using UI.Models;

namespace UI.Components.Menu;

public sealed class MenuItemDefDisplayItem
{
    public MenuItemDefDisplayItem(int index, XPointer<ItemDef>? itemPointer)
    {
        Index = index + 1;

        if (itemPointer is null)
        {
            SetUnavailableValue(MenuDisplayFormatter.NullPointerText);
            return;
        }

        Pointer = MenuDisplayFormatter.FormatPointerRaw(itemPointer);
        PointerNavigationTarget = BlockStreamNavigationTarget.FromPointer(itemPointer);

        if (!itemPointer.IsResolved || itemPointer.Value is null)
        {
            SetUnavailableValue(Pointer);
            return;
        }

        Item = itemPointer.Value;
        Name = MenuDisplayFormatter.FormatStringPointer(
            Item.Window?.NamePtr,
            Item.Window?.Name,
            "(unnamed item)");
        Text = MenuDisplayFormatter.FormatStringPointer(Item.Text, Item.Text?.Value, string.Empty);
        Type = $"{MenuEnumFormatter.FormatItemTypeCompact(Item.Type)} / {MenuEnumFormatter.FormatItemTypeCompact(Item.DataType)}";
        DataType = MenuEnumFormatter.FormatItemTypeCompact(Item.DataType);
        Rect = MenuDisplayFormatter.FormatRectangle(Item.Window?.Rect);
        ClientRect = MenuDisplayFormatter.FormatRectangle(Item.Window?.RectClient);
        Dvar = MenuDisplayFormatter.FormatStringPointer(Item.Dvar, Item.Dvar?.Value, string.Empty);
        EnableDvar = MenuDisplayFormatter.FormatStringPointer(Item.EnableDvar, Item.EnableDvar?.Value, string.Empty);
        FloatExpressions = Item.FloatExpressionCount.ToString("N0", CultureInfo.CurrentCulture);
        Pointer = MenuDisplayFormatter.FormatPointerRaw(itemPointer);
    }

    public int Index { get; }

    public ItemDef? Item { get; private set; }

    public bool CanOpen => Item is not null;

    public string Name { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public string DataType { get; private set; } = string.Empty;

    public string Rect { get; private set; } = string.Empty;

    public string ClientRect { get; private set; } = string.Empty;

    public string Dvar { get; private set; } = string.Empty;

    public string EnableDvar { get; private set; } = string.Empty;

    public string FloatExpressions { get; private set; } = string.Empty;

    public string Pointer { get; private set; } = string.Empty;

    public BlockStreamNavigationTarget? PointerNavigationTarget { get; private set; }

    private void SetUnavailableValue(string value)
    {
        Name = value;
        Text = value;
        Type = value;
        DataType = value;
        Rect = value;
        ClientRect = value;
        Dvar = value;
        EnableDvar = value;
        FloatExpressions = value;
        Pointer = value;
    }
}
