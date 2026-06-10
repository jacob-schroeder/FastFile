using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.StringTables;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public class StringTable() : BaseAsset(XAssetType.StringTable)
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int ColumnCount { get; set; }

    [XField(Offset = 0x08)]
    public int RowCount { get; set; }

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(CellCount))]
    public XPointer<StringTableCell[]> StringsPtr { get; set; } // Direct
    public StringTableCell[] Strings => StringsPtr is { IsResolved: true, Value: not null }
        ? StringsPtr.Value
        : [];

    public int CellCount => ColumnCount * RowCount;

    public override string? GetDisplayName => Name;
}
