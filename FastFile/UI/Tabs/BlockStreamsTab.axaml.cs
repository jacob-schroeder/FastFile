using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FastFile.Models.Zone;
using UI.Models;

namespace UI.Tabs;

public partial class BlockStreamsTab : UserControl
{
    private BlockStreamDisplayItem[] _streams = [];
    private byte[] _visibleBlockData = [];
    private int _hexRowCount;

    public BlockStreamsTab()
    {
        InitializeComponent();
        InitializeHexEditor();
        UpdateBlockStreams(null, []);
    }

    public void UpdateBlockStreams(
        XFile? xfile,
        IReadOnlyList<XBlockStreamSnapshot>? blockStreams)
    {
        if (xfile is null)
        {
            _streams = [];
            BlockStreamsSubtitleTextBlock.Text = "Open a fastfile.";
            BlockStreamsListBox.ItemsSource = _streams;
            ShowSelectedBlock(null);
            return;
        }

        var snapshotsByBlock = (blockStreams ?? Array.Empty<XBlockStreamSnapshot>())
            .ToDictionary(stream => stream.Block);

        var blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
        _streams = Enumerable
            .Range(0, blockCount)
            .Select(index =>
            {
                var block = (XFILE_BLOCK)index;
                var declaredSize = index < xfile.BlockSize.Length ? xfile.BlockSize[index] : 0;
                snapshotsByBlock.TryGetValue(block, out var snapshot);
                var data = snapshot?.Data ?? Array.Empty<byte>();
                var actualDeclaredSize = snapshot?.DeclaredSize ?? declaredSize;

                return new BlockStreamDisplayItem
                {
                    Block = block,
                    Index = index,
                    Name = block.ToString(),
                    SizeText = FormatBytes(data.Length),
                    DeclaredSizeText = $"Declared {FormatBytes(actualDeclaredSize)}",
                    Data = data
                };
            })
            .ToArray();

        var nonEmptyCount = _streams.Count(stream => stream.Data.Length > 0);
        BlockStreamsSubtitleTextBlock.Text = $"{nonEmptyCount:N0} non-empty streams.";

        BlockStreamsListBox.ItemsSource = _streams;
        BlockStreamsListBox.SelectedItem = _streams.FirstOrDefault(stream => stream.Data.Length > 0)
            ?? _streams.FirstOrDefault();
    }

    private void BlockStreamsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ShowSelectedBlock(BlockStreamsListBox.SelectedItem as BlockStreamDisplayItem);
    }

    private void ShowSelectedBlock(BlockStreamDisplayItem? stream)
    {
        if (stream is null)
        {
            SelectedBlockNameTextBlock.Text = "Select a block stream";
            SelectedBlockSizeTextBlock.Text = "Offsets, raw data, and ASCII will appear here.";
            SelectedBlockDeclaredSizeTextBlock.Text = "0 bytes";
            SetHexDump(Array.Empty<byte>());
            EmptyHexTextBlock.Text = "Open a fastfile and select a block stream.";
            EmptyHexTextBlock.IsVisible = true;
            return;
        }

        SelectedBlockNameTextBlock.Text = $"{stream.Index} - {stream.Name}";
        SelectedBlockSizeTextBlock.Text = $"{FormatBytes(stream.Data.Length)} materialized in this block stream.";
        SelectedBlockDeclaredSizeTextBlock.Text = stream.DeclaredSizeText;

        SetHexDump(stream.Data);
        EmptyHexTextBlock.Text = stream.Data.Length == 0
            ? "This block stream is empty."
            : string.Empty;
        EmptyHexTextBlock.IsVisible = stream.Data.Length == 0;
    }

    private void InitializeHexEditor()
    {
        RawDataTextEditor.Options.EnableHyperlinks = false;
        RawDataTextEditor.Options.HighlightCurrentLine = false;
        RawDataTextEditor.TextArea.Caret.CaretBrush = Brushes.White;
        RawDataTextEditor.TextArea.TextView.ScrollOffsetChanged += (_, _) => UpdateVisibleSideColumns();
        RawDataTextEditor.TextArea.TextView.VisualLinesChanged += (_, _) => UpdateVisibleSideColumns();
        RawDataTextEditor.SizeChanged += (_, _) => UpdateVisibleSideColumns();
    }

    private void SetHexDump(byte[] data)
    {
        _visibleBlockData = data;
        _hexRowCount = (data.Length + 15) / 16;

        RawDataTextEditor.Text = CreateRawDataDump(data);
        OffsetTextBlock.Text = string.Empty;
        AsciiTextBlock.Text = string.Empty;
        RawDataTextEditor.ScrollToHome();

        Dispatcher.UIThread.Post(UpdateVisibleSideColumns, DispatcherPriority.Background);
    }

    private void UpdateVisibleSideColumns()
    {
        if (_hexRowCount == 0 || _visibleBlockData.Length == 0)
        {
            OffsetTextBlock.Text = string.Empty;
            AsciiTextBlock.Text = string.Empty;
            return;
        }

        var textView = RawDataTextEditor.TextArea.TextView;
        var lineHeight = textView.DefaultLineHeight;
        if (lineHeight <= 0)
            return;

        var firstLine = textView.GetDocumentLineByVisualTop(textView.ScrollOffset.Y)?.LineNumber ?? 1;
        var firstRow = Math.Clamp(firstLine - 1, 0, _hexRowCount - 1);
        var visibleRows = Math.Min(
            _hexRowCount - firstRow,
            Math.Max(1, (int)Math.Ceiling(textView.Bounds.Height / lineHeight) + 2));
        var visualTop = textView.GetVisualTopByDocumentLine(firstLine);
        var topDelta = visualTop - textView.ScrollOffset.Y;

        OffsetTextBlock.LineHeight = lineHeight;
        AsciiTextBlock.LineHeight = lineHeight;
        OffsetTextBlock.Margin = new Avalonia.Thickness(0, topDelta, 0, 0);
        AsciiTextBlock.Margin = new Avalonia.Thickness(0, topDelta, 0, 0);

        OffsetTextBlock.Text = CreateOffsetWindow(firstRow, visibleRows);
        AsciiTextBlock.Text = CreateAsciiWindow(_visibleBlockData, firstRow, visibleRows);
    }

    private static string CreateRawDataDump(byte[] data)
    {
        if (data.Length == 0)
            return string.Empty;

        var rowCount = (data.Length + 15) / 16;
        var rawDataBuilder = new StringBuilder(capacity: rowCount * 50);

        for (var row = 0; row < rowCount; row++)
        {
            var offset = row * 16;
            var count = Math.Min(16, data.Length - offset);

            rawDataBuilder.Append(FormatHexBytes(data, offset, count));

            if (row != rowCount - 1)
                rawDataBuilder.AppendLine();
        }

        return rawDataBuilder.ToString();
    }

    private static string CreateOffsetWindow(int firstRow, int visibleRows)
    {
        var builder = new StringBuilder(capacity: visibleRows * 11);

        for (var row = 0; row < visibleRows; row++)
        {
            builder
                .Append("0x")
                .Append(((firstRow + row) * 16).ToString("X8", CultureInfo.InvariantCulture));

            if (row != visibleRows - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string CreateAsciiWindow(byte[] data, int firstRow, int visibleRows)
    {
        var builder = new StringBuilder(capacity: visibleRows * 17);

        for (var row = 0; row < visibleRows; row++)
        {
            var offset = (firstRow + row) * 16;
            var count = Math.Min(16, data.Length - offset);

            if (count <= 0)
                break;

            builder.Append(FormatAscii(data, offset, count));

            if (row != visibleRows - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatHexBytes(byte[] data, int offset, int count)
    {
        var builder = new StringBuilder(capacity: 50);

        for (var i = 0; i < 16; i++)
        {
            if (i < count)
                builder.Append(data[offset + i].ToString("X2", CultureInfo.InvariantCulture));
            else
                builder.Append("  ");

            if (i is 3 or 7 or 11)
                builder.Append("  ");
            else if (i != 15)
                builder.Append(' ');
        }

        return builder.ToString();
    }

    private static string FormatAscii(byte[] data, int offset, int count)
    {
        Span<char> chars = stackalloc char[16];

        for (var i = 0; i < 16; i++)
        {
            if (i >= count)
            {
                chars[i] = ' ';
                continue;
            }

            var value = data[offset + i];
            chars[i] = value is >= 0x20 and <= 0x7E
                ? (char)value
                : '.';
        }

        return new string(chars);
    }

    private static string FormatBytes(long value)
    {
        return $"{value.ToString("N0", CultureInfo.CurrentCulture)} bytes (0x{value:X})";
    }

}
