using FastFile.Models.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;

namespace FastFile.Models.Assets.Menufile;

public class MenuList() : BaseAsset(XAssetType.MenuFile)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
    public int MenuCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(MenuCount))]
    public DirectPointer<AliasPointer<MenuDef>[]> Menus { get; set; }

    public override string? GetDisplayName => NamePtr is { IsResolved: true } ? NamePtr.Result : string.Empty;
}
