using Avalonia.Controls;

namespace UI.Components.Menu;

public partial class MenuItemDetailsWindow : Window
{
    public MenuItemDetailsWindow()
    {
        InitializeComponent();
    }

    public MenuItemDetailsWindow(MenuItemDefDisplayItem item) : this()
    {
        Title = string.IsNullOrWhiteSpace(item.Name)
            ? "Menu Item Details"
            : $"Menu Item Details - {item.Name}";
        MenuItemDetailsControllerView.SetItem(item);
    }
}
