using FastFile.ModelsOLD.Assets.StringTables;

namespace UI.Models;

public sealed class StringTableCellDisplayItem
{
    private readonly StringTableCell? _cell;
    private string _value;

    public StringTableCellDisplayItem(StringTableCell cell)
    {
        _cell = cell;
        _value = cell.String;
    }

    public StringTableCellDisplayItem(string value)
    {
        _value = value;
    }

    public string Value
    {
        get => _value;
        set
        {
            value ??= string.Empty;
            if (_value == value)
                return;

            _value = value;
            _cell?.SetString(value);
        }
    }
}
