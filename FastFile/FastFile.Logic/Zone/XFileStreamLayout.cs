using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal sealed class XFileStreamLayout
{
    private const int XFileHeaderSize = 0x24;
    private readonly int _xfileSize;
    private readonly int[] _blockSizes;
    private readonly int[] _blockZoneBases;

    public XFileStreamLayout(XFile header)
        : this(header.Size, header.BlockSize)
    {
    }

    public XFileStreamLayout(int xfileSize, int[] blockSizes)
    {
        _xfileSize = xfileSize;
        _blockSizes = blockSizes.ToArray();
        _blockZoneBases = GetBlockZoneBases(_xfileSize, _blockSizes);
    }

    public bool TryGetZoneOffset(Pointer pointer, out int zoneOffset)
    {
        zoneOffset = 0;

        if (pointer.Kind != PointerKind.Offset
            || pointer.StreamBlockIndex < 0
            || pointer.StreamBlockIndex >= _blockSizes.Length
            || _blockSizes[pointer.StreamBlockIndex] <= 0
            || pointer.Offset < 0
            || pointer.Offset >= _blockSizes[pointer.StreamBlockIndex])
        {
            return false;
        }

        zoneOffset = _blockZoneBases[pointer.StreamBlockIndex] + pointer.Offset;
        return true;
    }

    public bool TryGetStreamOffset(int streamBlockIndex, int zoneOffset, out int streamOffset)
    {
        streamOffset = 0;

        if (streamBlockIndex < 0
            || streamBlockIndex >= _blockSizes.Length
            || _blockSizes[streamBlockIndex] <= 0)
        {
            return false;
        }

        streamOffset = zoneOffset - _blockZoneBases[streamBlockIndex];
        return streamOffset >= 0 && streamOffset < _blockSizes[streamBlockIndex];
    }

    private static int[] GetBlockZoneBases(int xfileSize, int[] blockSizes)
    {
        var blockBases = new int[blockSizes.Length];
        var position = Math.Max(0, xfileSize - blockSizes.Sum());
        var largeIndex = (int)XFILE_BLOCK.LARGE;

        for (var i = 0; i < blockBases.Length; i++)
        {
            if (i == largeIndex)
                continue;

            blockBases[i] = position;
            position += blockSizes[i];
        }

        if (largeIndex >= 0 && largeIndex < blockBases.Length)
            blockBases[largeIndex] = Math.Max(0, xfileSize - blockSizes[largeIndex]);

        return blockBases;
    }
}
