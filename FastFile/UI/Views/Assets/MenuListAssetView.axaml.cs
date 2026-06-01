using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using System.Linq;
using UI.Components.Menu;
using UI.Models;

namespace UI.Views.Assets;

public partial class MenuListAssetView : UserControl
{
    public MenuListAssetView()
    {
        InitializeComponent();
    }

    public MenuListAssetView(MenuList asset) : this()
    {
        MenuListNameTextBlock.Text = GetMenuListName(asset);
        MenuListCountTextBlock.Text = $"{asset.MenuCount:N0} menus";
        MenuListEmptyTextBlock.Text = asset.Menus is { Kind: PointerKind.Offset }
            ? "[EXTERNAL]"
            : "No menus available.";

        var menus = asset.Menus is { IsResolved: true, Result: not null }
            ? asset.Menus.Result
                .Select((menu, index) => new MenuListMenuDisplayItem(index, menu))
                .ToArray()
            : [];

        MenusItemsControl.ItemsSource = menus;
        MenuListTableScrollViewer.IsVisible = menus.Length > 0;
        MenuListEmptyTextBlock.IsVisible = menus.Length == 0;
    }

    private void MenuRow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MenuListMenuDisplayItem { Menu: not null } menuItem })
        {
            return;
        }

        var window = new MenuDetailsWindow(menuItem.Menu, menuItem.Name);
        if (VisualRoot is Window owner)
        {
            window.Show(owner);
            return;
        }

        window.Show();
    }

    private static string GetMenuListName(MenuList asset)
    {
        if (asset.NamePtr is { Kind: PointerKind.Offset })
        {
            return "[EXTERNAL]";
        }

        return string.IsNullOrWhiteSpace(asset.GetDisplayName)
            ? "(unnamed menu list)"
            : asset.GetDisplayName;
    }
}
