using FastFile.Models.Archive;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models;

public sealed class FastFileDocument
{
    private const string DefaultMagic = "IWffu100";

    public byte[]? Buffer { get; init; }
    public bool IsNew { get; init; }
    public DB_Header Header { get; init; } = null!;
    public XFile ZoneHeader { get; init; } = null!;
    public XAssetListOLD AssetListOld { get; init; } = null!;
    public byte[]? ZoneBuffer { get; init; }

    public static FastFileDocument CreateNew()
    {
        return new FastFileDocument
        {
            IsNew = true,
            Header = new DB_Header
            {
                Magic = DefaultMagic,
                Version = XFILE_VERSION.Mw2,
                AllowOnlineUpdate = false,
                FileCreationTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
                Region = Language.LANGUAGE_ENGLISH,
                EntryCount = 0,
                FileSize = 0,
                MaxFileSize = 0
            },
            ZoneHeader = new XFile
            {
                Size = 0,
                ExternalSize = 0,
                BlockSize = new int[(int)XFILE_BLOCK.MAX_XFILE_COUNT]
            },
            AssetListOld = CreateEmptyAssetList(),
            ZoneBuffer = []
        };
    }

    public static FastFileDocument FromParsed(
        byte[] buffer,
        DB_Header header,
        XFile zoneHeader,
        XAssetListOLD assetListOld,
        byte[] zoneBuffer)
    {
        return new FastFileDocument
        {
            Buffer = buffer,
            IsNew = false,
            Header = header,
            ZoneHeader = zoneHeader,
            AssetListOld = assetListOld,
            ZoneBuffer = zoneBuffer
        };
    }

    private static XAssetListOLD CreateEmptyAssetList()
    {
        var scriptStringsPtr = new DirectPointer<ZonePointer<string?>[]>(0);
        scriptStringsPtr.SetResult([]);

        var assetsPtr = new DirectPointer<XAsset[]>(0);
        assetsPtr.SetResult([]);

        return new XAssetListOLD
        {
            ScriptStringCount = 0,
            ScriptStringsPtr = scriptStringsPtr,
            AssetCount = 0,
            AssetsPtr = assetsPtr
        };
    }
}
