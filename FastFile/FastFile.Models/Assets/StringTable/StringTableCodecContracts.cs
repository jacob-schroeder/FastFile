using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StringTable;

public static class StringTableCodecContracts
{
    private const string EvidenceText =
        "PS3 StringTable loader: top-level inline payload pushes TEMP, aligns runtime block to 4, " +
        "Load_Stream root size 0x10, then pushes LARGE and loads name XString followed by a direct " +
        "counted StringTableCell array; each cell is XString plus int32 hash; validated against patch_mp.";

    public static readonly XPointerFieldContract NamePointer = new(
        "name",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x00: Load_XString for StringTable name; direct pointer cell patched before string payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XScalarFieldContract ColumnCount = new(
        "columnCount",
        0x04,
        sizeof(int),
        "int32",
        "0x04: column count copied by root Load_Stream.");

    public static readonly XScalarFieldContract RowCount = new(
        "rowCount",
        0x08,
        sizeof(int),
        "int32",
        "0x08: row count copied by root Load_Stream.");

    public static readonly XPointerFieldContract CellsPointer = new(
        "cells",
        0x0C,
        "StringTableCell[]",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x0C: direct counted cell array pointer; destination cell patched before cell table payload.",
        InlineAlignment: 4,
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XPointerFieldContract CellStringPointer = new(
        "string",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "StringTableCell +0x00: Load_XString for cell value; direct pointer cell patched before string payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XScalarFieldContract CellHash = new(
        "hash",
        0x04,
        sizeof(int),
        "int32",
        "StringTableCell +0x04: int32 hash copied by cell table Load_Stream.");

    public static readonly XStructCodecContract Cell = new(
        "StringTableCell",
        StringTableCell.SerializedSize,
        [
            CellStringPointer,
            CellHash
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract Root = new(
        "StringTableRoot",
        StringTableAsset.SerializedSize,
        [
            NamePointer,
            ColumnCount,
            RowCount,
            CellsPointer
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady,
        new XCodecRecipe(
            "StringTableRoot",
            [
                XCodecOps.PushBlock(XFileBlockType.TEMP, "Top-level StringTable wrapper pushes TEMP."),
                XCodecOps.Align(4, "StringTable loader aligns the active runtime block to 4 before root Load_Stream."),
                XCodecOps.StreamStruct("StringTableRoot", StringTableAsset.SerializedSize, XFileBlockType.TEMP, 4, "Load_Stream root size 0x10."),
                XCodecOps.PushBlock(XFileBlockType.LARGE, "StringTable name, cells, and cell strings load under LARGE."),
                XCodecOps.Field(NamePointer),
                XCodecOps.Align(4, "StringTable cells pointer path aligns active runtime block to 4."),
                XCodecOps.Field(CellsPointer),
                XCodecOps.PopBlock(XFileBlockType.LARGE, "StringTable pops LARGE after cells and nested strings."),
                XCodecOps.PopBlock(XFileBlockType.TEMP, "StringTable pops TEMP after root and children.")
            ],
            EvidenceText));

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.StringTable,
        Root,
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        Cell,
        Root,
        Asset
    ];

    public static void Register(XCodecContractRegistry registry)
    {
        registry.AddRange(All);
    }
}
