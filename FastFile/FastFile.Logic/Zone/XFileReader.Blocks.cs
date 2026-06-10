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
        _blockStack.Push(_activeBlock);
        _activeBlock = _streamBlocks[(int)block];
    }

    private void PopStreamBlock()
    {
        _activeBlock = _blockStack.Pop();
    }
}