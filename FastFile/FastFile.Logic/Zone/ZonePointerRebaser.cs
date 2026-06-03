using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal sealed class ZonePointerRebaser
{
    private const int StreamOffsetMask = 0x0FFFFFFF;

    private readonly int[] _oldBlockBases;
    private readonly int[] _oldBlockSizes;
    private readonly List<RebaseRange> _ranges = new();

    public ZonePointerRebaser(XFile header)
    {
        _oldBlockSizes = header.BlockSize.ToArray();
        _oldBlockBases = GetBlockBases(header.Size, _oldBlockSizes);
    }

    public void AddRange(int oldStart, int oldLength, int newStart, int newLength)
    {
        if (oldStart < 0 || oldLength <= 0 || newStart < 0 || newLength <= 0)
            return;

        _ranges.Add(new RebaseRange(oldStart, oldLength, newStart, newLength));
    }

    public bool TryRebase(
        Pointer pointer,
        int newXFileSize,
        int[] newBlockSizes,
        out int raw)
    {
        raw = pointer.Raw;

        if (pointer.Kind != PointerKind.Offset
            || pointer.StreamBlockIndex < 0
            || pointer.StreamBlockIndex >= _oldBlockSizes.Length
            || pointer.StreamBlockIndex >= newBlockSizes.Length
            || _oldBlockSizes[pointer.StreamBlockIndex] <= 0
            || newBlockSizes[pointer.StreamBlockIndex] <= 0
            || pointer.Offset < 0
            || pointer.Offset >= _oldBlockSizes[pointer.StreamBlockIndex])
        {
            return false;
        }

        var oldPhysical = pointer.Offset + _oldBlockBases[pointer.StreamBlockIndex];
        var translated = TryTranslatePhysicalOffset(oldPhysical, out var newPhysical);
        if ((!translated || newPhysical == oldPhysical)
            && TryTranslateByBlockDelta(pointer.StreamBlockIndex, oldPhysical, newBlockSizes, out var blockDeltaPhysical))
        {
            newPhysical = blockDeltaPhysical;
            translated = true;
        }

        if (!translated)
            return false;

        var newBlockBases = GetBlockBases(newXFileSize, newBlockSizes);
        var newOffset = newPhysical - newBlockBases[pointer.StreamBlockIndex];
        if (newOffset < 0 || newOffset >= newBlockSizes[pointer.StreamBlockIndex] || newOffset > StreamOffsetMask)
            return false;

        raw = (pointer.StreamBlockIndex << 28) | newOffset;
        return raw != pointer.Raw;
    }

    private bool TryTranslatePhysicalOffset(int oldPhysical, out int newPhysical)
    {
        newPhysical = oldPhysical;
        RebaseRange? previous = null;
        RebaseRange? next = null;

        foreach (var range in _ranges)
        {
            if (oldPhysical >= range.OldStart && oldPhysical < range.OldEnd)
            {
                var offsetInRange = oldPhysical - range.OldStart;
                if (offsetInRange >= range.NewLength)
                    return false;

                newPhysical = range.NewStart + offsetInRange;
                return true;
            }

            if (range.OldEnd <= oldPhysical
                && (previous is null || range.OldEnd > previous.Value.OldEnd))
            {
                previous = range;
                continue;
            }

            if (range.OldStart > oldPhysical
                && (next is null || range.OldStart < next.Value.OldStart))
            {
                next = range;
            }
        }

        if (previous is null && next is null)
            return false;

        var delta = previous is not null
            ? previous.Value.NewEnd - previous.Value.OldEnd
            : next!.Value.NewStart - next.Value.OldStart;

        newPhysical = oldPhysical + delta;
        return true;
    }

    private bool TryTranslateByBlockDelta(
        int blockIndex,
        int oldPhysical,
        int[] newBlockSizes,
        out int newPhysical)
    {
        newPhysical = oldPhysical;

        var blockDelta = newBlockSizes[blockIndex] - _oldBlockSizes[blockIndex];
        if (blockDelta == 0)
            return false;

        var oldBlockStart = _oldBlockBases[blockIndex];
        var oldBlockEnd = oldBlockStart + _oldBlockSizes[blockIndex];
        int? firstChangedEnd = null;

        foreach (var range in _ranges)
        {
            if (range.OldStart < oldBlockStart
                || range.OldStart >= oldBlockEnd
                || range.OldLength == range.NewLength)
            {
                continue;
            }

            if (firstChangedEnd is null || range.OldEnd < firstChangedEnd)
                firstChangedEnd = range.OldEnd;
        }

        if (firstChangedEnd is null || oldPhysical < firstChangedEnd)
            return false;

        newPhysical = oldPhysical + blockDelta;
        return true;
    }

    private static int[] GetBlockBases(int xfileSize, int[] blockSizes)
    {
        var blockBases = new int[blockSizes.Length];
        var position = Math.Max(0, xfileSize - blockSizes.Sum());
        var largeIndex = (int)XFILE_BLOCK.LARGE;

        for (var i = 0; i < blockSizes.Length; i++)
        {
            if (i == largeIndex)
                continue;

            blockBases[i] = position;
            position += blockSizes[i];
        }

        if (largeIndex >= 0 && largeIndex < blockBases.Length)
            blockBases[largeIndex] = position;

        return blockBases;
    }

    private readonly record struct RebaseRange(
        int OldStart,
        int OldLength,
        int NewStart,
        int NewLength)
    {
        public int OldEnd => OldStart + OldLength;
        public int NewEnd => NewStart + NewLength;
    }
}
