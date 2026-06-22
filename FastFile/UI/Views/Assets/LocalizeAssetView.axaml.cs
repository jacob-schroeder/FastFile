using Avalonia.Controls;
using FastFile.ModelsOLD.Assets.Localize;

namespace UI.Views.Assets;

public partial class LocalizeAssetView : UserControl
{
    public LocalizeAssetView()
    {
        InitializeComponent();
    }

    public LocalizeAssetView(LocalizeEntry asset) : this()
    {
        NameTextBox.Text = asset.Name;
        ValueTextBox.Text = asset.Value;
    }
}
