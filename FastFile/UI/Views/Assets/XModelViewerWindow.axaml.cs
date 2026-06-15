using Avalonia.Controls;
using FastFile.Models.Assets.XModels;
using System;

namespace UI.Views.Assets;

public partial class XModelViewerWindow : Window
{
    public XModelViewerWindow()
    {
        InitializeComponent();
    }

    public XModelViewerWindow(XModel model, string? contextName = null) : this()
    {
        var modelName = XModelPreviewHelper.GetDisplayName(model);
        var titleName = string.IsNullOrWhiteSpace(contextName) ||
                        string.Equals(contextName, modelName, StringComparison.Ordinal)
            ? modelName
            : $"{contextName} - {modelName}";

        Title = $"XModel Viewer - {titleName}";
        ViewerContent.SetModel(model);
    }
}
