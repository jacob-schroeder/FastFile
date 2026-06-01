using Avalonia.Controls;
using FastFile.Models.Assets.Menu;

namespace UI.Components.Menu;

public partial class MenuDetailsWindow : Window
{
    public MenuDetailsWindow()
    {
        InitializeComponent();
    }

    public MenuDetailsWindow(MenuDef menu, string title) : this()
    {
        Title = string.IsNullOrWhiteSpace(title)
            ? "Menu Details"
            : $"Menu Details - {title}";
        MenuControllerView.SetMenu(menu);
    }
}
