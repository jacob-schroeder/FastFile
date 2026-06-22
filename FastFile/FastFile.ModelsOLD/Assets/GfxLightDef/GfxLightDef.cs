using FastFile.ModelsOLD.Assets.Material;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.GfxLightDef;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class GfxLightDef() : BaseAsset(XAssetType.LightDef)
{
    public const int RootSize = 0x10;

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?> NamePtr { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<GfxImage> Image { get; set; }

    [XField(Offset = 0x08)]
    public int Unknown8 { get; set; }

    [XField(Offset = 0x0C)]
    public int UnknownC { get; set; }

    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public override string? GetDisplayName => string.IsNullOrEmpty(Name) ? $"LightDef 0x{Offset:X8}" : Name;
}
