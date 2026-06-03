using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.StructuredData;

public sealed class StructuredDataDefSet() : BaseAsset(XAssetType.StructuredDataDef)
{
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public int DefCount { get; set; }
    public ZonePointer<StructuredDataDef[]> DefsPtr { get; set; } = new(0);
    public StructuredDataDef[] Defs => DefsPtr is { IsResolved: true, Result: not null }
        ? DefsPtr.Result
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
    public ZonePointer<StructuredDataEnum[]> EnumsPtr { get; set; } = new(0);
    public StructuredDataEnum[] Enums => EnumsPtr is { IsResolved: true, Result: not null }
        ? EnumsPtr.Result
        : [];

    public int StructCount { get; set; }
    public ZonePointer<StructuredDataStruct[]> StructsPtr { get; set; } = new(0);
    public StructuredDataStruct[] Structs => StructsPtr is { IsResolved: true, Result: not null }
        ? StructsPtr.Result
        : [];

    public int IndexedArrayCount { get; set; }
    public ZonePointer<StructuredDataIndexedArray[]> IndexedArraysPtr { get; set; } = new(0);
    public StructuredDataIndexedArray[] IndexedArrays => IndexedArraysPtr is { IsResolved: true, Result: not null }
        ? IndexedArraysPtr.Result
        : [];

    public int EnumedArrayCount { get; set; }
    public ZonePointer<StructuredDataEnumedArray[]> EnumedArraysPtr { get; set; } = new(0);
    public StructuredDataEnumedArray[] EnumedArrays => EnumedArraysPtr is { IsResolved: true, Result: not null }
        ? EnumedArraysPtr.Result
        : [];

    public StructuredDataType RootType { get; set; } = new();
    public uint Size { get; set; }
}

public sealed class StructuredDataEnum
{
    public int EntryCount { get; set; }
    public int ReservedEntryCount { get; set; }
    public ZonePointer<StructuredDataEnumEntry[]> EntriesPtr { get; set; } = new(0);
    public StructuredDataEnumEntry[] Entries => EntriesPtr is { IsResolved: true, Result: not null }
        ? EntriesPtr.Result
        : [];
}

public sealed class StructuredDataEnumEntry
{
    public ZonePointer<string> StringPtr { get; set; } = new(0);
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Result ?? string.Empty : string.Empty;

    public ushort Index { get; set; }
    public ushort Padding { get; set; }
}

public sealed class StructuredDataStructProperty
{
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public StructuredDataType Type { get; set; } = new();
    public uint Offset { get; set; }
}

public sealed class StructuredDataStruct
{
    public int PropertyCount { get; set; }
    public ZonePointer<StructuredDataStructProperty[]> PropertiesPtr { get; set; } = new(0);
    public StructuredDataStructProperty[] Properties => PropertiesPtr is { IsResolved: true, Result: not null }
        ? PropertiesPtr.Result
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
