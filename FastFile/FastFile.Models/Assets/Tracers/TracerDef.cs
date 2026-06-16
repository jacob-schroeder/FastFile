using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Tracers;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x70)]
[XEbootEvidence(
    "0x10e148",
    "eboot/graphs/tracer_loader_map.md",
    Detail = "Load_TracerDef body: Load_Stream size 0x70; PushStreamPos(4); Load_XString at +0x00; Load_MaterialPtr at +0x04, whose pointer wrapper pushes block 0; PopStreamPos. XAsset type 0x27 dispatch branches to 0x10e1b8.")]
public class TracerDef() : BaseAsset(XAssetType.Tracer)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<Material.Material> Material { get; set; } // Alias

    [XField(Offset = 0x08)]
    public uint DrawInterval { get; set; }

    [XField(Offset = 0x0C)]
    public float Speed { get; set; }

    [XField(Offset = 0x10)]
    public float BeamLength { get; set; }

    [XField(Offset = 0x14)]
    public float BeamWidth { get; set; }

    [XField(Offset = 0x18)]
    public float ScrewRadius { get; set; }

    [XField(Offset = 0x1C)]
    public float ScrewDist { get; set; }

    [XField(Offset = 0x20)]
    public Vec4[] Colors { get; set; } = new Vec4[5];

    public override string? GetDisplayName => Name;
}
