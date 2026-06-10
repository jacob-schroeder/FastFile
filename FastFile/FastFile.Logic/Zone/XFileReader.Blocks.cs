using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private void WithStreamBlock(XFILE_BLOCK block, Action action)
    {
        PushStreamBlock(block);

        try
        {
            action();
        }
        finally
        {
            PopStreamBlock();
        }
    }

    private T WithStreamBlock<T>(XFILE_BLOCK block, Func<T> func)
    {
        PushStreamBlock(block);

        try
        {
            return func();
        }
        finally
        {
            PopStreamBlock();
        }
    }

    private void PushStreamBlock(XFILE_BLOCK block)
    {
        _blockStack.Push(new StreamBlockFrame(
            _activeBlock.BlockType,
            block,
            _streamBlocks[(int)block].Position));

        _activeBlock = _streamBlocks[(int)block];
    }

    private void PopStreamBlock()
    {
        var frame = _blockStack.Pop();

        if (_activeBlock.BlockType == XFILE_BLOCK.TEMP)
            _activeBlock.Seek(frame.PushedPosition);

        _activeBlock = _streamBlocks[(int)frame.PreviousBlock];
    }
}
