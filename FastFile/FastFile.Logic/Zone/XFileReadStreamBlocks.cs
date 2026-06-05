using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal sealed class XFileReadStreamBlocks
{
    private const int Ps3AssetStreamLeadInSize = 0x58;

    private readonly int[] _positions;
    private readonly Stack<XFileStreamBlockStackEntry> _blockStack = new();
    private int _activeBlockIndex;

    public XFileReadStreamBlocks(XFile header, int initialZonePosition)
    {
        var blockCount = Math.Max((int)XFILE_BLOCK.MAX_XFILE_COUNT, header.BlockSize.Length);
        _positions = new int[blockCount];
        _positions[(int)XFILE_BLOCK.TEMP] = GetBlockSize(header, XFILE_BLOCK.TEMP);
        _positions[(int)XFILE_BLOCK.LARGE] =
            GetBlockSize(header, XFILE_BLOCK.TEMP) + Ps3AssetStreamLeadInSize;
        _activeBlockIndex = (int)XFILE_BLOCK.TEMP;
    }

    public int ActiveBlockIndex => _activeBlockIndex;
    public int ActivePosition => _positions[_activeBlockIndex];

    public XFileBlockAddress ActiveAddress => new(_activeBlockIndex, ActivePosition);

    public void PushStreamBlock(XFILE_BLOCK block)
    {
        PushStreamBlock((int)block);
    }

    public void PushStreamBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= _positions.Length)
            throw new InvalidDataException($"Invalid XFile stream block index {blockIndex:N0}.");

        var previousBlockIndex = _activeBlockIndex;
        _activeBlockIndex = blockIndex;
        _blockStack.Push(new XFileStreamBlockStackEntry(previousBlockIndex, _positions[_activeBlockIndex]));
    }

    public void PopStreamBlock()
    {
        if (_blockStack.Count == 0)
            throw new InvalidDataException("Cannot pop an XFile stream block because the stream block stack is empty.");

        var entry = _blockStack.Pop();
        if (_activeBlockIndex == (int)XFILE_BLOCK.TEMP)
            _positions[_activeBlockIndex] = entry.SavedActivePosition;

        _activeBlockIndex = entry.PreviousBlockIndex;
    }

    public XFileBlockAddress Align(int alignment)
    {
        if (alignment <= 0)
            throw new InvalidDataException($"Cannot align XFile stream block with invalid alignment {alignment:N0}.");

        var position = ActivePosition;
        var remainder = position % alignment;
        if (remainder != 0)
            _positions[_activeBlockIndex] = position + alignment - remainder;

        return ActiveAddress;
    }

    public XFileBlockAddress Reserve(
        XFILE_BLOCK block,
        int alignment,
        int byteCount)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Cannot reserve a negative XFile stream byte count: {byteCount:N0}.");

        PushStreamBlock(block);
        try
        {
            Align(alignment);
            var address = ActiveAddress;
            Advance(byteCount);
            return address;
        }
        finally
        {
            PopStreamBlock();
        }
    }

    public XFileBlockAddress ReserveInsertSlot()
    {
        return Reserve(XFILE_BLOCK.LARGE, alignment: 4, byteCount: 4);
    }

    public void Advance(int byteCount)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Cannot advance XFile stream block by a negative byte count: {byteCount:N0}.");

        _positions[_activeBlockIndex] += byteCount;
    }

    private static int GetBlockSize(XFile header, XFILE_BLOCK block)
    {
        var index = (int)block;
        return index >= 0 && index < header.BlockSize.Length
            ? header.BlockSize[index]
            : 0;
    }

    private readonly record struct XFileStreamBlockStackEntry(
        int PreviousBlockIndex,
        int SavedActivePosition);
}
