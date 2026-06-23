using FastFile.Models.Database.Streaming;
using FastFile.Models.Zone;

namespace FastFile.Models.Database.DbFileLoad;

public readonly record struct FastFileLoad(
    DbHeader Header,
    XFile XFileHeader,
    XAssetListSnapshot XAssetList,
    IReadOnlyList<XAssetLoadResult> LoadedAssets,
    byte[] ZoneBytes,
    GfxImageStreamTable ImageStreams,
    IReadOnlyList<string> Warnings);
