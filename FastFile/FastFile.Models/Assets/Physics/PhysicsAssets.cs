using FastFile.Models.Data;
using FastFile.Models.Utils;
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
    public float BulletForceScale { get; set; }
    public float ExplosiveForceScale { get; set; }
    public ZonePointer<string> SndAliasPrefix { get; set; }
    public float PiecesSpreadFraction { get; set; }
    public float PiecesUpwardVelocity { get; set; }
    public bool TempDefaultToCylinder { get; set; }
    public bool PerSurfaceSndAlias { get; set; }
    public ushort BoolAlignmentPadding { get; set; }

    public override string? GetDisplayName => Name;
}

public class PhysCollmap() : BaseAsset(XAssetType.PhysCollmap)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public uint Count { get; set; }
    public ZonePointer<PhysGeomInfo[]> Geoms { get; set; }
    public PhysMass Mass { get; set; }
    public Bounds Bounds { get; set; }

    public override string? GetDisplayName => Name;
}

public sealed class PhysGeomInfo
{
    public ZonePointer<BrushWrapper> BrushWrapper { get; set; }
    public int Type { get; set; }
    public Vec3[] Orientation { get; set; } = new Vec3[3];
    public Bounds Bounds { get; set; }
}

public sealed class BrushWrapper
{
    public Bounds Bounds { get; set; }
}

public sealed class PhysMass
{
    public Vec3 CenterOfMass { get; set; }
    public Vec3 MomentsOfInertia { get; set; }
    public Vec3 ProductsOfInertia { get; set; }
}
