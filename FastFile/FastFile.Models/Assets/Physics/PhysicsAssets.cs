using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Physics;

public class PhysPreset() : BaseAsset(XAssetType.PhysPreset)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public int PresetType { get; set; }
    public float Mass { get; set; }
    public float Bounce { get; set; }
    public float Friction { get; set; }

    public override string? GetDisplayName => Name;
}

public class PhysCollmap() : BaseAsset(XAssetType.PhysCollmap)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public uint Count { get; set; }

    public override string? GetDisplayName => Name;
}
