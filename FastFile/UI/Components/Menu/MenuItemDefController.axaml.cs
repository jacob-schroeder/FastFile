using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI.Components.Menu;

public partial class MenuItemDefController : UserControl
{
    public MenuItemDefController()
    {
        InitializeComponent();
    }

    private void MenuItemRow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MenuItemDefDisplayItem { Item: not null } item })
        {
            return;
        }

        var window = new MenuItemDetailsWindow(item);
        if (VisualRoot is Window owner)
        {
            window.Show(owner);
            return;
        }

        window.Show();
    }
}
