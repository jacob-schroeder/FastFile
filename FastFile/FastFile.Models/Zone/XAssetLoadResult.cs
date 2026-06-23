using FastFile.Models.Assets;
using FastFile.Models.Pointers;

namespace FastFile.Models.Zone;

public sealed record XAssetLoadResult(
    int Index,
    XAssetType Type,
    int SourceOffset,
    int EndSourceOffset,
    XPointer<BaseAsset> AssetPointer,
    BaseAsset? Asset,
    string? StopReason)
{
    public bool IsLoaded => Asset is not null;
}
