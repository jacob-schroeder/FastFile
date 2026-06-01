using Avalonia.Controls;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Data;

namespace UI.Views.Assets;

public partial class LocalizeAssetView : UserControl
{
    public LocalizeAssetView()
    {
        InitializeComponent();
    }

    public LocalizeAssetView(LocalizeEntry asset) : this()
    {
        var nameIsExternal = asset.NamePtr is { Kind: PointerKind.Offset };

        NameTextBox.Text = nameIsExternal ? "[EXTERNAL]" : asset.Name;
        NameTextBox.IsReadOnly = nameIsExternal;
        ValueTextBox.Text = asset.Value;
    }
}
