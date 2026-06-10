using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Tracers;

public class TracerDef() : BaseAsset(XAssetType.Tracer)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public XPointer<Material.Material> Material { get; set; } // Alias
    public uint DrawInterval { get; set; }
    public float Speed { get; set; }
    public float BeamLength { get; set; }
    public float BeamWidth { get; set; }
    public float ScrewRadius { get; set; }
    public float ScrewDist { get; set; }
    public Vec4[] Colors { get; set; } = new Vec4[5];

    public override string? GetDisplayName => Name;
}
