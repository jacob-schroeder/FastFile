using FastFile.Models.Assets;
using FastFile.Models.Pointers;

namespace FastFile.Models.Zone;

public struct XAsset
{
    public XAssetType Type;
    public XPointer<BaseAsset> Asset;
}