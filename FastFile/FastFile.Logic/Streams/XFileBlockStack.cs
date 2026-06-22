using FastFile.Models;
using FastFile.Models.Zone;

namespace FastFile.Logic.Streams;

public sealed class XFileBlockStack(IReadOnlyDictionary<XFileBlockType, IBlockCursor> blocks, XFileBlockType initialBlockType)
{
    private readonly Stack<XFileBlockType> _frames = new();

    public XFileBlockType ActiveBlockType { get; private set; } = initialBlockType;
    public IBlockCursor ActiveCursor => blocks[ActiveBlockType];
    public XBlockAddress ActiveAddress => new(ActiveBlockType, (int)ActiveCursor.Position);

    public IDisposable Push(XFileBlockType blockType)
    {
        _frames.Push(ActiveBlockType);
        ActiveBlockType = blockType;
        return new Frame(this);
    }

    private void Pop()
    {
        if (!_frames.TryPop(out XFileBlockType previous))
            throw new InvalidOperationException("Block pop without matching push.");

        ActiveBlockType = previous;
    }

    private sealed class Frame(XFileBlockStack owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            owner.Pop();
            _disposed = true;
        }
    }
}
