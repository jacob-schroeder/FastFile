using FastFile.Models.Assets;
using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public class XAsset
{
    public XAssetType Type { get; set; }
    public ZonePointer<BaseAsset> XAssetPtr { get; set; }
};
