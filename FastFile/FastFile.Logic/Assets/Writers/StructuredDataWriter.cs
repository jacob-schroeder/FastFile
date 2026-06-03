using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class StructuredDataWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        var set = (StructuredDataDefSet)asset;

        GenericWriter.WriteStringPointer(context, set.NamePtr);
        context.WriteInt32(set.DefCount);
        context.WritePointer(set.DefsPtr, WriteStructuredDataDefs);
    }

    private static void WriteStructuredDataDefs(
        ZoneWriterContext context,
        ZonePointer<StructuredDataDef[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataDef(context, value);
    }

    private static void WriteStructuredDataDef(ZoneWriterContext context, StructuredDataDef value)
    {
        context.WriteInt32(value.Version);
        context.WriteUInt32(value.FormatChecksum);
        context.WriteInt32(value.EnumCount);
        context.WritePointer(value.EnumsPtr, WriteStructuredDataEnums);
        context.WriteInt32(value.StructCount);
        context.WritePointer(value.StructsPtr, WriteStructuredDataStructs);
        context.WriteInt32(value.IndexedArrayCount);
        context.WritePointer(value.IndexedArraysPtr, WriteStructuredDataIndexedArrays);
        context.WriteInt32(value.EnumedArrayCount);
        context.WritePointer(value.EnumedArraysPtr, WriteStructuredDataEnumedArrays);
        WriteStructuredDataType(context, value.RootType);
        context.WriteUInt32(value.Size);
    }

    private static void WriteStructuredDataEnums(
        ZoneWriterContext context,
        ZonePointer<StructuredDataEnum[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataEnum(context, value);
    }

    private static void WriteStructuredDataEnum(ZoneWriterContext context, StructuredDataEnum value)
    {
        context.WriteInt32(value.EntryCount);
        context.WriteInt32(value.ReservedEntryCount);
        context.WritePointer(value.EntriesPtr, WriteStructuredDataEnumEntries);
    }

    private static void WriteStructuredDataEnumEntries(
        ZoneWriterContext context,
        ZonePointer<StructuredDataEnumEntry[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataEnumEntry(context, value);
    }

    private static void WriteStructuredDataEnumEntry(ZoneWriterContext context, StructuredDataEnumEntry value)
    {
        GenericWriter.WriteStringPointer(context, value.StringPtr);
        context.WriteUInt16(value.Index);
        context.WriteUInt16(value.Padding);
    }

    private static void WriteStructuredDataStructs(
        ZoneWriterContext context,
        ZonePointer<StructuredDataStruct[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataStruct(context, value);
    }

    private static void WriteStructuredDataStruct(ZoneWriterContext context, StructuredDataStruct value)
    {
        context.WriteInt32(value.PropertyCount);
        context.WritePointer(value.PropertiesPtr, WriteStructuredDataStructProperties);
        context.WriteInt32(value.Size);
        context.WriteUInt32(value.BitOffset);
    }

    private static void WriteStructuredDataStructProperties(
        ZoneWriterContext context,
        ZonePointer<StructuredDataStructProperty[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataStructProperty(context, value);
    }

    private static void WriteStructuredDataStructProperty(
        ZoneWriterContext context,
        StructuredDataStructProperty value)
    {
        GenericWriter.WriteStringPointer(context, value.NamePtr);
        WriteStructuredDataType(context, value.Type);
        context.WriteUInt32(value.Offset);
    }

    private static void WriteStructuredDataIndexedArrays(
        ZoneWriterContext context,
        ZonePointer<StructuredDataIndexedArray[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataIndexedArray(context, value);
    }

    private static void WriteStructuredDataIndexedArray(
        ZoneWriterContext context,
        StructuredDataIndexedArray value)
    {
        context.WriteInt32(value.ArraySize);
        WriteStructuredDataType(context, value.ElementType);
        context.WriteUInt32(value.ElementSize);
    }

    private static void WriteStructuredDataEnumedArrays(
        ZoneWriterContext context,
        ZonePointer<StructuredDataEnumedArray[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            WriteStructuredDataEnumedArray(context, value);
    }

    private static void WriteStructuredDataEnumedArray(
        ZoneWriterContext context,
        StructuredDataEnumedArray value)
    {
        context.WriteInt32(value.EnumIndex);
        WriteStructuredDataType(context, value.ElementType);
        context.WriteUInt32(value.ElementSize);
    }

    private static void WriteStructuredDataType(ZoneWriterContext context, StructuredDataType value)
    {
        context.WriteInt32((int)value.Type);
        context.WriteInt32(value.UnionValue);
    }
}
