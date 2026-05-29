using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Menu;

public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    public override string? GetDisplayName => "menufile";
}