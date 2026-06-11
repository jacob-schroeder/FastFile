using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.StructuredData;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
[XEbootEvidence(
    "0x103630",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataDefSet body: Load_Stream size 0x0c; Load_XString at +0x00; defs pointer at +0x08; count at +0x04; def array consumed through 0x1035a8.")]
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
[XEbootEvidence(
    "0x103400",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataDef body: Load_Stream size 0x34; +0x0c/+0x08 enum array, +0x14/+0x10 struct array, +0x1c/+0x18 indexed arrays via 0xe3bb8, +0x24/+0x20 enumed arrays via 0xe3ae0.")]
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
[XEbootEvidence(
    "0x103278",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataEnum body: Load_Stream size 0x0c; entries pointer at +0x08; entry count at +0x00; entries are count * 0x08 through 0x1031f0.")]
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
[XEbootEvidence(
    "0x103138",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataEnumEntry body: Load_Stream size 0x08; Load_XString at +0x00; remaining index/padding bytes are copied as part of the 0x08 root.")]
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
[XEbootEvidence(
    "0x102e78",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataStructProperty body: Load_Stream size 0x10; Load_XString at +0x00; embedded type/offset tail is copied as part of the 0x10 root.")]
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
[XEbootEvidence(
    "0x102fb8",
    "Data/eboot/structureddata_loader_102e78_103630.txt",
    Detail = "StructuredDataStruct body: Load_Stream size 0x10; properties pointer at +0x04; property count at +0x00; property array is count * 0x10 through 0x102f30.")]
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
[XEbootEvidence(
    "0xe3bb8",
    "Data/eboot/structured_indexed_enumed_arrays_e3ae0_e3bb8.txt",
    Detail = "StructuredDataIndexedArray leaf: parent StructuredDataDef uses +0x1c pointer and +0x18 count; loader allocates count * 0x10 entries.")]
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
[XEbootEvidence(
    "0xe3ae0",
    "Data/eboot/structured_indexed_enumed_arrays_e3ae0_e3bb8.txt",
    Detail = "StructuredDataEnumedArray leaf: parent StructuredDataDef uses +0x24 pointer and +0x20 count; loader allocates count * 0x10 entries.")]
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
