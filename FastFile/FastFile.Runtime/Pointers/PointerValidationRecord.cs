using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;

namespace FastFile.Runtime.Pointers;

public sealed record PointerValidationRecord(
    string Severity,
    string Kind,
    string TargetName,
    string? TargetType,
    bool TypeProven,
    int RawPointer,
    PointerType PointerType,
    string ResolutionMode,
    XBlockAddress? PointerCellAddress,
    XBlockAddress? PackedAddress,
    XBlockAddress? ResolvedTargetAddress,
    int? ByteCount,
    int? AliasedRaw,
    string? PointerCellBytes,
    string? ResolvedTargetBytes,
    string Message)
{
    public bool IsTempRelated =>
        PointerCellAddress?.BlockType == XFileBlockType.TEMP ||
        PackedAddress?.BlockType == XFileBlockType.TEMP ||
        ResolvedTargetAddress?.BlockType == XFileBlockType.TEMP;
}
