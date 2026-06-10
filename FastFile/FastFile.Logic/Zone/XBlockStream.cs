using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal sealed class XBlockStream
{
    private readonly XBlock[] _blocks;
    private readonly Stack<StreamBlockFrame> _blockStack = new();

    public XBlock ActiveBlock { get; private set; }
    public XBlock[] Blocks => _blocks;
    public XFILE_BLOCK ActiveBlockType => ActiveBlock.BlockType;
    public int ActivePosition => ActiveBlock.Position;
    public XBlockAddress ActiveAddress => ActiveBlock.Address;

    private readonly record struct StreamBlockFrame(
        XFILE_BLOCK PreviousBlock,
        int PushedPosition);

    public XBlockStream(
        IReadOnlyList<int> blockSizes,
        XFILE_BLOCK initialBlock = XFILE_BLOCK.TEMP)
    {
        _blocks = new XBlock[blockSizes.Count];

        for (int i = 0; i < blockSizes.Count; i++)
            _blocks[i] = new XBlock((XFILE_BLOCK)i, blockSizes[i]);

        ActiveBlock = GetBlock(initialBlock);
    }

    public XBlock GetBlock(XFILE_BLOCK block)
    {
        int index = (int)block;
        if (index < 0 || index >= _blocks.Length)
            throw new InvalidDataException($"Invalid XFILE block {block}.");

        return _blocks[index];
    }

    public bool ContainsBlock(XFILE_BLOCK block)
    {
        int index = (int)block;
        return index >= 0 && index < _blocks.Length;
    }

    public int GetPosition(XFILE_BLOCK block)
    {
        return GetBlock(block).Position;
    }

    public ReadOnlySpan<byte> GetWrittenSpan(XFILE_BLOCK block)
    {
        return GetBlock(block).WrittenSpan;
    }

    public ReadOnlySpan<byte> GetBlockSpan(XFILE_BLOCK block)
    {
        return GetBlock(block).BlockSpan;
    }

    public XBlockAddress GetAddress(XFILE_BLOCK block)
    {
        return GetBlock(block).Address;
    }

    public void WriteInt32(int value)
    {
        ActiveBlock.WriteInt32(value);
    }

    public void Write(byte[] value)
    {
        ActiveBlock.Write(value);
    }

    public void WriteCString(string value)
    {
        ActiveBlock.WriteCString(value);
    }

    public void PatchInt32(XBlockAddress address, int value)
    {
        GetBlock(address.Block).PatchInt32(address.Offset, value);
    }

    public void SeekOrVerify(int expectedOffset)
    {
        int currentOffset = ActiveBlock.Position;

        if (currentOffset != expectedOffset)
        {
            throw new InvalidDataException(
                $"Expected {ActiveBlock.BlockType} offset 0x{expectedOffset:X}, " +
                $"but current offset is 0x{currentOffset:X}.");
        }
    }

    public void WithBlock(XFILE_BLOCK block, Action action)
    {
        PushBlock(block);

        try
        {
            action();
        }
        finally
        {
            PopBlock();
        }
    }

    public T WithBlock<T>(XFILE_BLOCK block, Func<T> func)
    {
        PushBlock(block);

        try
        {
            return func();
        }
        finally
        {
            PopBlock();
        }
    }

    public bool TryMaterializePointer<T>(
        XPointer<T> pointer,
        Func<XBlockAddress> inlineAddressFactory)
    {
        XBlockAddress? insertPatchAddress = null;

        switch (pointer.Kind)
        {
            case PointerKind.Null:
                return false;

            case PointerKind.Offset:
                pointer.Address = XPointerCodec.DecodeOffset(pointer.Raw);
                break;

            case PointerKind.Inline:
                pointer.Address = inlineAddressFactory();
                break;

            case PointerKind.Insert:
                insertPatchAddress = AllocateInsertPointerCell();
                pointer.Address = inlineAddressFactory();
                break;

            default:
                throw new InvalidDataException($"Unknown pointer kind {pointer.Kind}.");
        }

        PatchPointer(pointer);
        PatchInsertPointer(insertPatchAddress, pointer.Address);
        return true;
    }

    public bool TryMaterializeCurrentStreamPointer<T>(
        XPointer<T> pointer,
        int alignment = 4)
    {
        XBlockAddress? insertPatchAddress = null;

        switch (pointer.Kind)
        {
            case PointerKind.Null:
                return false;

            case PointerKind.Offset:
                pointer.Address = XPointerCodec.DecodeOffset(pointer.Raw);
                PatchPointer(pointer);
                return false;

            case PointerKind.Inline:
                break;

            case PointerKind.Insert:
                insertPatchAddress = AllocateInsertPointerCell();
                break;

            default:
                throw new InvalidDataException($"Unknown pointer kind {pointer.Kind}.");
        }

        ActiveBlock.Align(alignment);
        pointer.Address = ActiveBlock.Address;

        PatchPointer(pointer);
        PatchInsertPointer(insertPatchAddress, pointer.Address);
        return true;
    }

    public XBlockAddress AllocatePointerPayload(XFILE_BLOCK block, int alignment)
    {
        var streamBlock = GetBlock(block);
        streamBlock.Align(alignment);

        return streamBlock.Address;
    }

    public XBlockAddress AllocateInsertPointerCell()
    {
        var block = GetBlock(XFILE_BLOCK.LARGE);
        block.Align(4);

        var address = block.Address;
        block.WriteInt32(0);

        return address;
    }

    public void PatchPointer<T>(XPointer<T> pointer)
    {
        if (pointer.Address is null || pointer.PatchAddress is null)
            return;

        PatchPointerCell(pointer.PatchAddress.Value, pointer.Address.Value);
    }

    public void PatchInsertPointer(
        XBlockAddress? insertPatchAddress,
        XBlockAddress? valueAddress)
    {
        if (insertPatchAddress is null || valueAddress is null)
            return;

        PatchPointerCell(insertPatchAddress.Value, valueAddress.Value);
    }

    public void PatchPointerCell(XBlockAddress patchAddress, XBlockAddress valueAddress)
    {
        int encoded = XPointerCodec.EncodeAddress(valueAddress);
        PatchInt32(patchAddress, encoded);
    }

    public void Seal(IReadOnlyList<int> expectedBlockSizes)
    {
        if (expectedBlockSizes.Count != _blocks.Length)
            throw new InvalidDataException(
                $"Expected {_blocks.Length} block sizes but received {expectedBlockSizes.Count}.");

        for (int i = 0; i < _blocks.Length; i++)
        {
            var block = _blocks[i];
            var expectedSize = expectedBlockSizes[i];

            if (block.WrittenSpan.Length > expectedSize)
            {
                throw new InvalidDataException(
                    $"{block.BlockType} stream length 0x{block.WrittenSpan.Length:X} exceeds header block size 0x{expectedSize:X}.");
            }

            block.PadToCapacity();
        }
    }

    public int[] GetWrittenBlockSizes()
    {
        return [.._blocks.Select(block => block.WrittenSpan.Length)];
    }

    private void PushBlock(XFILE_BLOCK block)
    {
        _blockStack.Push(new StreamBlockFrame(
            ActiveBlock.BlockType,
            GetBlock(block).Position));

        ActiveBlock = GetBlock(block);
    }

    private void PopBlock()
    {
        var frame = _blockStack.Pop();

        if (ActiveBlock.BlockType == XFILE_BLOCK.TEMP)
            ActiveBlock.Seek(frame.PushedPosition);

        ActiveBlock = GetBlock(frame.PreviousBlock);
    }
}
