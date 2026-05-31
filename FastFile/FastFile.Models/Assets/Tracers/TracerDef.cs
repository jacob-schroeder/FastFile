using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Tracers;

public class TracerDef() : BaseAsset(XAssetType.Tracer)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public ZonePointer<Material.Material> Material { get; set; }
    public uint DrawInterval { get; set; }

    public override string? GetDisplayName => Name;
}
