using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.StructuredData;

public sealed class StructuredDataDefSetAsset : BaseAsset
{
    public const int SerializedSize = 0x0c;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public int DefCount { get; init; }
    public XPointer<StructuredDataDef[]> DefsPointer { get; init; }
    public IReadOnlyList<StructuredDataDef> Defs { get; init; } = [];
}

public sealed class StructuredDataDef
{
    public const int SerializedSize = 0x34;

    public int Version { get; init; }
    public uint FormatChecksum { get; init; }
    public int EnumCount { get; init; }
    public XPointer<StructuredDataEnum[]> EnumsPointer { get; init; }
    public int StructCount { get; init; }
    public XPointer<StructuredDataStruct[]> StructsPointer { get; init; }
    public int IndexedArrayCount { get; init; }
    public XPointer<StructuredDataIndexedArray[]> IndexedArraysPointer { get; init; }
    public int EnumedArrayCount { get; init; }
    public XPointer<StructuredDataEnumedArray[]> EnumedArraysPointer { get; init; }
    public StructuredDataType RootType { get; init; } = new();
    public uint Size { get; init; }

    public IReadOnlyList<StructuredDataEnum> Enums { get; set; } = [];
    public IReadOnlyList<StructuredDataStruct> Structs { get; set; } = [];
    public IReadOnlyList<StructuredDataIndexedArray> IndexedArrays { get; set; } = [];
    public IReadOnlyList<StructuredDataEnumedArray> EnumedArrays { get; set; } = [];
}

public sealed class StructuredDataEnum
{
    public const int SerializedSize = 0x0c;

    public int EntryCount { get; init; }
    public int ReservedEntryCount { get; init; }
    public XPointer<StructuredDataEnumEntry[]> EntriesPointer { get; init; }
    public IReadOnlyList<StructuredDataEnumEntry> Entries { get; set; } = [];
}

public sealed class StructuredDataEnumEntry
{
    public const int SerializedSize = 0x08;

    public XString StringPointer { get; init; }
    public string? String { get; init; }
    public ushort Index { get; init; }
    public ushort Padding { get; init; }
}

public sealed class StructuredDataStruct
{
    public const int SerializedSize = 0x10;

    public int PropertyCount { get; init; }
    public XPointer<StructuredDataStructProperty[]> PropertiesPointer { get; init; }
    public int Size { get; init; }
    public uint BitOffset { get; init; }
    public IReadOnlyList<StructuredDataStructProperty> Properties { get; set; } = [];
}

public sealed class StructuredDataStructProperty
{
    public const int SerializedSize = 0x10;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public StructuredDataType Type { get; init; } = new();
    public uint Offset { get; init; }
}

public sealed class StructuredDataIndexedArray
{
    public const int SerializedSize = 0x10;

    public int ArraySize { get; init; }
    public StructuredDataType ElementType { get; init; } = new();
    public uint ElementSize { get; init; }
}

public sealed class StructuredDataEnumedArray
{
    public const int SerializedSize = 0x10;

    public int EnumIndex { get; init; }
    public StructuredDataType ElementType { get; init; } = new();
    public uint ElementSize { get; init; }
}

public sealed class StructuredDataType
{
    public StructuredDataTypeCategory Type { get; init; }
    public int UnionValue { get; init; }
}

public enum StructuredDataTypeCategory
{
    DataInt = 0,
    DataByte = 1,
    DataBool = 2,
    DataString = 3,
    DataEnum = 4,
    DataStruct = 5,
    DataIndexedArray = 6,
    DataEnumArray = 7,
    DataFloat = 8,
    DataShort = 9,
    DataCount = 10
}
