using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed record XFileWriteResult(
    XFile Header,
    IReadOnlyList<byte[]> Blocks,
    byte[] LinearZoneBuffer);
