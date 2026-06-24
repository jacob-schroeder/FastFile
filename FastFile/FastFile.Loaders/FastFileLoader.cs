using FastFile.Loaders.Assets;
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
    private readonly XAssetListReader _xassetListReader = new();
    private readonly XAssetDispatcher _xassetDispatcher = new();

    public FastFileLoad Load(byte[] buffer, int length, FastFileLoadContext? context = null)
    {
        FastFileLoadContext activeContext = context ?? new FastFileLoadContext();
        var cursor = new FastFileCursor(buffer.AsMemory(0, length));

        DbHeader header = _dbHeaderReader.Read(cursor, activeContext);
        byte[] zone = _xBlockReader.ReadZone(cursor, header.FileSize, activeContext.Diagnostics);
        var zoneCursor = new FastFileCursor(zone);
        XFile xfile = _xfileHeaderReader.Read(zoneCursor);
        activeContext.Blocks.Initialize(xfile);
        activeContext.Diagnostics.Trace(
            $"xfile header source=0x0 size=0x{xfile.Size:X} external=0x{xfile.ExternalSize:X} blocks={activeContext.Blocks.DescribePositions()}");
        XAssetListSnapshot xassetList = _xassetListReader.Read(zoneCursor, activeContext);
        activeContext.Diagnostics.Trace(
            $"xassetlist source=0x{xassetList.SerializedOffset:X} scripts={xassetList.ScriptStringCount} ptr={xassetList.ScriptStringsPointer} assets={xassetList.AssetCount} ptr={xassetList.AssetsPointer} nextSource=0x{zoneCursor.Offset:X} blocks={activeContext.Blocks.DescribePositions()}");
        IReadOnlyList<XAssetLoadResult> loadedAssets = _xassetDispatcher.LoadSupportedPrefix(zoneCursor, xassetList, activeContext);

        return new FastFileLoad(
            Header: header,
            XFileHeader: xfile,
            XAssetList: xassetList,
            LoadedAssets: loadedAssets,
            ZoneBytes: zone,
            ImageStreams: activeContext.ImageStreams,
            Warnings: activeContext.Diagnostics.Warnings);
    }
}
