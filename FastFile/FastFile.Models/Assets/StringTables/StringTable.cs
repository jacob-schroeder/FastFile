using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StringTables;

public class StringTable() : BaseAsset(XAssetType.StringTable)
{
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public int ColumnCount { get; set; }
    public int RowCount { get; set; }

    public DirectPointer<StringTableCell[]> StringsPtr { get; set; }
    public StringTableCell[] Strings => StringsPtr is { IsResolved: true, Result: not null }
        ? StringsPtr.Result
        : [];

    public override string? GetDisplayName => Name;
}
