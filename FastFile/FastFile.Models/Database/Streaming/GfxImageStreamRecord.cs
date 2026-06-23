namespace FastFile.Models.Database.Streaming;

public readonly record struct GfxImageStreamRecord(
    uint SourceStart,
    uint BlockOffset,
    uint StreamOffset,
    uint SourceEnd,
    StreamFileRef? File)
{
    public uint SourceSize => SourceEnd - SourceStart;
    public bool HasFile => File is not null;
}
