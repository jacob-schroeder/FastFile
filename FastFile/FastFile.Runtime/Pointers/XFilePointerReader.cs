using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime.Blocks;
using FastFile.Runtime.IO;
using System.Reflection;

namespace FastFile.Runtime.Pointers;

public sealed class XFilePointerReader
{
    private readonly BlockStreamState _blocks;
    private readonly List<PointerValidationRecord> _validationRecords = new();
    private readonly List<PointerValidationRecord> _validationIssues = new();

    public XFilePointerReader(BlockStreamState blocks)
    {
        _blocks = blocks;
    }

    public IReadOnlyList<PointerValidationRecord> ValidationRecords => _validationRecords;
    public IReadOnlyList<PointerValidationRecord> ValidationIssues => _validationIssues;

    public int DirectTargetValidationCount { get; private set; }
    public int AliasCellValidationCount { get; private set; }
    public int ResolvedAliasTargetValidationCount { get; private set; }
    public int TypeProvenTargetValidationCount { get; private set; }
    public int UntypedTargetValidationCount { get; private set; }
    public int NullObjectPointerCount { get; private set; }
    public int NullAliasTargetCount { get; private set; }
    public int EmptyXStringTargetCount { get; private set; }

    public XPointerReference ReadCell(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode = XPointerOffsetMode.None)
    {
        int cellOffset = cursor.Offset;
        XPointerReference pointer = XPointerReference.FromRaw(
            cursor.ReadInt32(),
            offsetMode,
            cursor.AddressAt(cellOffset));
        ValidateOffsetPointer(pointer, null);
        return pointer;
    }

    public XPointerReference FromRaw(
        int raw,
        XPointerOffsetMode offsetMode = XPointerOffsetMode.None,
        XBlockAddress? cellAddress = null)
    {
        XPointerReference pointer = XPointerReference.FromRaw(raw, offsetMode, cellAddress);
        ValidateOffsetPointer(pointer, null);
        return pointer;
    }

    public XPointer<T> FromRaw<T>(
        int raw,
        XPointerResolutionMode resolutionMode = XPointerResolutionMode.None,
        XBlockAddress? cellAddress = null)
    {
        XPointerReference pointer = XPointerReference.FromRaw(raw, resolutionMode, cellAddress);
        RecordNullObjectPointer(pointer, typeof(T));
        ValidateOffsetPointer(pointer, typeof(T));
        return new XPointer<T>(raw, resolutionMode, cellAddress);
    }

    public XPointer<T> FromRaw<T>(
        int raw,
        XPointerOffsetMode resolutionMode,
        XBlockAddress? cellAddress = null)
    {
        XPointerReference pointer = XPointerReference.FromRaw(raw, resolutionMode, cellAddress);
        RecordNullObjectPointer(pointer, typeof(T));
        ValidateOffsetPointer(pointer, typeof(T));
        return new XPointer<T>(raw, resolutionMode.ToResolutionMode(), cellAddress);
    }

    public XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        XPointerResolutionMode resolutionMode = XPointerResolutionMode.None)
    {
        int cellOffset = cursor.Offset;
        return FromRaw<T>(cursor.ReadInt32(), resolutionMode, cursor.AddressAt(cellOffset));
    }

    public XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode)
    {
        int cellOffset = cursor.Offset;
        return FromRaw<T>(cursor.ReadInt32(), offsetMode, cursor.AddressAt(cellOffset));
    }

    public bool HasInlinePayload(XPointerReference pointer)
    {
        return pointer.Type is PointerType.Inline;
    }

    public XBlockAddress PatchInlinePointerCell(
        XBlockAddress cellAddress,
        int raw,
        int alignment)
    {
        XPointerReference pointer = XPointerReference.FromRaw(raw);
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Pointer cell {cellAddress} contains 0x{raw:X8}, not an inline/insert source sentinel.");

        if (alignment > 0)
            _blocks.AlignCurrent(alignment);

        XBlockAddress targetAddress = _blocks.CurrentAddress;
        _blocks.WriteInt32(cellAddress, XPointerCodec.Encode(targetAddress));
        return targetAddress;
    }

    public XBlockAddress PatchInlinePointerCell(
        XPointerReference pointer,
        int alignment)
    {
        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Pointer 0x{pointer.Raw:X8} has no destination cell address to patch.");

        return PatchInlinePointerCell(cellAddress, pointer.Raw, alignment);
    }

    public XBlockAddress PatchInlinePointerCell<T>(
        XPointer<T> pointer,
        int alignment)
    {
        return PatchInlinePointerCell(pointer.Untyped, alignment);
    }

    public string? LoadXString(
        FastFileCursor cursor,
        XBlockAddress pointerCellAddress,
        XPointer<string> pointer,
        int alignment = 0)
    {
        return LoadXString(cursor, pointerCellAddress, pointer.Untyped, alignment);
    }

    public string? LoadXString(
        FastFileCursor cursor,
        XBlockAddress pointerCellAddress,
        XPointerReference pointer,
        int alignment = 0)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.PackedAddress is not null)
        {
            ValidateOffsetPointer(pointer, typeof(string));
            return null;
        }

        if (!HasInlinePayload(pointer))
            return null;

        PatchInlinePointerCell(pointerCellAddress, pointer.Raw, alignment);
        return _blocks.LoadCString(cursor);
    }

    public string? LoadXString(
        FastFileCursor cursor,
        XPointer<string> pointer,
        int alignment = 0)
    {
        return LoadXString(cursor, pointer.Untyped, alignment);
    }

    public string? LoadXString(
        FastFileCursor cursor,
        XPointerReference pointer,
        int alignment = 0)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.PackedAddress is not null)
        {
            ValidateOffsetPointer(pointer, typeof(string));
            return null;
        }

        if (!HasInlinePayload(pointer))
            return null;

        PatchInlinePointerCell(pointer, alignment);
        return _blocks.LoadCString(cursor);
    }

    public byte[]? LoadBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment = 0)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.PackedAddress is not null)
        {
            ValidateOffsetPointerRange<byte[]>(pointer, byteCount, "byte[]");
            return null;
        }

        if (!HasInlinePayload(pointer))
            return null;

        PatchInlinePointerCell(pointer, alignment);
        return _blocks.Load(cursor, byteCount);
    }

    public T? ReadNullableInline<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        Func<T> readPayload,
        int alignment = 0)
        where T : class
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (!HasInlinePayload(pointer))
            return null;

        AlignIfNeeded(cursor, alignment);
        return readPayload();
    }

    public T ReadRequiredInline<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        Func<T> readPayload,
        string ownerName,
        int alignment = 0)
    {
        if (!HasInlinePayload(pointer))
            throw new InvalidDataException($"{ownerName} pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        AlignIfNeeded(cursor, alignment);
        return readPayload();
    }

    public string? ReadCString(FastFileCursor cursor, XPointerReference pointer)
    {
        return ReadNullableInline(cursor, pointer, cursor.ReadCString);
    }

    public byte[]? ReadBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment = 0)
    {
        return ReadNullableInline(cursor, pointer, () => cursor.ReadBytes(byteCount), alignment);
    }

    public void ReadInlinePayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        Action readPayload,
        int alignment = 0)
    {
        if (!HasInlinePayload(pointer))
            return;

        AlignIfNeeded(cursor, alignment);
        readPayload();
    }

    public void ValidateOffsetPointer<T>(XPointerReference pointer)
    {
        ValidateOffsetPointer(pointer, typeof(T));
    }

    public void ValidateOffsetPointerRange(
        XPointerReference pointer,
        int byteCount,
        string targetName)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        ValidateOffsetPointer(pointer, null, byteCount, targetName);
    }

    public void ValidateOffsetPointerRange<T>(
        XPointerReference pointer,
        int byteCount,
        string? targetName = null)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        ValidateOffsetPointer(pointer, typeof(T), byteCount, targetName);
    }

    public int ReadAliasCellRaw(XPointerReference pointer)
    {
        if (pointer.Type != PointerType.Offset || pointer.ResolutionMode != XPointerResolutionMode.AliasCell)
            throw new InvalidDataException($"Pointer 0x{pointer.Raw:X8} is not a packed alias-cell pointer.");

        if (pointer.PackedAddress is not { } sourceCell)
            throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} has no packed source cell.");

        ValidateRange(pointer.Raw, sourceCell, sizeof(int), "alias pointer cell", isAliasCell: true);
        return _blocks.ReadInt32(sourceCell);
    }

    private void AlignIfNeeded(FastFileCursor cursor, int alignment)
    {
        if (alignment < 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        if (alignment > 0)
        {
            int aligned = (cursor.Offset + alignment - 1) / alignment * alignment;
            int byteCount = aligned - cursor.Offset;
            _blocks.RecordSourcePadding(cursor, byteCount, "source alignment padding");
            cursor.Align(alignment);
        }
    }

    private void ValidateOffsetPointer(
        XPointerReference pointer,
        Type? targetType)
    {
        ValidateOffsetPointer(pointer, targetType, null, null);
    }

    private void ValidateOffsetPointer(
        XPointerReference pointer,
        Type? targetType,
        int? byteCountOverride,
        string? targetNameOverride)
    {
        if (pointer.PackedAddress is not { } address)
            return;

        string targetName = targetNameOverride ?? GetTargetName(targetType);
        bool typeProven = IsTypeProven(targetType);
        bool hasTargetContract = targetType is not null || byteCountOverride.HasValue || targetNameOverride is not null;

        if (pointer.ResolutionMode == XPointerResolutionMode.AliasCell)
        {
            ValidateRange(pointer.Raw, address, sizeof(int), $"{targetName} alias cell", isAliasCell: true);
            RecordValidation(
                severity: "Pass",
                kind: "AliasCell",
                targetName: $"{targetName} alias cell",
                targetType: "pointer cell",
                typeProven: true,
                pointer: pointer,
                resolvedTargetAddress: address,
                byteCount: sizeof(int),
                aliasedRaw: null,
                message: "Packed alias-cell pointer targets a materialized pointer cell.");

            int aliasedRaw = _blocks.ReadInt32(address);
            if (aliasedRaw == 0)
            {
                NullAliasTargetCount++;
                RecordValidation(
                    severity: "Inspect",
                    kind: "AliasCellNullTarget",
                    targetName: targetName,
                    targetType: GetTargetTypeName(targetType),
                    typeProven: typeProven,
                    pointer: pointer,
                    resolvedTargetAddress: null,
                    byteCount: byteCountOverride,
                    aliasedRaw: aliasedRaw,
                    message: "Alias-cell offset pointer resolved to a null object pointer.");
                return;
            }

            if (XPointerCodec.GetType(aliasedRaw) != PointerType.Offset)
            {
                RecordValidation(
                    severity: "Inspect",
                    kind: "AliasCellNonOffsetTarget",
                    targetName: targetName,
                    targetType: GetTargetTypeName(targetType),
                    typeProven: typeProven,
                    pointer: pointer,
                    resolvedTargetAddress: null,
                    byteCount: byteCountOverride,
                    aliasedRaw: aliasedRaw,
                    message: "Alias-cell offset pointer resolved to a non-offset raw pointer.");
                return;
            }

            XBlockAddress aliasedAddress = XPointerCodec.Decode(aliasedRaw);
            if (!hasTargetContract)
                return;

            ValidateTarget(
                sourcePointer: pointer,
                rawPointer: aliasedRaw,
                address: aliasedAddress,
                targetType: targetType,
                byteCountOverride: byteCountOverride,
                targetName: targetName,
                isResolvedAliasTarget: true,
                aliasedRaw: aliasedRaw);
            return;
        }

        if (!hasTargetContract)
            return;

        ValidateTarget(
            sourcePointer: pointer,
            rawPointer: pointer.Raw,
            address: address,
            targetType: targetType,
            byteCountOverride: byteCountOverride,
            targetName: targetName,
            isResolvedAliasTarget: false,
            aliasedRaw: null);
    }

    private void ValidateTarget(
        XPointerReference sourcePointer,
        int rawPointer,
        XBlockAddress address,
        Type? targetType,
        int? byteCountOverride,
        string targetName,
        bool isResolvedAliasTarget,
        int? aliasedRaw)
    {
        bool typeProven = IsTypeProven(targetType);
        if (targetType == typeof(string))
        {
            _blocks.ValidateMaterializedCString(address, targetName, rawPointer);
            bool isEmpty = _blocks.ReadByte(address) == 0;
            if (isEmpty)
                EmptyXStringTargetCount++;

            IncrementTargetValidation(isResolvedAliasTarget);
            IncrementTypeProof(typeProven);
            RecordValidation(
                severity: "Pass",
                kind: isResolvedAliasTarget ? "ResolvedAliasTarget" : "DirectTarget",
                targetName: targetName,
                targetType: GetTargetTypeName(targetType),
                typeProven: typeProven,
                pointer: sourcePointer,
                resolvedTargetAddress: address,
                byteCount: null,
                aliasedRaw: aliasedRaw,
                message: isEmpty
                    ? "XString offset pointer targets a materialized null string."
                    : "XString offset pointer targets a materialized null-terminated string.");
            return;
        }

        int byteCount = byteCountOverride ?? GetSerializedSize(targetType) ?? 1;
        if (byteCount == 0)
            return;

        ValidateRange(rawPointer, address, byteCount, targetName, isAliasCell: false);
        IncrementTargetValidation(isResolvedAliasTarget);
        IncrementTypeProof(typeProven);
        RecordValidation(
            severity: typeProven ? "Pass" : "Inspect",
            kind: isResolvedAliasTarget ? "ResolvedAliasTarget" : "DirectTarget",
            targetName: targetName,
            targetType: GetTargetTypeName(targetType),
            typeProven: typeProven,
            pointer: sourcePointer,
            resolvedTargetAddress: address,
            byteCount: byteCount,
            aliasedRaw: aliasedRaw,
            message: typeProven
                ? "Offset pointer targets materialized data validated through an XPointer<T> contract."
                : "Offset pointer range is materialized, but no nominal XPointer<T> target type was supplied.");
    }

    private void ValidateRange(
        int rawPointer,
        XBlockAddress address,
        int byteCount,
        string targetName,
        bool isAliasCell)
    {
        _blocks.ValidateMaterializedRange(address, byteCount, targetName, rawPointer);
        if (isAliasCell)
            AliasCellValidationCount++;
    }

    private void IncrementTargetValidation(bool isResolvedAliasTarget)
    {
        if (isResolvedAliasTarget)
            ResolvedAliasTargetValidationCount++;
        else
            DirectTargetValidationCount++;
    }

    private void IncrementTypeProof(bool typeProven)
    {
        if (typeProven)
            TypeProvenTargetValidationCount++;
        else
            UntypedTargetValidationCount++;
    }

    private void RecordNullObjectPointer(
        XPointerReference pointer,
        Type targetType)
    {
        if (pointer.Type != PointerType.Null || targetType == typeof(string))
            return;

        NullObjectPointerCount++;
        RecordValidation(
            severity: "Inspect",
            kind: "NullObjectPointer",
            targetName: GetTargetName(targetType),
            targetType: GetTargetTypeName(targetType),
            typeProven: IsTypeProven(targetType),
            pointer: pointer,
            resolvedTargetAddress: null,
            byteCount: null,
            aliasedRaw: null,
            message: "Typed XPointer<T> object pointer is null; XString nulls are the only null pointer category auto-passed.");
    }

    private void RecordValidation(
        string severity,
        string kind,
        string targetName,
        string? targetType,
        bool typeProven,
        XPointerReference pointer,
        XBlockAddress? resolvedTargetAddress,
        int? byteCount,
        int? aliasedRaw,
        string message)
    {
        var record = new PointerValidationRecord(
            Severity: severity,
            Kind: kind,
            TargetName: targetName,
            TargetType: targetType,
            TypeProven: typeProven,
            RawPointer: pointer.Raw,
            PointerType: pointer.Type,
            ResolutionMode: pointer.ResolutionMode.ToString(),
            PointerCellAddress: pointer.CellAddress,
            PackedAddress: pointer.PackedAddress,
            ResolvedTargetAddress: resolvedTargetAddress,
            ByteCount: byteCount,
            AliasedRaw: aliasedRaw,
            PointerCellBytes: severity == "Pass" ? null : FormatWindow(pointer.CellAddress),
            ResolvedTargetBytes: severity == "Pass" ? null : FormatWindow(resolvedTargetAddress),
            Message: message);

        _validationRecords.Add(record);
        if (severity != "Pass")
            _validationIssues.Add(record);
    }

    private string? FormatWindow(XBlockAddress? address)
    {
        if (address is not { } value)
            return null;

        try
        {
            return _blocks.FormatByteWindow(value, bytesBefore: 16, bytesAfter: 64);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return ex.Message;
        }
    }

    private static string GetTargetName(Type? targetType)
    {
        if (targetType is null)
            return "untyped pointer target";

        return targetType.IsArray
            ? $"{targetType.GetElementType()?.Name ?? "unknown"}[]"
            : targetType.Name;
    }

    private static string? GetTargetTypeName(Type? targetType)
    {
        if (targetType is null)
            return null;

        return targetType.FullName;
    }

    private static bool IsTypeProven(Type? targetType)
    {
        return targetType is not null && targetType != typeof(object);
    }

    private static int? GetSerializedSize(Type? targetType)
    {
        if (targetType is null || targetType == typeof(string) || targetType.IsArray)
            return null;

        FieldInfo? field = targetType.GetField(
            "SerializedSize",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return field?.FieldType == typeof(int)
            ? (int?)field.GetRawConstantValue()
            : null;
    }
}
