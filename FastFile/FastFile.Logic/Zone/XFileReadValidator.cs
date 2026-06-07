using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal static class XFileReadValidator
{
    public static bool Enabled { get; } = IsEnabled("FASTFILE_VALIDATE_ASSETS");

    public static void ValidateCount(
        ref XFileReadContext context,
        string fieldPath,
        int value,
        int min,
        int max,
        string evidence)
    {
        if (!Enabled)
            return;

        if (value >= min && value <= max)
            return;

        throw new InvalidDataException(
            $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"zone=0x{context.Position:X8}: {fieldPath}={value:N0} is outside [{min:N0}, {max:N0}]. "
            + evidence);
    }

    public static void ValidateEnum<TEnum>(
        ref XFileReadContext context,
        string fieldPath,
        TEnum value,
        string evidence)
        where TEnum : struct, Enum
    {
        if (!Enabled)
            return;

        if (Enum.IsDefined(value))
            return;

        throw new InvalidDataException(
            $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"zone=0x{context.Position:X8}: {fieldPath}=0x{Convert.ToUInt64(value):X} is not a known {typeof(TEnum).Name}. "
            + evidence);
    }

    public static void ValidateShaderArgumentType(
        ref XFileReadContext context,
        string fieldPath,
        ushort raw,
        string evidence)
    {
        if (!Enabled)
            return;

        if (raw <= 7)
            return;

        throw new InvalidDataException(
            $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"zone=0x{context.Position:X8}: {fieldPath}=0x{raw:X4} is not a known MaterialShaderArgumentType. "
            + evidence);
    }

    public static void ValidatePointerShape(
        XFileReadContext context,
        Pointer pointer,
        Type valueType)
    {
        if (!Enabled || pointer.Kind != PointerKind.Offset)
            return;

        var maxBlock = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
        if (pointer.StreamBlockIndex < 0 || pointer.StreamBlockIndex >= maxBlock)
        {
            throw new InvalidDataException(
                $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
                + $"zone=0x{context.Position:X8}: {DescribePointer(pointer, valueType)} encodes invalid stream block "
                + $"{pointer.StreamBlockIndex:N0}; valid block indexes are 0..{maxBlock - 1:N0}.");
        }

        if (!context.TryGetStreamBlockSize(pointer.StreamBlockIndex, out var blockSize))
            return;

        if (blockSize <= 0)
        {
            throw new InvalidDataException(
                $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
                + $"zone=0x{context.Position:X8}: {DescribePointer(pointer, valueType)} targets empty stream block "
                + $"{pointer.StreamBlockIndex:N0}.");
        }

        if (pointer.Offset < 0 || pointer.Offset >= blockSize)
        {
            throw new InvalidDataException(
                $"Reader validation failed; EBOOT audit required at asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
                + $"zone=0x{context.Position:X8}: {DescribePointer(pointer, valueType)} targets offset "
                + $"0x{pointer.Offset:X8}, outside stream block {pointer.StreamBlockIndex:N0} size 0x{blockSize:X8}.");
        }
    }

    private static string DescribePointer(Pointer pointer, Type valueType)
    {
        var field = string.IsNullOrWhiteSpace(pointer.FieldPath)
            ? "<unknown>"
            : pointer.FieldPath;
        return $"{field} ({valueType.Name}) raw=0x{pointer.Raw:X8} kind={pointer.Kind}";
    }

    private static bool IsEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }
}
