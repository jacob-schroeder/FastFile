using FastFile.Models.Zone;

namespace FastFile.Runtime.Coverage;

public sealed record SourceCoverageRecord(
    int SourceStart,
    int SourceEndExclusive,
    string Kind,
    string OwnerPath,
    string MemberName,
    string CallerName,
    XFileBlockType? DestinationBlock,
    int? DestinationOffset)
{
    public int Length => SourceEndExclusive - SourceStart;
}
