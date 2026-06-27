using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.StringTable;

public sealed class StringTableAsset : BaseAsset
{
    public const int SerializedSize = 0x10;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public int ColumnCount { get; init; }
    public int RowCount { get; init; }
    public XPointer<StringTableCell[]> CellsPointer { get; init; }
    public IReadOnlyList<StringTableCell> Cells { get; init; } = [];
    public int CellCount => checked(ColumnCount * RowCount);
}

public sealed class StringTableCell
{
    public const int SerializedSize = 0x08;

    public XString StringPointer { get; init; }
    public string? String { get; init; }
    public int Hash { get; init; }
}
