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
        XBlockAddress? insertPatchAddress = null;

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
                insertPatchAddress = AllocateInsertPointerCell();
                ptr.Address = inlineAddressFactory();
                break;

            default:
                throw new InvalidDataException($"Unknown pointer kind {ptr.Kind}.");
        }

        PatchPointer(ptr);
        PatchInsertPointer(insertPatchAddress, ptr.Address);
        return true;
    }

    private bool TryMaterializeCurrentStreamPointer<T>(
        XPointer<T> ptr,
        int alignment = 4)
    {
        XBlockAddress? insertPatchAddress = null;

        switch (ptr.Kind)
        {
            case PointerKind.Null:
                return false;

            case PointerKind.Offset:
                ptr.Address = DecodeXPointer(ptr.Raw);
                PatchPointer(ptr);
                return false;

            case PointerKind.Insert:
                insertPatchAddress = AllocateInsertPointerCell();
                break;
        }

        _activeBlock.Align(alignment);
        ptr.Address = _activeBlock.Address;

        PatchPointer(ptr);
        PatchInsertPointer(insertPatchAddress, ptr.Address);
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
        PointerResolutionKind resolutionKind,
        bool patchEmittedCell = true)
    {
        int raw = Span.ReadInt32(ref _position);

        XBlockAddress? patchAddress = null;

        int patchOffset = _activeBlock.Position;
        _activeBlock.WriteInt32(raw);

        if (patchEmittedCell)
        {
            patchAddress = new XBlockAddress(
                _activeBlock.BlockType,
                patchOffset);
        }

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

        PatchPointerCell(ptr.PatchAddress.Value, ptr.Address.Value);
    }

    private XBlockAddress AllocateInsertPointerCell()
    {
        var block = _streamBlocks[(int)XFILE_BLOCK.LARGE];
        block.Align(4);

        var address = block.Address;
        block.WriteInt32(0);

        return address;
    }

    private void PatchInsertPointer(
        XBlockAddress? insertPatchAddress,
        XBlockAddress? valueAddress)
    {
        if (insertPatchAddress is null || valueAddress is null)
            return;

        PatchPointerCell(insertPatchAddress.Value, valueAddress.Value);
    }

    private void PatchPointerCell(XBlockAddress patchAddress, XBlockAddress valueAddress)
    {
        int encoded = EncodeBlockAddress(valueAddress);

        _streamBlocks[(int)patchAddress.Block]
            .PatchInt32(patchAddress.Offset, encoded);
    }
}
