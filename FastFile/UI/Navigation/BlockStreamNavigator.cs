using Avalonia.Controls;
using UI.Models;

namespace UI.Navigation;

internal static class BlockStreamNavigator
{
    public static void Navigate(Control source, BlockStreamNavigationTarget? target)
    {
        if (target is null)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(source);
        if (topLevel is MainWindow mainWindow)
        {
            mainWindow.NavigateToBlockStream(target.Block, target.Offset);
            return;
        }

        if (topLevel is not Window window)
        {
            return;
        }

        for (var owner = window.Owner; owner is not null; owner = owner.Owner)
        {
            if (owner is MainWindow ownerMainWindow)
            {
                ownerMainWindow.NavigateToBlockStream(target.Block, target.Offset);
                return;
            }
        }
    }
}
