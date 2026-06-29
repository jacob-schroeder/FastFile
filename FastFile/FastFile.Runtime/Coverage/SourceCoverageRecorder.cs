using FastFile.Models.Zone;

namespace FastFile.Runtime.Coverage;

public sealed class SourceCoverageRecorder
{
    private readonly List<SourceCoverageRecord> _records = new();
    private readonly Stack<string> _ownerStack = new();

    public int SourceLength { get; private set; }
    public IReadOnlyList<SourceCoverageRecord> Records => _records;

    public void BeginZone(int sourceLength)
    {
        if (sourceLength < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceLength));

        SourceLength = sourceLength;
        _records.Clear();
        _ownerStack.Clear();
    }

    public IDisposable PushOwner(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Coverage owner must be non-empty.", nameof(owner));

        _ownerStack.Push(owner);
        return new OwnerScope(this);
    }

    public void RecordHeader(
        int sourceStart,
        int byteCount,
        string memberName,
        string callerName)
    {
        Record(sourceStart, byteCount, "Header", memberName, callerName, null, null);
    }

    public void RecordLoadStream(
        int sourceStart,
        int byteCount,
        XFileBlockType destinationBlock,
        int destinationOffset,
        string memberName,
        string callerName)
    {
        Record(sourceStart, byteCount, "LoadStream", memberName, callerName, destinationBlock, destinationOffset);
    }

    public void RecordCString(
        int sourceStart,
        int byteCount,
        XFileBlockType destinationBlock,
        int destinationOffset,
        string memberName,
        string callerName)
    {
        Record(sourceStart, byteCount, "XString", memberName, callerName, destinationBlock, destinationOffset);
    }

    public void RecordSourcePadding(
        int sourceStart,
        int byteCount,
        string memberName,
        string callerName)
    {
        Record(sourceStart, byteCount, "SourcePadding", memberName, callerName, null, null);
    }

    public SourceCoverageSummary BuildSummary(int maxUncoveredRanges = 1024)
    {
        if (maxUncoveredRanges < 0)
            throw new ArgumentOutOfRangeException(nameof(maxUncoveredRanges));

        var ranges = _records
            .Where(x => x.Length > 0)
            .OrderBy(x => x.SourceStart)
            .ThenByDescending(x => x.SourceEndExclusive)
            .ToArray();

        var uncovered = new List<SourceCoverageGap>();
        long coveredBytes = 0;
        int coveredEnd = 0;
        int overlapCount = 0;
        long overlappedBytes = 0;

        foreach (SourceCoverageRecord range in ranges)
        {
            if (range.SourceStart < 0 || range.SourceEndExclusive > SourceLength || range.SourceEndExclusive < range.SourceStart)
                throw new InvalidDataException(
                    $"Coverage range 0x{range.SourceStart:X}..0x{range.SourceEndExclusive:X} is outside source length 0x{SourceLength:X}.");

            if (range.SourceStart > coveredEnd)
                AddUncovered(uncovered, coveredEnd, range.SourceStart, maxUncoveredRanges);

            if (range.SourceStart < coveredEnd)
            {
                overlapCount++;
                overlappedBytes += Math.Min(range.SourceEndExclusive, coveredEnd) - range.SourceStart;
            }

            if (range.SourceEndExclusive > coveredEnd)
            {
                coveredBytes += range.SourceEndExclusive - Math.Max(range.SourceStart, coveredEnd);
                coveredEnd = range.SourceEndExclusive;
            }
        }

        if (coveredEnd < SourceLength)
            AddUncovered(uncovered, coveredEnd, SourceLength, maxUncoveredRanges);

        return new SourceCoverageSummary(
            SourceLength,
            coveredBytes,
            SourceLength - coveredBytes,
            _records.Count,
            overlapCount,
            overlappedBytes,
            uncovered);
    }

    private void Record(
        int sourceStart,
        int byteCount,
        string kind,
        string memberName,
        string callerName,
        XFileBlockType? destinationBlock,
        int? destinationOffset)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        if (byteCount == 0)
            return;

        _records.Add(new SourceCoverageRecord(
            sourceStart,
            checked(sourceStart + byteCount),
            kind,
            CurrentOwnerPath(),
            string.IsNullOrWhiteSpace(memberName) ? "<unlabeled>" : memberName,
            string.IsNullOrWhiteSpace(callerName) ? "<unknown>" : callerName,
            destinationBlock,
            destinationOffset));
    }

    private string CurrentOwnerPath()
    {
        return _ownerStack.Count == 0
            ? "<root>"
            : string.Join(" > ", _ownerStack.Reverse());
    }

    private static void AddUncovered(
        List<SourceCoverageGap> uncovered,
        int start,
        int endExclusive,
        int maxUncoveredRanges)
    {
        if (uncovered.Count < maxUncoveredRanges)
            uncovered.Add(new SourceCoverageGap(start, endExclusive));
    }

    private sealed class OwnerScope : IDisposable
    {
        private SourceCoverageRecorder? _recorder;

        public OwnerScope(SourceCoverageRecorder recorder)
        {
            _recorder = recorder;
        }

        public void Dispose()
        {
            SourceCoverageRecorder? recorder = Interlocked.Exchange(ref _recorder, null);
            if (recorder is not null)
                recorder._ownerStack.Pop();
        }
    }
}

public sealed record SourceCoverageGap(int SourceStart, int SourceEndExclusive)
{
    public int Length => SourceEndExclusive - SourceStart;
}

public sealed record SourceCoverageSummary(
    int SourceLength,
    long CoveredBytes,
    long UncoveredBytes,
    int RecordCount,
    int OverlapCount,
    long OverlappedBytes,
    IReadOnlyList<SourceCoverageGap> UncoveredRanges)
{
    public bool IsComplete => UncoveredBytes == 0;
}
