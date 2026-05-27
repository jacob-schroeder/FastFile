using Avalonia.Controls;
using FastFile.Models;

namespace UI.Tabs;

public partial class FastFileTab : UserControl
{
    public FastFileTab()
    {
        InitializeComponent();
    }

    public void SetStatus(string status)
    {
        StatusTextBlock.Text = status;
    }

    public void UpdateHeader(DB_Header? header)
    {
        if (header is null)
        {
            MagicValueTextBlock.Text = "-";
            VersionValueTextBlock.Text = "-";
            SizeValueTextBlock.Text = "-";
            return;
        }

        MagicValueTextBlock.Text = header.Magic;
        VersionValueTextBlock.Text = header.Version.ToString();
        SizeValueTextBlock.Text = $"{header.FileSize:N0} bytes";
    }
}
