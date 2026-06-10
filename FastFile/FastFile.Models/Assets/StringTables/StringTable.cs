using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StringTables;

public class StringTable() : BaseAsset(XAssetType.StringTable)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public int ColumnCount { get; set; }
    public int RowCount { get; set; }

    public XPointer<StringTableCell[]> StringsPtr { get; set; } // Direct
    public StringTableCell[] Strings => StringsPtr is { IsResolved: true, Value: not null }
        ? StringsPtr.Value
        : [];

    public override string? GetDisplayName => Name;
}
