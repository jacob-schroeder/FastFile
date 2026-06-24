using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;

namespace FastFile.Models.Pointers;

public readonly record struct XPointerReference(
    int Raw,
    PointerType Type,
    XPointerResolutionMode ResolutionMode,
    XBlockAddress? PackedAddress,
    XBlockAddress? CellAddress)
{
    public XPointerOffsetMode OffsetMode => ResolutionMode.ToOffsetMode();
    public bool ConsumesSource => Type is PointerType.Inline;

    public XPointer<T> AsPointer<T>() => new(Raw, ResolutionMode, CellAddress);

    public static XPointerReference FromRaw(
        int raw,
        XPointerResolutionMode resolutionMode = XPointerResolutionMode.None,
        XBlockAddress? cellAddress = null)
    {
        PointerType type = XPointerCodec.GetType(raw);
        XBlockAddress? packedAddress = type == PointerType.Offset
            ? XPointerCodec.Decode(raw)
            : null;

        return new XPointerReference(raw, type, resolutionMode, packedAddress, cellAddress);
    }

    public static XPointerReference FromRaw(
        int raw,
        XPointerOffsetMode offsetMode,
        XBlockAddress? cellAddress = null)
    {
        return FromRaw(raw, offsetMode.ToResolutionMode(), cellAddress);
    }
}

public enum XPointerOffsetMode
{
    None,
    Direct,
    AliasCell
}

public static class XPointerResolutionModeExtensions
{
    public static XPointerResolutionMode ToResolutionMode(this XPointerOffsetMode mode)
    {
        return mode switch
        {
            XPointerOffsetMode.None => XPointerResolutionMode.None,
            XPointerOffsetMode.Direct => XPointerResolutionMode.Direct,
            XPointerOffsetMode.AliasCell => XPointerResolutionMode.AliasCell,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown pointer resolution mode.")
        };
    }

    public static XPointerOffsetMode ToOffsetMode(this XPointerResolutionMode mode)
    {
        return mode switch
        {
            XPointerResolutionMode.None => XPointerOffsetMode.None,
            XPointerResolutionMode.Direct => XPointerOffsetMode.Direct,
            XPointerResolutionMode.AliasCell => XPointerOffsetMode.AliasCell,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown pointer resolution mode.")
        };
    }
}
