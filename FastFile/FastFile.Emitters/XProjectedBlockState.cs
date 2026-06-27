using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Emitters;

public sealed class XProjectedBlockState
{
    private readonly Stack<StreamBlockFrame> _stack = new();
    private readonly int[] _positions = new int[(int)XFileBlockType.COUNT];
    private readonly int[] _highWater = new int[(int)XFileBlockType.COUNT];
    private readonly Dictionary<XBlockAddress, int> _pointerPatches = new();

    public XFileBlockType CurrentBlock { get; private set; } = XFileBlockType.TEMP;
    public XBlockAddress CurrentAddress => GetAddress(CurrentBlock);
    public IReadOnlyDictionary<XBlockAddress, int> PointerPatches => _pointerPatches;

    public void Push(XFileBlockType block)
    {
        _stack.Push(new StreamBlockFrame(CurrentBlock, GetPosition(block)));
        CurrentBlock = block;
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException("Projected DB stream block stack underflow.");

        StreamBlockFrame frame = _stack.Pop();

        if (CurrentBlock == XFileBlockType.TEMP)
            _positions[(int)XFileBlockType.TEMP] = frame.PushedBlockPosition;

        CurrentBlock = frame.PreviousBlock;
    }

    public XBlockAddress GetAddress(XFileBlockType block)
    {
        return new XBlockAddress(block, GetPosition(block));
    }

    public XBlockAddress AlignCurrent(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        int index = (int)CurrentBlock;
        _positions[index] = checked((_positions[index] + alignment - 1) / alignment * alignment);
        MarkMaterialized(index);
        return CurrentAddress;
    }

    public XBlockAddress AllocateCurrent(int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        XBlockAddress address = CurrentAddress;
        int index = (int)CurrentBlock;
        _positions[index] = checked(_positions[index] + byteCount);
        MarkMaterialized(index);
        return address;
    }

    public XBlockAddress PatchInlinePointerCell(XBlockAddress pointerCellAddress, int alignment = 0)
    {
        if (alignment > 0)
            AlignCurrent(alignment);

        XBlockAddress targetAddress = CurrentAddress;
        _pointerPatches[pointerCellAddress] = XPointerCodec.Encode(targetAddress);
        return targetAddress;
    }

    public int[] GetProjectedBlockSizes()
    {
        return _highWater.ToArray();
    }

    public string DescribePositions()
    {
        var parts = new List<string>(_positions.Length);
        for (int i = 0; i < _positions.Length; i++)
            parts.Add($"{(XFileBlockType)i}=0x{_positions[i]:X}/0x{_highWater[i]:X}");

        return string.Join(", ", parts);
    }

    private int GetPosition(XFileBlockType block)
    {
        int index = (int)block;
        if (index < 0 || index >= _positions.Length)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Invalid XFile block.");

        return _positions[index];
    }

    private void MarkMaterialized(int index)
    {
        _highWater[index] = Math.Max(_highWater[index], _positions[index]);
    }

    private readonly record struct StreamBlockFrame(
        XFileBlockType PreviousBlock,
        int PushedBlockPosition);
}
