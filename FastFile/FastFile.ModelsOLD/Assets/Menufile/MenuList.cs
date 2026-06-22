using FastFile.ModelsOLD.Assets.Menu;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.Menufile;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
[XEbootEvidence(
    "0x10f0b8",
    "eboot/traces/xasset_loader_findings.txt",
    Detail = "MenuFile inner loader: Load_Stream size 0x0c; Load_XString at root+0x00; menu pointer array at +0x08; count at +0x04; entries are 4-byte MenuDef alias pointers.")]
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
