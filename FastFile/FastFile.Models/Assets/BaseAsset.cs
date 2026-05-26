using FastFile.Models.Zone;

namespace FastFile.Models.Assets;

public abstract class BaseAsset
{
    public XAssetType Type { get; }
    public int Offset { get; init; }

    protected BaseAsset(XAssetType type)
    {
        Type = type;
    }
}
