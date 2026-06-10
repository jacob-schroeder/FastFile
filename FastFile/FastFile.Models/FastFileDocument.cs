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
    public XAssetList AssetList { get; init; } = null!;
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
            AssetList = CreateEmptyAssetList(),
            ZoneBuffer = []
        };
    }

    public static FastFileDocument FromParsed(
        byte[] buffer,
        DB_Header header,
        XFile zoneHeader,
        XAssetList assetList,
        byte[] zoneBuffer)
    {
        return new FastFileDocument
        {
            Buffer = buffer,
            IsNew = false,
            Header = header,
            ZoneHeader = zoneHeader,
            AssetList = assetList,
            ZoneBuffer = zoneBuffer
        };
    }

    private static XAssetList CreateEmptyAssetList()
    {
        var scriptStringsPtr = new XPointer<XPointer<string?>[]>()
        {
            Raw = 0,
            Kind = PointerKind.Null,
            ResolutionKind = PointerResolutionKind.Unknown
        };
        scriptStringsPtr.Value = [];

        var assetsPtr = new XPointer<XAsset[]>
        {
            Value =
            [
            ],
            Raw = 0,
            Kind = PointerKind.Null,
            ResolutionKind = PointerResolutionKind.Unknown
        };

        return new XAssetList
        {
            ScriptStringCount = 0,
            ScriptStringsPtr = scriptStringsPtr,
            AssetCount = 0,
            AssetsPtr = assetsPtr
        };
    }
}
