using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.StructuredData;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class StructuredDataDefSet() : BaseAsset(XAssetType.StructuredDataDef)
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int DefCount { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(DefCount))]
    public XPointer<StructuredDataDef[]> DefsPtr { get; set; } // Direct
    public StructuredDataDef[] Defs => DefsPtr is { IsResolved: true, Value: not null }
        ? DefsPtr.Value
        : [];

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"StructuredDataDef 0x{Offset:X8}"
        : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x34)]
public sealed class StructuredDataDef
{
    [XField(Offset = 0x00)]
    public int Version { get; set; }

    [XField(Offset = 0x04)]
    public uint FormatChecksum { get; set; }

    [XField(Offset = 0x08)]
    public int EnumCount { get; set; }

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(EnumCount))]
    public XPointer<StructuredDataEnum[]> EnumsPtr { get; set; } // Direct
    public StructuredDataEnum[] Enums => EnumsPtr is { IsResolved: true, Value: not null }
        ? EnumsPtr.Value
        : [];

    [XField(Offset = 0x10)]
    public int StructCount { get; set; }

    [XField(Offset = 0x14)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(StructCount))]
    public XPointer<StructuredDataStruct[]> StructsPtr { get; set; } // Direct
    public StructuredDataStruct[] Structs => StructsPtr is { IsResolved: true, Value: not null }
        ? StructsPtr.Value
        : [];

    [XField(Offset = 0x18)]
    public int IndexedArrayCount { get; set; }

    [XField(Offset = 0x1C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(IndexedArrayCount))]
    public XPointer<StructuredDataIndexedArray[]> IndexedArraysPtr { get; set; } // Direct
    public StructuredDataIndexedArray[] IndexedArrays => IndexedArraysPtr is { IsResolved: true, Value: not null }
        ? IndexedArraysPtr.Value
        : [];

    [XField(Offset = 0x20)]
    public int EnumedArrayCount { get; set; }

    [XField(Offset = 0x24)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(EnumedArrayCount))]
    public XPointer<StructuredDataEnumedArray[]> EnumedArraysPtr { get; set; } // Direct
    public StructuredDataEnumedArray[] EnumedArrays => EnumedArraysPtr is { IsResolved: true, Value: not null }
        ? EnumedArraysPtr.Value
        : [];

    [XField(Offset = 0x28)]
    public StructuredDataType RootType { get; set; } = new();

    [XField(Offset = 0x30)]
    public uint Size { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class StructuredDataEnum
{
    [XField(Offset = 0x00)]
    public int EntryCount { get; set; }

    [XField(Offset = 0x04)]
    public int ReservedEntryCount { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(EntryCount))]
    public XPointer<StructuredDataEnumEntry[]> EntriesPtr { get; set; } // Direct
    public StructuredDataEnumEntry[] Entries => EntriesPtr is { IsResolved: true, Value: not null }
        ? EntriesPtr.Value
        : [];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class StructuredDataEnumEntry
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> StringPtr { get; set; } // Direct
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public ushort Index { get; set; }

    [XField(Offset = 0x06)]
    public ushort Padding { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class StructuredDataStructProperty
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public StructuredDataType Type { get; set; } = new();

    [XField(Offset = 0x0C)]
    public uint Offset { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class StructuredDataStruct
{
    [XField(Offset = 0x00)]
    public int PropertyCount { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(PropertyCount))]
    public XPointer<StructuredDataStructProperty[]> PropertiesPtr { get; set; } // Direct
    public StructuredDataStructProperty[] Properties => PropertiesPtr is { IsResolved: true, Value: not null }
        ? PropertiesPtr.Value
        : [];

    [XField(Offset = 0x08)]
    public int Size { get; set; }

    [XField(Offset = 0x0C)]
    public uint BitOffset { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class StructuredDataIndexedArray
{
    [XField(Offset = 0x00)]
    public int ArraySize { get; set; }

    [XField(Offset = 0x04)]
    public StructuredDataType ElementType { get; set; } = new();

    [XField(Offset = 0x0C)]
    public uint ElementSize { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class StructuredDataEnumedArray
{
    [XField(Offset = 0x00)]
    public int EnumIndex { get; set; }

    [XField(Offset = 0x04)]
    public StructuredDataType ElementType { get; set; } = new();

    [XField(Offset = 0x0C)]
    public uint ElementSize { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class StructuredDataType
{
    [XField(Offset = 0x00)]
    public StructuredDataTypeCategory Type { get; set; }

    [XField(Offset = 0x04)]
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
