using FastFile.Models.Zone;

namespace FastFile.Runtime.Assets;

public sealed class AssetRegistry
{
    private readonly List<XAsset> _assets = new();

    public IReadOnlyList<XAsset> Assets => _assets;

    public void Add(XAsset asset)
    {
        _assets.Add(asset);
    }
}
