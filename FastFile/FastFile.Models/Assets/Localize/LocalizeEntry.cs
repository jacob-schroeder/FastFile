using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Localize;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x8)]
[XEbootEvidence(
    "0x104278",
    "eboot/traces/xasset_loader_findings.txt",
    Detail = "Localize inner loader: Load_Stream size 0x08; Load_XString at root+0x00 and root+0x04.")]
public class LocalizeEntry() : BaseAsset(XAssetType.Localize)
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string?> ValuePtr { get; set; }
    
    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string?> NamePtr { get; set; }
    
    // Exposed
    public string Value => ValuePtr is { IsResolved: true } ? ValuePtr.Value ?? string.Empty : string.Empty;
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    
    public override string? GetDisplayName => Name;
}
