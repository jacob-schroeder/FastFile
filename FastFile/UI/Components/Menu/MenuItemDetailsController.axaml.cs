using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UI.Models;
using UI.Views.Assets;
using MaterialAsset = FastFile.Models.Assets.Material.Material;
using MenuWindow = FastFile.Models.Assets.Menu.Elements.Window;

namespace UI.Components.Menu;

public partial class MenuItemDetailsController : UserControl
{
    public MenuItemDetailsController()
    {
        InitializeComponent();
    }

    public void SetItem(MenuItemDefDisplayItem displayItem)
    {
        var item = displayItem.Item;
        if (item is null)
        {
            return;
        }

        ItemNameTextBlock.Text = displayItem.Name;
        ItemSubtitleTextBlock.Text = $"#{displayItem.Index} | {displayItem.Rect}";
        ItemTypeBadgeTextBlock.Text = displayItem.Type;

        ItemDetailsItemsControl.ItemsSource = BuildItemDetails(displayItem, item);
        ItemWindowItemsControl.ItemsSource = BuildWindowDetails(item.Window);
        ItemPresentationItemsControl.ItemsSource = BuildPresentationDetails(item);
        ItemEventsItemsControl.ItemsSource = MenuBehaviorFormatter.BuildItemEvents(item);

        var expressions = MenuBehaviorFormatter.BuildItemExpressions(item);
        ExpressionTypeComboBox.ItemsSource = expressions;
        ExpressionTypeComboBox.IsVisible = expressions.Length > 0;
        ExpressionEmptyTextBlock.IsVisible = expressions.Length == 0;

        if (expressions.Length > 0)
        {
            ExpressionTypeComboBox.SelectedIndex = 0;
            return;
        }

        UpdateExpressionDisplay(null);
    }

    private void ExpressionTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateExpressionDisplay(ExpressionTypeComboBox.SelectedItem as MenuInsightDisplayItem);
    }

    private void UpdateExpressionDisplay(MenuInsightDisplayItem? expression)
    {
        var hasExpression = expression is not null;
        var hasDetail = expression?.HasDetail == true;

        ExpressionEmptyTextBlock.IsVisible = !hasExpression;
        ExpressionSummaryHeaderTextBlock.IsVisible = hasExpression;
        ExpressionSummaryBorder.IsVisible = hasExpression;
        ExpressionSummaryTextBlock.Text = expression?.Summary ?? string.Empty;
        ExpressionDetailHeaderTextBlock.IsVisible = hasDetail;
        ExpressionDetailBorder.IsVisible = hasDetail;
        ExpressionDetailTextBlock.Text = expression?.Detail ?? string.Empty;
    }

    private static KeyValueListItem[] BuildItemDetails(MenuItemDefDisplayItem displayItem, ItemDef item)
    {
        return
        [
            new("Index", displayItem.Index.ToString("N0", CultureInfo.CurrentCulture)),
            new("Name", displayItem.Name),
            new("Text", displayItem.Text),
            new("Type", MenuEnumFormatter.FormatItemType(item.Type)),
            new("Data Type", MenuEnumFormatter.FormatItemType(item.DataType)),
            new("Parent", MenuDisplayFormatter.FormatPointer(item.Parent)),
            new("Dvar", displayItem.Dvar),
            new("Dvar Test", MenuDisplayFormatter.FormatStringPointer(item.DvarTest, item.DvarTest?.Value, string.Empty)),
            new("Enable Dvar", displayItem.EnableDvar),
            new("Dvar Flags", MenuEnumFormatter.FormatDvarFlags(item.DvarFlags)),
            new("Focus Sound", MenuDisplayFormatter.FormatAssetPointer(item.FocusSound)),
            new("Special", item.Special.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Cursor Position", string.Join(", ", item.CursorPos)),
            new("Image Track", item.ImageTrack.ToString(CultureInfo.CurrentCulture)),
            new("Float Expressions", item.FloatExpressionCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("Pointer", displayItem.Pointer)
        ];
    }

    private static MenuMaterialReferenceDisplayItem[] BuildWindowDetails(MenuWindow? window)
    {
        if (window is null)
        {
            return [new("Window", MenuDisplayFormatter.NullPointerText)];
        }

        return
        [
            new("Name", MenuDisplayFormatter.FormatStringPointer(window.NamePtr, window.Name, "(unnamed item)")),
            new("Group", MenuDisplayFormatter.FormatStringPointer(window.GroupPtr, window.Group, string.Empty)),
            new("Rect", MenuDisplayFormatter.FormatRectangle(window.Rect)),
            new("Client Rect", MenuDisplayFormatter.FormatRectangle(window.RectClient)),
            new("Style", MenuEnumFormatter.FormatWindowStyle(window.Style)),
            new("Border", MenuEnumFormatter.FormatWindowBorder(window.Border)),
            new("Owner Draw", MenuEnumFormatter.FormatOwnerDraw(window.OwnerDraw)),
            new("Owner Flags", $"0x{window.OwnerDrawFlags:X8}"),
            new("Border Size", window.BorderSize.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Static Flags", $"0x{window.StaticFlags:X8}"),
            new("Dynamic Flags", string.Join(", ", window.DynamicFlags)),
            new(
                "Background Material",
                MenuDisplayFormatter.FormatAssetPointer(window.Background),
                window.Background?.Value)
        ];
    }

    private void MaterialViewButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MaterialAsset material })
        {
            return;
        }

        var window = new MaterialAssetWindow(material);
        if (VisualRoot is Avalonia.Controls.Window owner)
        {
            window.Show(owner);
            return;
        }

        window.Show();
    }

    private static MenuMaterialReferenceDisplayItem[] BuildPresentationDetails(ItemDef item)
    {
        var rows = new List<MenuMaterialReferenceDisplayItem>
        {
            new("Text Rect", FormatRectangles(item.TextRect)),
            new("Align", item.Align.ToString(CultureInfo.CurrentCulture)),
            new("Font", item.FontEnum.ToString(CultureInfo.CurrentCulture)),
            new("Text Align Mode", MenuEnumFormatter.FormatTextAlignMode(item.TextAlignMode)),
            new("Text Align Offset", $"{item.TextAlignX:0.###}, {item.TextAlignY:0.###}"),
            new("Text Scale", item.TextScale.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Text Style", MenuEnumFormatter.FormatTextStyle(item.TextStyle)),
            new("Game Msg Window", item.GameMsgWindowIndex.ToString(CultureInfo.CurrentCulture)),
            new("Game Msg Mode", item.GameMsgWindowMode.ToString(CultureInfo.CurrentCulture)),
            new("Text Save Game Info", item.TextSaveGameInfo.ToString(CultureInfo.CurrentCulture)),
            new("Foreground", MenuDisplayFormatter.FormatVec4(item.Window?.ForeColor)),
            new("Background", MenuDisplayFormatter.FormatVec4(item.Window?.BackColor)),
            new("Border Color", MenuDisplayFormatter.FormatVec4(item.Window?.BorderColor)),
            new("Outline Color", MenuDisplayFormatter.FormatVec4(item.Window?.OutlineColor)),
            new("Disable Color", MenuDisplayFormatter.FormatVec4(item.Window?.DisableColor)),
            new("Glow Color", MenuDisplayFormatter.FormatVec4(item.GlowColor)),
            new("Type Data", FormatTypeData(item.TypeData))
        };

        var selectIcon = item.TypeData?.ListBox?.Value?.SelectIcon;
        if (selectIcon is not null && selectIcon.Kind != PointerKind.Null)
        {
            rows.Add(new MenuMaterialReferenceDisplayItem(
                "Select Icon Material",
                MenuDisplayFormatter.FormatAssetPointer(selectIcon),
                selectIcon.Value));
        }

        return rows.ToArray();
    }

    private static string FormatRectangles(RectangleDef[]? rectangles)
    {
        return rectangles is null || rectangles.Length == 0
            ? string.Empty
            : string.Join(" | ", rectangles.Select(MenuDisplayFormatter.FormatRectangle));
    }

    private static string FormatTypeData(ItemDefData? typeData)
    {
        return typeData is null
            ? MenuDisplayFormatter.NullPointerText
            : $"0x{typeData.Raw:X8}";
    }
}
