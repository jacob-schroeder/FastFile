using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;

namespace FastFile.LogicOLD.Zone;

internal static class XPointerCodec
{
    public static XBlockAddress DecodeOffset(int raw)
    {
        uint value = unchecked((uint)raw);

        var block = (XFILE_BLOCK)(value >> 28);
        int offset = (int)(value & 0x0FFFFFFF) - 1;

        return new XBlockAddress(block, offset);
    }

    public static int EncodeAddress(XBlockAddress address)
    {
        return XPointer.EncodeOffset((int)address.Block, address.Offset);
    }

    public static PointerKind GetKind(int raw)
    {
        return raw switch
        {
            0 => PointerKind.Null,
            -1 => PointerKind.Inline,
            -2 => PointerKind.Insert,
            _ => PointerKind.Offset
        };
    }

    public static XPointer<T> CreatePointer<T>(
        int raw,
        PointerResolutionKind resolutionKind,
        XBlockAddress? patchAddress = null)
    {
        return new XPointer<T>
        {
            Raw = raw,
            Kind = GetKind(raw),
            ResolutionKind = resolutionKind,
            PatchAddress = patchAddress
        };
    }

    public static XPointer<T> ReinterpretPointer<T>(
        XPointer<object> pointer,
        PointerResolutionKind resolutionKind)
    {
        return new XPointer<T>
        {
            Raw = pointer.Raw,
            Kind = pointer.Kind,
            ResolutionKind = resolutionKind,
            PatchAddress = pointer.PatchAddress,
            Address = pointer.Address
        };
    }
}
