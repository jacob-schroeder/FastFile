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

    public int ReadInt32(XBlockAddress address)
    {
        return GetBlock(address.Block).ReadInt32(address.Offset);
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
        DB_PushStreamPos(block);

        try
        {
            action();
        }
        finally
        {
            DB_PopStreamPos();
        }
    }

    public T WithBlock<T>(XFILE_BLOCK block, Func<T> func)
    {
        DB_PushStreamPos(block);

        try
        {
            return func();
        }
        finally
        {
            DB_PopStreamPos();
        }
    }

    public XPointerMaterializationResult MaterializePointer<T>(
        XPointer<T> pointer,
        XPointerMaterializationPlan plan)
    {
        if (plan.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{plan.Target} pointer has unknown resolution semantics.");

        XBlockAddress? insertPatchAddress = null;

        switch (pointer.Kind)
        {
            case PointerKind.Null:
                return new XPointerMaterializationResult(pointer.Kind, null, ShouldReadPayload: false);

            case PointerKind.Offset:
                pointer.Address = ResolveOffsetPointerAddress(pointer.Raw, plan);
                PatchPointer(pointer);
                return new XPointerMaterializationResult(pointer.Kind, pointer.Address, plan.ReadOffsetPayload);

            case PointerKind.Inline:
                pointer.Address = ResolvePayloadAddress(plan);
                break;

            case PointerKind.Insert:
                insertPatchAddress = AllocateInsertPointerCell();
                pointer.Address = ResolvePayloadAddress(plan);
                break;

            default:
                throw new InvalidDataException($"Unknown pointer kind {pointer.Kind}.");
        }

        PatchPointer(pointer);
        PatchInsertPointer(insertPatchAddress, pointer.Address);
        return new XPointerMaterializationResult(pointer.Kind, pointer.Address, ShouldReadPayload: true);
    }

    public XBlockAddress AllocatePointerPayload(XFILE_BLOCK block, int alignment)
    {
        var streamBlock = GetBlock(block);
        streamBlock.DB_AllocStreamPos(ToDBAllocStreamPosMask(alignment));

        return streamBlock.Address;
    }

    private XBlockAddress ResolvePayloadAddress(XPointerMaterializationPlan plan)
    {
        return plan.AddressMode switch
        {
            XPointerPayloadAddressMode.BlockPosition => GetAddress(plan.PayloadBlock),
            XPointerPayloadAddressMode.AllocatedBlock => AllocatePointerPayload(plan.PayloadBlock, plan.Alignment),
            XPointerPayloadAddressMode.CurrentStream => AllocateCurrentStreamPayload(plan.Alignment),
            _ => throw new InvalidDataException($"Unsupported pointer payload address mode {plan.AddressMode}.")
        };
    }

    private XBlockAddress AllocateCurrentStreamPayload(int alignment)
    {
        ActiveBlock.DB_AllocStreamPos(ToDBAllocStreamPosMask(alignment));
        return ActiveBlock.Address;
    }

    private XBlockAddress ResolveOffsetPointerAddress(int raw, XPointerMaterializationPlan plan)
    {
        var address = XPointerCodec.DecodeOffset(raw);
        if (!plan.OffsetIsAliasCell)
            return address;

        int aliasedRaw = ReadInt32(address);
        if (aliasedRaw == 0)
            // Forward alias cells stay zero until a later insert target patches them.
            return address;

        if (XPointerCodec.GetKind(aliasedRaw) != PointerKind.Offset)
        {
            throw new InvalidDataException(
                $"Expected alias cell at {address.Block}:0x{address.Offset:X} to contain a packed offset, got 0x{aliasedRaw:X8}.");
        }

        return XPointerCodec.DecodeOffset(aliasedRaw);
    }

    public XBlockAddress AllocateInsertPointerCell()
    {
        var block = GetBlock(XFILE_BLOCK.LARGE);
        block.DB_AllocStreamPos(ToDBAllocStreamPosMask(4));

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

            // PS3 loaders can allocate into zero-sized destination arenas such
            // as common_mp's RUNTIME block. Preserve strict header matching for
            // nonzero serialized blocks, but allow zero-sized blocks to grow.
            if (expectedSize != 0 && block.WrittenSpan.Length > expectedSize)
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

    public IReadOnlyList<XBlockStreamSnapshot> GetBlockStreamSnapshots()
    {
        return [.._blocks.Select(block => new XBlockStreamSnapshot(
            block.BlockType,
            block.DeclaredSize,
            block.BlockSpan.ToArray()))];
    }

    private static int ToDBAllocStreamPosMask(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        return alignment - 1;
    }

    private void DB_PushStreamPos(XFILE_BLOCK block)
    {
        _blockStack.Push(new StreamBlockFrame(
            ActiveBlock.BlockType,
            GetBlock(block).Position));

        ActiveBlock = GetBlock(block);
    }

    private void DB_PopStreamPos()
    {
        var frame = _blockStack.Pop();

        if (ActiveBlock.BlockType == XFILE_BLOCK.TEMP)
            ActiveBlock.Seek(frame.PushedPosition);

        ActiveBlock = GetBlock(frame.PreviousBlock);
    }
}
