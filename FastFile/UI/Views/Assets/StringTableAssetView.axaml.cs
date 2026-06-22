using Avalonia.Controls;
using FastFile.ModelsOLD.Assets.StringTables;
using System.Linq;
using UI.Models;

namespace UI.Views.Assets;

public partial class StringTableAssetView : UserControl
{
    public StringTableAssetView()
    {
        InitializeComponent();
    }

    public StringTableAssetView(StringTable asset) : this()
    {
        StringTableRowsListBox.ItemsSource = Enumerable
            .Range(0, asset.RowCount)
            .Select(row => new StringTableRowDisplayItem(asset, row))
            .ToArray();
    }
}
