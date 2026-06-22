using FastFile.Logic.Database.Streaming;
using FastFile.Models.Zone;

namespace FastFile.Logic.Database.DbFileLoad;

public readonly record struct FastFileLoad(
    DbHeader Header,
    XFile XFileHeader,
    byte[] ZoneBytes,
    GfxImageStreamTable ImageStreams,
    IReadOnlyList<string> Warnings);
