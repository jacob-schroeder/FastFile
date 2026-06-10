using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Logic.Compression;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Data;
using System.Text;
using FastFile.Models.Zone;
using UI.Views.Assets.Highlighting;

namespace UI.Views.Assets;

public partial class RawfileAssetView : UserControl
{
    private RawFile? _asset;

    public RawfileAssetView()
    {
        InitializeComponent();
        InitializeEditor(fileName: null);
        SaveRawFileButton.IsEnabled = false;
    }

    public RawfileAssetView(RawFile asset)
    {
        InitializeComponent();
        _asset = asset;
        InitializeEditor(asset.Name);
        RawFileTextEditor.Text = ReadFileText(asset);
    }

    private void SaveRawFileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_asset is null)
            return;

        var decompressed = Encoding.UTF8.GetBytes(RawFileTextEditor.Text ?? string.Empty);
        var compressed = ZLib.Compress(decompressed);

        _asset.Len = decompressed.Length;
        _asset.CompressedLen = compressed.Length;

        if (_asset.BufferPtr is null)
            _asset.BufferPtr = new XPointer<byte[]>
            {
                Raw = -1, 
                Kind = PointerKind.Inline, 
                ResolutionKind = PointerResolutionKind.Direct
            };

        _asset.BufferPtr.Value = compressed;
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

        var decompressed = ZLib.Decompress(asset.Buffer);
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
