using Avalonia.Controls;
using FastFile.Models.Assets.XModels;

namespace UI.Views.Assets;

public partial class XModelViewerWindow : Window
{
    public XModelViewerWindow()
    {
        InitializeComponent();
    }

    public XModelViewerWindow(XModel model, string weaponName) : this()
    {
        var modelName = string.IsNullOrWhiteSpace(model.Name)
            ? weaponName
            : model.Name;

        Title = $"XModel Viewer - {modelName}";
        ViewerContent.SetModel(model);
    }
}
