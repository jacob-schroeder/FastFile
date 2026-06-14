using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.XModels;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x24)]
public class XModelSurfs() : BaseAsset(XAssetType.XModelSurfs)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(NumSurfs))]
    public XPointer<XSurface[]> Surfs { get; set; } // Direct

    [XField(Offset = 0x08)]
    public ushort NumSurfs { get; set; }

    [XField(Offset = 0x0A)]
    public ushort PartBitsAlignment { get; set; }

    [XField(Offset = 0x0C)]
    public int[] PartBits { get; set; } = new int[6];

    public override string? GetDisplayName => Name;
}
