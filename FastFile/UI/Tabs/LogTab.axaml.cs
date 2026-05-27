using Avalonia.Controls;

namespace UI.Tabs;

public partial class LogTab : UserControl
{
    public LogTab()
    {
        InitializeComponent();
    }

    public void SetLogText(string text)
    {
        LogTextBlock.Text = text;
    }
}
