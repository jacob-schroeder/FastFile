using FastFile.Loaders.DbFileLoad;
using FastFile.Loaders.XFileLoad;
using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders;

public sealed class FastFileLoader
{
    private readonly DbHeaderReader _dbHeaderReader = new();
    private readonly XBlockReader _xBlockReader = new();
    private readonly XFileHeaderReader _xfileHeaderReader = new();

    public FastFileLoad Load(byte[] buffer, int length, FastFileLoadContext? context = null)
    {
        FastFileLoadContext activeContext = context ?? new FastFileLoadContext();
        var cursor = new FastFileCursor(buffer.AsMemory(0, length));

        DbHeader header = _dbHeaderReader.Read(cursor, activeContext);
        byte[] zone = _xBlockReader.ReadZone(cursor, header.FileSize, activeContext.Diagnostics);
        XFile xfile = _xfileHeaderReader.Read(new FastFileCursor(zone));
        activeContext.Blocks.Initialize(xfile);

        return new FastFileLoad(
            Header: header,
            XFileHeader: xfile,
            ZoneBytes: zone,
            ImageStreams: activeContext.ImageStreams,
            Warnings: activeContext.Diagnostics.Warnings);
    }
}
