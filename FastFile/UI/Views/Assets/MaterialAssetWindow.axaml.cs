using Avalonia.Controls;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;

namespace UI.Views.Assets;

public partial class MaterialAssetWindow : Window
{
    public MaterialAssetWindow()
    {
        InitializeComponent();
    }

    public MaterialAssetWindow(MaterialAsset material) : this()
    {
        var title = string.IsNullOrWhiteSpace(material.GetDisplayName)
            ? "Material Details"
            : $"Material Details - {material.GetDisplayName}";

        Title = title;
        Content = new MaterialAssetView(material);
    }
}
