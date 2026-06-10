using FastFile.Models.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;

namespace FastFile.Models.Assets.Menufile;

public class MenuList() : BaseAsset(XAssetType.MenuFile)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public int MenuCount { get; set; }
    public XPointer<XPointer<MenuDef>[]> Menus { get; set; } // Direct -> Alias ?

    public override string? GetDisplayName => NamePtr is { IsResolved: true } ? NamePtr.Value : string.Empty;
}
