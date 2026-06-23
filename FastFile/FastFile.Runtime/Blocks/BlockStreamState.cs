using FastFile.Models.Zone;

namespace FastFile.Runtime.Blocks;

public sealed class BlockStreamState
{
    private readonly Stack<XFileBlockType> _stack = new();

    public XFileBlockType CurrentBlock { get; private set; } = XFileBlockType.TEMP;
    public int[] BlockSizes { get; private set; } = [];

    public void Initialize(XFile xfile)
    {
        BlockSizes = xfile.BlockSize;
        CurrentBlock = XFileBlockType.TEMP;
        _stack.Clear();
    }

    public void Push(XFileBlockType block)
    {
        _stack.Push(CurrentBlock);
        CurrentBlock = block;
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException("DB stream block stack underflow.");

        CurrentBlock = _stack.Pop();
    }
}
