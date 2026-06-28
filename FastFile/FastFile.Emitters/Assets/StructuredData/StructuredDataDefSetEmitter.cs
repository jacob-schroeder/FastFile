using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Codecs;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.StructuredData;

public sealed class StructuredDataDefSetEmitter : IXAssetEmitter<StructuredDataDefSetAsset>
{
    public IXAssetCodecContract Contract => StructuredDataCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, StructuredDataDefSetAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        string name = asset.Name ?? throw new InvalidDataException("StructuredDataDefSet name is required for inline PS3 emission.");
        ValidateCount(asset.DefCount, asset.Defs.Count, "StructuredDataDefSet defs");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress rootAddress = context.Blocks.AllocateCurrent(StructuredDataDefSetAsset.SerializedSize);

            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(asset.DefCount);
            context.Source.WriteInt32(PointerRaw(asset.DefCount));

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitInlineXString(context, rootAddress.Add(0x00), name);
                if (asset.DefCount > 0)
                    EmitDefArray(context, rootAddress.Add(0x08), asset.Defs);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"StructuredDataDefSet emitted source=0x{sourceOffset:X} name='{name}' defs={asset.DefCount} blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitDefArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataDef> defs)
    {
        XBlockAddress tableAddress = PatchAndAllocateTable(context, pointerCellAddress, defs.Count, StructuredDataDef.SerializedSize);

        for (int i = 0; i < defs.Count; i++)
        {
            StructuredDataDef def = defs[i];
            ValidateCount(def.EnumCount, def.Enums.Count, $"StructuredDataDef[{i}].enums");
            ValidateCount(def.StructCount, def.Structs.Count, $"StructuredDataDef[{i}].structs");
            ValidateCount(def.IndexedArrayCount, def.IndexedArrays.Count, $"StructuredDataDef[{i}].indexedArrays");
            ValidateCount(def.EnumedArrayCount, def.EnumedArrays.Count, $"StructuredDataDef[{i}].enumedArrays");

            context.Source.WriteInt32(def.Version);
            context.Source.WriteUInt32(def.FormatChecksum);
            context.Source.WriteInt32(def.EnumCount);
            context.Source.WriteInt32(PointerRaw(def.EnumCount));
            context.Source.WriteInt32(def.StructCount);
            context.Source.WriteInt32(PointerRaw(def.StructCount));
            context.Source.WriteInt32(def.IndexedArrayCount);
            context.Source.WriteInt32(PointerRaw(def.IndexedArrayCount));
            context.Source.WriteInt32(def.EnumedArrayCount);
            context.Source.WriteInt32(PointerRaw(def.EnumedArrayCount));
            EmitStructuredDataType(context, def.RootType);
            context.Source.WriteUInt32(def.Size);
        }

        for (int i = 0; i < defs.Count; i++)
        {
            StructuredDataDef def = defs[i];
            XBlockAddress rowAddress = tableAddress.Add(checked(i * StructuredDataDef.SerializedSize));

            if (def.EnumCount > 0)
                EmitEnumArray(context, rowAddress.Add(0x0C), def.Enums);
            if (def.StructCount > 0)
                EmitStructArray(context, rowAddress.Add(0x14), def.Structs);
            if (def.IndexedArrayCount > 0)
                EmitIndexedArray(context, rowAddress.Add(0x1C), def.IndexedArrays);
            if (def.EnumedArrayCount > 0)
                EmitEnumedArray(context, rowAddress.Add(0x24), def.EnumedArrays);
        }
    }

    private static void EmitEnumArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataEnum> enums)
    {
        XBlockAddress tableAddress = PatchAndAllocateTable(context, pointerCellAddress, enums.Count, StructuredDataEnum.SerializedSize);

        for (int i = 0; i < enums.Count; i++)
        {
            StructuredDataEnum value = enums[i];
            ValidateCount(value.EntryCount, value.Entries.Count, $"StructuredDataEnum[{i}].entries");

            context.Source.WriteInt32(value.EntryCount);
            context.Source.WriteInt32(value.ReservedEntryCount);
            context.Source.WriteInt32(PointerRaw(value.EntryCount));
        }

        for (int i = 0; i < enums.Count; i++)
        {
            if (enums[i].EntryCount > 0)
                EmitEnumEntryArray(context, tableAddress.Add(checked(i * StructuredDataEnum.SerializedSize + 0x08)), enums[i].Entries);
        }
    }

    private static void EmitEnumEntryArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataEnumEntry> entries)
    {
        XBlockAddress tableAddress = PatchAndAllocateTable(context, pointerCellAddress, entries.Count, StructuredDataEnumEntry.SerializedSize);

        for (int i = 0; i < entries.Count; i++)
        {
            context.Source.WriteInt32(PointerRaw(entries[i].String));
            context.Source.WriteUInt16(entries[i].Index);
            context.Source.WriteUInt16(entries[i].Padding);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].String is { } value)
                EmitInlineXString(context, tableAddress.Add(checked(i * StructuredDataEnumEntry.SerializedSize)), value);
        }
    }

    private static void EmitStructArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataStruct> structs)
    {
        XBlockAddress tableAddress = PatchAndAllocateTable(context, pointerCellAddress, structs.Count, StructuredDataStruct.SerializedSize);

        for (int i = 0; i < structs.Count; i++)
        {
            StructuredDataStruct value = structs[i];
            ValidateCount(value.PropertyCount, value.Properties.Count, $"StructuredDataStruct[{i}].properties");

            context.Source.WriteInt32(value.PropertyCount);
            context.Source.WriteInt32(PointerRaw(value.PropertyCount));
            context.Source.WriteInt32(value.Size);
            context.Source.WriteUInt32(value.BitOffset);
        }

        for (int i = 0; i < structs.Count; i++)
        {
            if (structs[i].PropertyCount > 0)
                EmitStructPropertyArray(context, tableAddress.Add(checked(i * StructuredDataStruct.SerializedSize + 0x04)), structs[i].Properties);
        }
    }

    private static void EmitStructPropertyArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataStructProperty> properties)
    {
        XBlockAddress tableAddress = PatchAndAllocateTable(context, pointerCellAddress, properties.Count, StructuredDataStructProperty.SerializedSize);

        for (int i = 0; i < properties.Count; i++)
        {
            context.Source.WriteInt32(PointerRaw(properties[i].Name));
            EmitStructuredDataType(context, properties[i].Type);
            context.Source.WriteUInt32(properties[i].Offset);
        }

        for (int i = 0; i < properties.Count; i++)
        {
            if (properties[i].Name is { } name)
                EmitInlineXString(context, tableAddress.Add(checked(i * StructuredDataStructProperty.SerializedSize)), name);
        }
    }

    private static void EmitIndexedArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataIndexedArray> values)
    {
        PatchAndAllocateTable(context, pointerCellAddress, values.Count, StructuredDataIndexedArray.SerializedSize);

        foreach (StructuredDataIndexedArray value in values)
        {
            context.Source.WriteInt32(value.ArraySize);
            EmitStructuredDataType(context, value.ElementType);
            context.Source.WriteUInt32(value.ElementSize);
        }
    }

    private static void EmitEnumedArray(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StructuredDataEnumedArray> values)
    {
        PatchAndAllocateTable(context, pointerCellAddress, values.Count, StructuredDataEnumedArray.SerializedSize);

        foreach (StructuredDataEnumedArray value in values)
        {
            context.Source.WriteInt32(value.EnumIndex);
            EmitStructuredDataType(context, value.ElementType);
            context.Source.WriteUInt32(value.ElementSize);
        }
    }

    private static XBlockAddress PatchAndAllocateTable(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        int count,
        int stride)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Inline table emit requires a positive element count.");

        context.Blocks.PatchInlinePointerCell(pointerCellAddress, alignment: 4);
        return context.Blocks.AllocateCurrent(checked(count * stride));
    }

    private static void EmitStructuredDataType(XEmitContext context, StructuredDataType value)
    {
        context.Source.WriteInt32((int)value.Type);
        context.Source.WriteInt32(value.UnionValue);
    }

    private static void EmitInlineXString(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        string value)
    {
        context.Blocks.PatchInlinePointerCell(pointerCellAddress);
        context.Blocks.AllocateCurrent(checked(System.Text.Encoding.Latin1.GetByteCount(value) + 1));
        context.Source.WriteCString(value);
    }

    private static int PointerRaw(int count)
    {
        if (count < 0)
            throw new InvalidDataException($"Negative pointer-backed count {count}.");

        return count == 0 ? 0 : -1;
    }

    private static int PointerRaw(string? value)
    {
        return value is null ? 0 : -1;
    }

    private static void ValidateCount(int declared, int actual, string owner)
    {
        if (declared < 0)
            throw new InvalidDataException($"{owner} has negative declared count {declared}.");

        if (declared != actual)
            throw new InvalidDataException($"{owner} declared count {declared} does not match {actual} value(s).");
    }
}
