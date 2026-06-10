using FastFile.Logic.Extensions;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private void SeekOrVerify(int expectedOffset)
    {
        int currentOffset = _activeBlock.Position;

        if (currentOffset != expectedOffset)
        {
            throw new InvalidDataException(
                $"Expected {_activeBlock.BlockType} offset 0x{expectedOffset:X}, " +
                $"but current offset is 0x{currentOffset:X}.");
        }
    }

    private bool TryMaterializePointer<T>(
        XPointer<T> ptr,
        Func<XBlockAddress> inlineAddressFactory)
    {
        switch (ptr.Kind)
        {
            case PointerKind.Null:
                return false;

            case PointerKind.Offset:
                ptr.Address = DecodeXPointer(ptr.Raw);
                break;

            case PointerKind.Inline:
                ptr.Address = inlineAddressFactory();
                break;

            case PointerKind.Insert:
                throw new NotSupportedException(
                    $"Insert pointer not implemented for {typeof(T).Name}.");

            default:
                throw new InvalidDataException($"Unknown pointer kind {ptr.Kind}.");
        }

        PatchPointer(ptr);
        return true;
    }

    private static XBlockAddress DecodeXPointer(int raw)
    {
        uint value = unchecked((uint)raw);

        var block = (XFILE_BLOCK)(value >> 28);
        int offset = (int)(value & 0x0FFFFFFF) - 1;

        return new XBlockAddress(block, offset);
    }

    private static int EncodeBlockAddress(XBlockAddress address)
    {
        return XPointer.EncodeOffset((int)address.Block, address.Offset);
    }

    private XPointer<T> ReadDirectPointer<T>() =>
        ReadPointer<T>(PointerResolutionKind.Direct);

    private XPointer<T> ReadAliasPointer<T>() =>
        ReadPointer<T>(PointerResolutionKind.Alias);

    private XPointer<T> ReadPointer<T>(
        PointerResolutionKind resolutionKind)
    {
        int raw = Span.ReadInt32(ref _position);

        XBlockAddress? patchAddress = null;

        int patchOffset = _activeBlock.Position;
        _activeBlock.WriteInt32(raw);

        patchAddress = new XBlockAddress(
            _activeBlock.BlockType,
            patchOffset);

        return new XPointer<T>
        {
            Raw = raw,
            Kind = raw switch
            {
                0 => PointerKind.Null,
                -1 => PointerKind.Inline,
                -2 => PointerKind.Insert,
                _ => PointerKind.Offset
            },
            ResolutionKind = resolutionKind,
            PatchAddress = patchAddress
        };
    }

    private void PatchPointer<T>(XPointer<T> ptr)
    {
        if (ptr.Address is null || ptr.PatchAddress is null)
            return;

        int encoded = EncodeBlockAddress(ptr.Address.Value);

        _streamBlocks[(int)ptr.PatchAddress.Value.Block]
            .PatchInt32(ptr.PatchAddress.Value.Offset, encoded);
    }
}