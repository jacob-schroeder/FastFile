using FastFile.Models.Zone;

namespace FastFile.Models.Assets;

public abstract class BaseAsset : IBaseAsset
{
    public XAssetType Type { get; }
    public int Offset { get; init; }

    public abstract string? GetDisplayName { get; }

    protected BaseAsset(XAssetType type)
    {
        Type = type;
    }
}

public interface IBaseAsset
{
    string? GetDisplayName { get; }
}
