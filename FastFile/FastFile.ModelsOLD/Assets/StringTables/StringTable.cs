using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.StringTables;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
[XEbootEvidence(
    "0x103b18",
    "eboot/traces/xasset_loader_findings.txt",
    Detail = "StringTable inner loader: Load_Stream size 0x10; Load_XString at root+0x00; cells pointer at +0x0c; count is columns(+0x04) * rows(+0x08).")]
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
