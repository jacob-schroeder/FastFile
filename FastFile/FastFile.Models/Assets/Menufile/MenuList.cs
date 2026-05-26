using FastFile.Models.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;

namespace FastFile.Models.Assets.Menufile;

public class MenuList() : BaseAsset(XAssetType.MenuFile)
{
    public ZonePointer<string> NamePtr { get; set; }
    public int MenuCount { get; set; }
    public ZonePointer<ZonePointer<MenuDef[]>> Menus { get; set; }
}