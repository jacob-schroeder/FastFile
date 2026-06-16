using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
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
    private int _visibleAsciiFirstRow;
    private int _visibleAsciiRowCount;
    private bool _syncingSelection;

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

    public void NavigateTo(XFILE_BLOCK block, int offset)
    {
        var stream = _streams.FirstOrDefault(item => item.Block == block);
        if (stream is null)
        {
            ShowOffsetJumpStatus($"Block {block} unavailable");
            return;
        }

        BlockStreamsListBox.SelectedItem = stream;
        OffsetJumpTextBox.Text = $"0x{offset:X8}";
        Dispatcher.UIThread.Post(() => JumpToOffset(offset), DispatcherPriority.Background);
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
        ClearOffsetJumpStatus();
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
        RawDataTextEditor.TextArea.SelectionChanged += (_, _) => RawDataTextEditor_SelectionChanged();
        RawDataTextEditor.SizeChanged += (_, _) => UpdateVisibleSideColumns();

        AsciiTextEditor.Options.EnableHyperlinks = false;
        AsciiTextEditor.Options.HighlightCurrentLine = false;
        AsciiTextEditor.TextArea.Caret.CaretBrush = Brushes.White;
        AsciiTextEditor.TextArea.SelectionChanged += (_, _) => AsciiTextEditor_SelectionChanged();
    }

    private void OffsetJumpButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        JumpToEnteredOffset();
    }

    private void OffsetJumpTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        JumpToEnteredOffset();
        e.Handled = true;
    }

    private void JumpToEnteredOffset()
    {
        var text = OffsetJumpTextBox.Text?.Trim() ?? string.Empty;
        if (!TryParseHexOffset(text, out var offset))
        {
            ShowOffsetJumpStatus("Use 0x00000000");
            return;
        }

        JumpToOffset((int)offset);
    }

    private void JumpToOffset(int offset)
    {
        if (_visibleBlockData.Length == 0)
        {
            ShowOffsetJumpStatus("No block data");
            return;
        }

        if (offset < 0 || offset >= _visibleBlockData.Length)
        {
            ShowOffsetJumpStatus($"Max 0x{Math.Max(0, _visibleBlockData.Length - 1):X8}");
            return;
        }

        ClearOffsetJumpStatus();
        JumpRawDataEditorToOffset(offset);
    }

    private void JumpRawDataEditorToOffset(int offset)
    {
        var line = offset / 16 + 1;
        var column = GetRawDataColumnForByte(offset % 16) + 1;

        RawDataTextEditor.ScrollTo(line, column);

        var documentOffset = RawDataTextEditor.Document.GetOffset(line, column);
        RawDataTextEditor.Select(documentOffset, length: 2);
        RawDataTextEditor.Focus();

        Dispatcher.UIThread.Post(UpdateVisibleSideColumns, DispatcherPriority.Background);
    }

    private static int GetRawDataColumnForByte(int byteIndex)
    {
        var column = byteIndex * 3;
        column += byteIndex / 4;

        return column;
    }

    private static bool TryParseHexOffset(string value, out uint offset)
    {
        offset = 0;

        if (value.Length < 3 || value.Length > 10)
            return false;

        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return false;

        var hex = value[2..];
        if (hex.Length == 0 || hex.Any(character => !Uri.IsHexDigit(character)))
            return false;

        return uint.TryParse(
            hex,
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out offset);
    }

    private void ShowOffsetJumpStatus(string message)
    {
        OffsetJumpStatusTextBlock.Text = message;
        OffsetJumpStatusTextBlock.IsVisible = true;
    }

    private void ClearOffsetJumpStatus()
    {
        OffsetJumpStatusTextBlock.Text = string.Empty;
        OffsetJumpStatusTextBlock.IsVisible = false;
    }

    private void SetHexDump(byte[] data)
    {
        _visibleBlockData = data;
        _hexRowCount = (data.Length + 15) / 16;
        _visibleAsciiFirstRow = 0;
        _visibleAsciiRowCount = 0;

        RawDataTextEditor.Text = CreateRawDataDump(data);
        OffsetTextBlock.Text = string.Empty;
        AsciiTextEditor.Text = string.Empty;
        RawDataTextEditor.ScrollToHome();

        Dispatcher.UIThread.Post(UpdateVisibleSideColumns, DispatcherPriority.Background);
    }

    private void RawDataTextEditor_SelectionChanged()
    {
        if (_syncingSelection)
            return;

        ApplyAsciiSelectionFromRaw();
    }

    private void AsciiTextEditor_SelectionChanged()
    {
        if (_syncingSelection)
            return;

        if (!TryGetAsciiSelectionByteRange(out var range))
            return;

        SelectRawByteRange(range);
    }

    private void UpdateVisibleSideColumns()
    {
        if (_hexRowCount == 0 || _visibleBlockData.Length == 0)
        {
            OffsetTextBlock.Text = string.Empty;
            AsciiTextEditor.Text = string.Empty;
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
        OffsetTextBlock.Margin = new Avalonia.Thickness(0, topDelta, 0, 0);
        AsciiTextEditor.Margin = new Avalonia.Thickness(0, topDelta, 0, 0);

        OffsetTextBlock.Text = CreateOffsetWindow(firstRow, visibleRows);
        _visibleAsciiFirstRow = firstRow;
        _visibleAsciiRowCount = visibleRows;

        _syncingSelection = true;

        try
        {
            AsciiTextEditor.Text = CreateAsciiWindow(_visibleBlockData, firstRow, visibleRows);
            ApplyAsciiSelectionFromRaw();
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void ApplyAsciiSelectionFromRaw()
    {
        var wasSyncingSelection = _syncingSelection;
        _syncingSelection = true;

        try
        {
            if (!TryGetRawSelectionByteRange(out var rawRange))
            {
                AsciiTextEditor.Select(0, 0);
                return;
            }

            var visibleStart = _visibleAsciiFirstRow * 16;
            var visibleEnd = Math.Min(_visibleBlockData.Length, visibleStart + _visibleAsciiRowCount * 16);
            var start = Math.Max(rawRange.Start, visibleStart);
            var end = Math.Min(rawRange.EndExclusive, visibleEnd);

            if (start >= end)
            {
                AsciiTextEditor.Select(0, 0);
                return;
            }

            var selectionStart = GetAsciiTextOffsetForByte(start);
            var selectionEnd = GetAsciiTextOffsetForByte(end - 1) + 1;
            AsciiTextEditor.Select(selectionStart, selectionEnd - selectionStart);
        }
        finally
        {
            _syncingSelection = wasSyncingSelection;
        }
    }

    private bool TryGetRawSelectionByteRange(out ByteRange range)
    {
        range = default;

        var selectionStart = RawDataTextEditor.SelectionStart;
        var selectionEnd = selectionStart + RawDataTextEditor.SelectionLength;
        if (selectionEnd <= selectionStart || _visibleBlockData.Length == 0)
            return false;

        var document = RawDataTextEditor.Document;
        var firstLine = document.GetLineByOffset(selectionStart);
        var lastLine = document.GetLineByOffset(Math.Max(selectionStart, selectionEnd - 1));
        var minByte = int.MaxValue;
        var maxByte = -1;

        for (var line = firstLine; line is not null && line.LineNumber <= lastLine.LineNumber; line = line.NextLine)
        {
            var columnStart = Math.Max(0, selectionStart - line.Offset);
            var columnEnd = Math.Min(line.Length, selectionEnd - line.Offset);

            if (columnEnd <= columnStart)
                continue;

            for (var byteInRow = 0; byteInRow < 16; byteInRow++)
            {
                var tokenStart = GetRawDataColumnForByte(byteInRow);
                var tokenEnd = tokenStart + 2;

                if (tokenEnd <= columnStart || tokenStart >= columnEnd)
                    continue;

                var byteOffset = (line.LineNumber - 1) * 16 + byteInRow;
                if (byteOffset >= _visibleBlockData.Length)
                    continue;

                minByte = Math.Min(minByte, byteOffset);
                maxByte = Math.Max(maxByte, byteOffset);
            }
        }

        if (maxByte < minByte)
            return false;

        range = new ByteRange(minByte, maxByte + 1);
        return true;
    }

    private bool TryGetAsciiSelectionByteRange(out ByteRange range)
    {
        range = default;

        var selectionStart = AsciiTextEditor.SelectionStart;
        var selectionEnd = selectionStart + AsciiTextEditor.SelectionLength;
        if (selectionEnd <= selectionStart || _visibleBlockData.Length == 0)
            return false;

        var document = AsciiTextEditor.Document;
        var firstLine = document.GetLineByOffset(selectionStart);
        var lastLine = document.GetLineByOffset(Math.Max(selectionStart, selectionEnd - 1));
        var minByte = int.MaxValue;
        var maxByte = -1;

        for (var line = firstLine; line is not null && line.LineNumber <= lastLine.LineNumber; line = line.NextLine)
        {
            var columnStart = Math.Max(0, selectionStart - line.Offset);
            var columnEnd = Math.Min(line.Length, selectionEnd - line.Offset);

            if (columnEnd <= columnStart)
                continue;

            var row = _visibleAsciiFirstRow + line.LineNumber - 1;
            var rowOffset = row * 16;

            for (var column = columnStart; column < columnEnd; column++)
            {
                var byteOffset = rowOffset + column;
                if (byteOffset >= _visibleBlockData.Length)
                    continue;

                minByte = Math.Min(minByte, byteOffset);
                maxByte = Math.Max(maxByte, byteOffset);
            }
        }

        if (maxByte < minByte)
            return false;

        range = new ByteRange(minByte, maxByte + 1);
        return true;
    }

    private void SelectRawByteRange(ByteRange range)
    {
        var start = Math.Clamp(range.Start, 0, Math.Max(0, _visibleBlockData.Length - 1));
        var endExclusive = Math.Clamp(range.EndExclusive, start + 1, _visibleBlockData.Length);
        var lastByte = endExclusive - 1;
        var startTextOffset = GetRawTextOffsetForByte(start);
        var endTextOffset = GetRawTextOffsetForByte(lastByte) + 2;

        _syncingSelection = true;

        try
        {
            RawDataTextEditor.Select(startTextOffset, endTextOffset - startTextOffset);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private int GetAsciiTextOffsetForByte(int byteOffset)
    {
        var row = byteOffset / 16 - _visibleAsciiFirstRow;
        var column = byteOffset % 16;

        return AsciiTextEditor.Document.GetOffset(row + 1, column + 1);
    }

    private int GetRawTextOffsetForByte(int byteOffset)
    {
        var line = byteOffset / 16 + 1;
        var column = GetRawDataColumnForByte(byteOffset % 16) + 1;

        return RawDataTextEditor.Document.GetOffset(line, column);
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

    private readonly record struct ByteRange(int Start, int EndExclusive);
}
