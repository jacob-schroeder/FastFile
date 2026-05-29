using FastFile.Models.Zone;

namespace FastFile.Models.Assets;

public sealed class UnknownAsset : BaseAsset
{
    public UnknownAsset(XAssetType type) : base(type)
    {
        
    }
    
    public override string? GetDisplayName => "unknown";
}
