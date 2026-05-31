using FastFile.Models.Zone;

namespace FastFile.Models.Assets;

public sealed class UnstructuredAsset(XAssetType type) : BaseAsset(type)
{
    public override string? GetDisplayName => Type.ToString();
}
