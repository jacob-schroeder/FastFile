using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class StructuredDataReader
{
    public static StructuredDataDefSet Read(ref ZoneReadContext context)
    {
        var asset = new StructuredDataDefSet
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            DefCount = context.ReadInt32()
        };

        asset.DefsPtr = context.ReadPointer<StructuredDataDef[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataDef[]> pointer) =>
            {
                var values = new StructuredDataDef[Math.Max(0, asset.DefCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataDef(ref pointerContext);

                pointer.SetResult(values);
            });

        return asset;
    }

    private static StructuredDataDef ReadStructuredDataDef(ref ZoneReadContext context)
    {
        var value = new StructuredDataDef
        {
            Version = context.ReadInt32(),
            FormatChecksum = context.ReadUInt32(),
            EnumCount = context.ReadInt32()
        };

        value.EnumsPtr = context.ReadPointer<StructuredDataEnum[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataEnum[]> pointer) =>
            {
                var values = new StructuredDataEnum[Math.Max(0, value.EnumCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataEnum(ref pointerContext);

                pointer.SetResult(values);
            });

        value.StructCount = context.ReadInt32();
        value.StructsPtr = context.ReadPointer<StructuredDataStruct[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataStruct[]> pointer) =>
            {
                var values = new StructuredDataStruct[Math.Max(0, value.StructCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataStruct(ref pointerContext);

                pointer.SetResult(values);
            });

        value.IndexedArrayCount = context.ReadInt32();
        value.IndexedArraysPtr = context.ReadPointer<StructuredDataIndexedArray[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataIndexedArray[]> pointer) =>
            {
                var values = new StructuredDataIndexedArray[Math.Max(0, value.IndexedArrayCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataIndexedArray(ref pointerContext);

                pointer.SetResult(values);
            });

        value.EnumedArrayCount = context.ReadInt32();
        value.EnumedArraysPtr = context.ReadPointer<StructuredDataEnumedArray[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataEnumedArray[]> pointer) =>
            {
                var values = new StructuredDataEnumedArray[Math.Max(0, value.EnumedArrayCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataEnumedArray(ref pointerContext);

                pointer.SetResult(values);
            });

        value.RootType = ReadStructuredDataType(ref context);
        value.Size = context.ReadUInt32();

        return value;
    }

    private static StructuredDataEnum ReadStructuredDataEnum(ref ZoneReadContext context)
    {
        var value = new StructuredDataEnum
        {
            EntryCount = context.ReadInt32(),
            ReservedEntryCount = context.ReadInt32()
        };

        value.EntriesPtr = context.ReadPointer<StructuredDataEnumEntry[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataEnumEntry[]> pointer) =>
            {
                var values = new StructuredDataEnumEntry[Math.Max(0, value.EntryCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataEnumEntry(ref pointerContext);

                pointer.SetResult(values);
            });

        return value;
    }

    private static StructuredDataEnumEntry ReadStructuredDataEnumEntry(ref ZoneReadContext context)
    {
        return new StructuredDataEnumEntry
        {
            StringPtr = GenericReader.ReadStringPointer(ref context),
            Index = context.ReadUInt16(),
            Padding = context.ReadUInt16()
        };
    }

    private static StructuredDataStruct ReadStructuredDataStruct(ref ZoneReadContext context)
    {
        var value = new StructuredDataStruct
        {
            PropertyCount = context.ReadInt32()
        };

        value.PropertiesPtr = context.ReadPointer<StructuredDataStructProperty[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StructuredDataStructProperty[]> pointer) =>
            {
                var values = new StructuredDataStructProperty[Math.Max(0, value.PropertyCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadStructuredDataStructProperty(ref pointerContext);

                pointer.SetResult(values);
            });

        value.Size = context.ReadInt32();
        value.BitOffset = context.ReadUInt32();

        return value;
    }

    private static StructuredDataStructProperty ReadStructuredDataStructProperty(ref ZoneReadContext context)
    {
        return new StructuredDataStructProperty
        {
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Type = ReadStructuredDataType(ref context),
            Offset = context.ReadUInt32()
        };
    }

    private static StructuredDataIndexedArray ReadStructuredDataIndexedArray(ref ZoneReadContext context)
    {
        return new StructuredDataIndexedArray
        {
            ArraySize = context.ReadInt32(),
            ElementType = ReadStructuredDataType(ref context),
            ElementSize = context.ReadUInt32()
        };
    }

    private static StructuredDataEnumedArray ReadStructuredDataEnumedArray(ref ZoneReadContext context)
    {
        return new StructuredDataEnumedArray
        {
            EnumIndex = context.ReadInt32(),
            ElementType = ReadStructuredDataType(ref context),
            ElementSize = context.ReadUInt32()
        };
    }

    private static StructuredDataType ReadStructuredDataType(ref ZoneReadContext context)
    {
        return new StructuredDataType
        {
            Type = (StructuredDataTypeCategory)context.ReadInt32(),
            UnionValue = context.ReadInt32()
        };
    }
}
