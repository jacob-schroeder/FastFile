using Avalonia.Controls;
using FastFile.Logic.Compression;
using FastFile.Models.Assets.RawFiles;
using System.Text;
using UI.Views.Assets.Highlighting;

namespace UI.Views.Assets;

public partial class RawfileAssetView : UserControl
{
    public RawfileAssetView()
    {
        InitializeComponent();
        InitializeEditor(fileName: null);
    }

    public RawfileAssetView(RawFile asset)
    {
        InitializeComponent();
        InitializeEditor(asset.Name);
        RawFileTextEditor.Text = ReadFileText(asset);
    }

    private static string ReadFileText(RawFile asset)
    {
        if (asset.Len == 0)
        {
            return string.Empty;
        }

        if (asset.CompressedLen == 0)
        {
            return Encoding.UTF8.GetString(asset.Buffer);
        }

        var decompressed = ZLib.DecompressZlib(asset.Buffer);
        return Encoding.UTF8.GetString(decompressed);
    }

    private void InitializeEditor(string? fileName)
    {
        RawFileTextEditor.SyntaxHighlighting = RawfileSyntaxHighlighting.GetDefinition(fileName);
        RawFileTextEditor.Options.ConvertTabsToSpaces = false;
        RawFileTextEditor.Options.EnableHyperlinks = false;
        RawFileTextEditor.Options.HighlightCurrentLine = true;
        RawFileTextEditor.Options.IndentationSize = 4;
        RawFileTextEditor.TextArea.Caret.CaretBrush = Avalonia.Media.Brushes.White;
    }
}
