using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;

namespace FastFile.Models.Pointers;

public readonly record struct XPointer<T>
{
    public XPointer(int raw)
        : this(raw, XPointerResolutionMode.None)
    {
    }

    public XPointer(int raw, XPointerResolutionMode resolutionMode)
    {
        Raw = raw;
        ResolutionMode = resolutionMode;
    }

    public XPointer(int raw, XPointerOffsetMode resolutionMode)
        : this(raw, resolutionMode.ToResolutionMode())
    {
    }

    public int Raw { get; }
    public int Value => Raw;
    public XPointerResolutionMode ResolutionMode { get; }
    public PointerType Type => XPointerCodec.GetType(Raw);
    public XBlockAddress? PackedAddress => Type == PointerType.Offset ? XPointerCodec.Decode(Raw) : null;
    public bool ConsumesSource => Type is PointerType.Inline or PointerType.Insert;

    public XPointerReference Untyped => XPointerReference.FromRaw(Raw, ResolutionMode);

    public override string ToString()
    {
        string address = PackedAddress is { } packedAddress
            ? $" {ResolutionMode}->{packedAddress}"
            : string.Empty;

        return $"0x{Raw:X8} {Type}{address}";
    }
}

public enum XPointerResolutionMode
{
    None,
    Direct,
    AliasCell
}
