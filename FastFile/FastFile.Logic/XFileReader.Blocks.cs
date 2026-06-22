
using FastFile.Logic.Streams;
using FastFile.Models.Zone;

namespace FastFile.Logic;

public partial class XFileReader
{
    private void SetupBlockStreams()
    {
        for (int i = 0; i < (int)XFileBlockType.COUNT; i++)
            _blockCursors[(XFileBlockType)i] = new BlockCursor(Header.BlockSize[i], debugName: ((XFileBlockType)i).ToString());

        _blocks = new XFileBlockStack(_blockCursors, XFileBlockType.TEMP);
        _reader = new MirroredReadCursor(_source, _blocks);
        _context = new EngineLoadContext(_reader, _blocks, _blockCursors);
    }

    public void DumpBlocks(string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (var item in _blockCursors.OrderBy(item => item.Key))
            File.WriteAllBytes(
                Path.Combine(directory, $"{(int)item.Key:D2}_{item.Key}.bin"),
                item.Value.ToArray());
    }
}
