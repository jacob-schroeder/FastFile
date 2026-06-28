using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StructuredData;

public static class StructuredDataCodecContracts
{
    private const string EvidenceText =
        "PS3 StructuredDataDefSet loader: top-level inline payload pushes TEMP, aligns runtime block to 4, " +
        "Load_Stream root size 0x0c, then pushes LARGE and loads name XString, def table, and direct counted " +
        "nested enum/struct/indexed-array/enumed-array tables; validated against patch_mp.";

    public static readonly XPointerFieldContract RootNamePointer = Pointer("name", 0x00, "XString", "DefSet +0x00: direct XString name.");
    public static readonly XScalarFieldContract DefCount = Scalar("defCount", 0x04, "int32", "DefSet +0x04: def count.");
    public static readonly XPointerFieldContract DefsPointer = Pointer("defs", 0x08, "StructuredDataDef[]", "DefSet +0x08: direct counted def array.", 4);

    public static readonly XStructCodecContract DefSet = new(
        "StructuredDataDefSetAsset",
        StructuredDataDefSetAsset.SerializedSize,
        [RootNamePointer, DefCount, DefsPointer],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract Type = new(
        "StructuredDataType",
        0x08,
        [
            Scalar("type", 0x00, "int32", "StructuredDataType +0x00: category enum."),
            Scalar("unionValue", 0x04, "int32", "StructuredDataType +0x04: union/index/value field.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract Def = new(
        "StructuredDataDef",
        StructuredDataDef.SerializedSize,
        [
            Scalar("version", 0x00, "int32", "StructuredDataDef +0x00: version."),
            Scalar("formatChecksum", 0x04, "uint32", "StructuredDataDef +0x04: format checksum."),
            Scalar("enumCount", 0x08, "int32", "StructuredDataDef +0x08: enum count."),
            Pointer("enums", 0x0C, "StructuredDataEnum[]", "StructuredDataDef +0x0c: direct counted enum array.", 4),
            Scalar("structCount", 0x10, "int32", "StructuredDataDef +0x10: struct count."),
            Pointer("structs", 0x14, "StructuredDataStruct[]", "StructuredDataDef +0x14: direct counted struct array.", 4),
            Scalar("indexedArrayCount", 0x18, "int32", "StructuredDataDef +0x18: indexed-array count."),
            Pointer("indexedArrays", 0x1C, "StructuredDataIndexedArray[]", "StructuredDataDef +0x1c: direct counted indexed-array table.", 4),
            Scalar("enumedArrayCount", 0x20, "int32", "StructuredDataDef +0x20: enumed-array count."),
            Pointer("enumedArrays", 0x24, "StructuredDataEnumedArray[]", "StructuredDataDef +0x24: direct counted enumed-array table.", 4),
            new XStructFieldContract("rootType", 0x28, 0x08, Type.Name, "StructuredDataDef +0x28: inline StructuredDataType."),
            Scalar("size", 0x30, "uint32", "StructuredDataDef +0x30: serialized runtime size.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract Enum = new(
        "StructuredDataEnum",
        StructuredDataEnum.SerializedSize,
        [
            Scalar("entryCount", 0x00, "int32", "StructuredDataEnum +0x00: entry count."),
            Scalar("reservedEntryCount", 0x04, "int32", "StructuredDataEnum +0x04: reserved entry count."),
            Pointer("entries", 0x08, "StructuredDataEnumEntry[]", "StructuredDataEnum +0x08: direct counted entry array.", 4)
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract EnumEntry = new(
        "StructuredDataEnumEntry",
        StructuredDataEnumEntry.SerializedSize,
        [
            Pointer("string", 0x00, "XString", "StructuredDataEnumEntry +0x00: direct XString value."),
            Scalar("index", 0x04, "uint16", "StructuredDataEnumEntry +0x04: enum index."),
            Scalar("padding", 0x06, "uint16", "StructuredDataEnumEntry +0x06: copied padding/reserved field.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract Struct = new(
        "StructuredDataStruct",
        StructuredDataStruct.SerializedSize,
        [
            Scalar("propertyCount", 0x00, "int32", "StructuredDataStruct +0x00: property count."),
            Pointer("properties", 0x04, "StructuredDataStructProperty[]", "StructuredDataStruct +0x04: direct counted property array.", 4),
            Scalar("size", 0x08, "int32", "StructuredDataStruct +0x08: struct size."),
            Scalar("bitOffset", 0x0C, "uint32", "StructuredDataStruct +0x0c: bit offset.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract StructProperty = new(
        "StructuredDataStructProperty",
        StructuredDataStructProperty.SerializedSize,
        [
            Pointer("name", 0x00, "XString", "StructuredDataStructProperty +0x00: direct XString name."),
            new XStructFieldContract("type", 0x04, 0x08, Type.Name, "StructuredDataStructProperty +0x04: inline StructuredDataType."),
            Scalar("offset", 0x0C, "uint32", "StructuredDataStructProperty +0x0c: property offset.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract IndexedArray = new(
        "StructuredDataIndexedArray",
        StructuredDataIndexedArray.SerializedSize,
        [
            Scalar("arraySize", 0x00, "int32", "StructuredDataIndexedArray +0x00: array size."),
            new XStructFieldContract("elementType", 0x04, 0x08, Type.Name, "StructuredDataIndexedArray +0x04: inline StructuredDataType."),
            Scalar("elementSize", 0x0C, "uint32", "StructuredDataIndexedArray +0x0c: element size.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XStructCodecContract EnumedArray = new(
        "StructuredDataEnumedArray",
        StructuredDataEnumedArray.SerializedSize,
        [
            Scalar("enumIndex", 0x00, "int32", "StructuredDataEnumedArray +0x00: enum index."),
            new XStructFieldContract("elementType", 0x04, 0x08, Type.Name, "StructuredDataEnumedArray +0x04: inline StructuredDataType."),
            Scalar("elementSize", 0x0C, "uint32", "StructuredDataEnumedArray +0x0c: element size.")
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.StructuredDataDef,
        DefSet,
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        DefSet,
        Def,
        Enum,
        EnumEntry,
        Struct,
        StructProperty,
        IndexedArray,
        EnumedArray,
        Type,
        Asset
    ];

    public static void Register(XCodecContractRegistry registry)
    {
        registry.AddRange(All);
    }

    private static XScalarFieldContract Scalar(string name, int offset, string type, string evidence)
    {
        int size = type == "uint16" ? sizeof(ushort) : sizeof(int);
        return new XScalarFieldContract(name, offset, size, type, evidence);
    }

    private static XPointerFieldContract Pointer(string name, int offset, string target, string evidence, int inlineAlignment = 0)
    {
        return new XPointerFieldContract(
            name,
            offset,
            target,
            XPointerResolutionMode.Direct,
            XPointerSourceSemantics.NullableReferenceOrInline,
            evidence,
            inlineAlignment,
            XFileBlockType.LARGE);
    }
}
