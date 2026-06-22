using FastFile.ModelsOLD.Assets.StringTables;

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
            cells[column] = GetCellDisplayItem(column);
        }

        return cells;
    }

    private StringTableCellDisplayItem GetCellDisplayItem(int column)
    {
        var index = (_rowIndex * _table.ColumnCount) + column;
        if (index < 0 || index >= _table.Strings.Length)
            return new StringTableCellDisplayItem(string.Empty);

        var cell = _table.Strings[index];
        return new StringTableCellDisplayItem(cell);
    }
}
