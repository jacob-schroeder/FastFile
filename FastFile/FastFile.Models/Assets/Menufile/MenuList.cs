using FastFile.Models.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Menufile;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public class MenuList() : BaseAsset(XAssetType.MenuFile)
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct

    [XField(Offset = 0x04)]
    public int MenuCount { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Alias,
        ElementTarget = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.LARGE,
        CountMember = nameof(MenuCount))]
    public XPointer<XPointer<MenuDef>[]> Menus { get; set; } // Direct -> Alias ?

    public override string? GetDisplayName => NamePtr is { IsResolved: true } ? NamePtr.Value : string.Empty;
}
