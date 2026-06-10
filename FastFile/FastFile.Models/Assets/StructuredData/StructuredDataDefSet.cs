using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StructuredData;

public sealed class StructuredDataDefSet() : BaseAsset(XAssetType.StructuredDataDef)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public int DefCount { get; set; }
    public XPointer<StructuredDataDef[]> DefsPtr { get; set; } // Direct
    public StructuredDataDef[] Defs => DefsPtr is { IsResolved: true, Value: not null }
        ? DefsPtr.Value
        : [];

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"StructuredDataDef 0x{Offset:X8}"
        : Name;
}

public sealed class StructuredDataDef
{
    public int Version { get; set; }
    public uint FormatChecksum { get; set; }

    public int EnumCount { get; set; }
    public XPointer<StructuredDataEnum[]> EnumsPtr { get; set; } // Direct
    public StructuredDataEnum[] Enums => EnumsPtr is { IsResolved: true, Value: not null }
        ? EnumsPtr.Value
        : [];

    public int StructCount { get; set; }
    public XPointer<StructuredDataStruct[]> StructsPtr { get; set; } // Direct
    public StructuredDataStruct[] Structs => StructsPtr is { IsResolved: true, Value: not null }
        ? StructsPtr.Value
        : [];

    public int IndexedArrayCount { get; set; }
    public XPointer<StructuredDataIndexedArray[]> IndexedArraysPtr { get; set; } // Direct
    public StructuredDataIndexedArray[] IndexedArrays => IndexedArraysPtr is { IsResolved: true, Value: not null }
        ? IndexedArraysPtr.Value
        : [];

    public int EnumedArrayCount { get; set; }
    public XPointer<StructuredDataEnumedArray[]> EnumedArraysPtr { get; set; } // Direct
    public StructuredDataEnumedArray[] EnumedArrays => EnumedArraysPtr is { IsResolved: true, Value: not null }
        ? EnumedArraysPtr.Value
        : [];

    public StructuredDataType RootType { get; set; } = new();
    public uint Size { get; set; }
}

public sealed class StructuredDataEnum
{
    public int EntryCount { get; set; }
    public int ReservedEntryCount { get; set; }
    public XPointer<StructuredDataEnumEntry[]> EntriesPtr { get; set; } // Direct
    public StructuredDataEnumEntry[] Entries => EntriesPtr is { IsResolved: true, Value: not null }
        ? EntriesPtr.Value
        : [];
}

public sealed class StructuredDataEnumEntry
{
    public XPointer<string> StringPtr { get; set; } // Direct
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Value ?? string.Empty : string.Empty;

    public ushort Index { get; set; }
    public ushort Padding { get; set; }
}

public sealed class StructuredDataStructProperty
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public StructuredDataType Type { get; set; } = new();
    public uint Offset { get; set; }
}

public sealed class StructuredDataStruct
{
    public int PropertyCount { get; set; }
    public XPointer<StructuredDataStructProperty[]> PropertiesPtr { get; set; } // Direct
    public StructuredDataStructProperty[] Properties => PropertiesPtr is { IsResolved: true, Value: not null }
        ? PropertiesPtr.Value
        : [];

    public int Size { get; set; }
    public uint BitOffset { get; set; }
}

public sealed class StructuredDataIndexedArray
{
    public int ArraySize { get; set; }
    public StructuredDataType ElementType { get; set; } = new();
    public uint ElementSize { get; set; }
}

public sealed class StructuredDataEnumedArray
{
    public int EnumIndex { get; set; }
    public StructuredDataType ElementType { get; set; } = new();
    public uint ElementSize { get; set; }
}

public sealed class StructuredDataType
{
    public StructuredDataTypeCategory Type { get; set; }
    public int UnionValue { get; set; }
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
