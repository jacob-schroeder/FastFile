using FastFile.Models.Assets.StringTables;
using FastFile.Models.Data;

namespace UI.Models;

public sealed class StringTableRowDisplayItem
{
    private readonly StringTable _table;
    private readonly int _rowIndex;
    private StringTableCellDisplayItem[]? _cells;

    public StringTableRowDisplayItem(StringTable table, int rowIndex)
    {
        _table = table;
        _rowIndex = rowIndex;
    }

    public StringTableCellDisplayItem[] Cells => _cells ??= CreateCells();

    private StringTableCellDisplayItem[] CreateCells()
    {
        var cells = new StringTableCellDisplayItem[_table.ColumnCount];

        for (var column = 0; column < _table.ColumnCount; column++)
        {
            cells[column] = new StringTableCellDisplayItem
            {
                Value = GetCellText(column)
            };
        }

        return cells;
    }

    private string GetCellText(int column)
    {
        var index = (_rowIndex * _table.ColumnCount) + column;
        if (index < 0 || index >= _table.Strings.Length)
        {
            return string.Empty;
        }

        var cell = _table.Strings[index];
        return cell.StringPtr.Kind == PointerKind.Offset
            ? "[EXTERNAL]"
            : cell.String;
    }
}
