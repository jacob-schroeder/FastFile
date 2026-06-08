using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Tracers;

public class TracerDef() : BaseAsset(XAssetType.Tracer)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<Material.Material> Material { get; set; }
    public uint DrawInterval { get; set; }
    public float Speed { get; set; }
    public float BeamLength { get; set; }
    public float BeamWidth { get; set; }
    public float ScrewRadius { get; set; }
    public float ScrewDist { get; set; }
    public Vec4[] Colors { get; set; } = new Vec4[5];

    public override string? GetDisplayName => Name;
}
