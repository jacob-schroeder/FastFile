using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Physics;

public class PhysPreset() : BaseAsset(XAssetType.PhysPreset)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public int PresetType { get; set; }
    public float Mass { get; set; }
    public float Bounce { get; set; }
    public float Friction { get; set; }
    public float BulletForceScale { get; set; }
    public float ExplosiveForceScale { get; set; }
    public XPointer<string> SndAliasPrefix { get; set; } // Direct
    public float PiecesSpreadFraction { get; set; }
    public float PiecesUpwardVelocity { get; set; }
    public bool TempDefaultToCylinder { get; set; }
    public bool PerSurfaceSndAlias { get; set; }
    public ushort BoolAlignmentPadding { get; set; }

    public override string? GetDisplayName => Name;
}

public class PhysCollmap() : BaseAsset(XAssetType.PhysCollmap)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public uint Count { get; set; }
    public XPointer<PhysGeomInfo[]> Geoms { get; set; } // Direct
    public PhysMass Mass { get; set; }
    public Bounds Bounds { get; set; }

    public override string? GetDisplayName => Name;
}

public sealed class PhysGeomInfo
{
    public XPointer<BrushWrapper> BrushWrapper { get; set; } // Direct
    public int Type { get; set; }
    public Vec3[] Orientation { get; set; } = new Vec3[3];
    public Bounds Bounds { get; set; }
}

public sealed class BrushWrapper
{
    public Bounds Bounds { get; set; }
    public CBrush Brush { get; set; } = new();
    public int TotalEdgeCount { get; set; }
    public XPointer<CPlane[]> Planes { get; set; } // Direct
}

public sealed class CBrush
{
    public ushort NumSides { get; set; }
    public ushort GlassPieceIndex { get; set; }
    public XPointer<CBrushSide[]> Sides { get; set; } // Direct
    public XPointer<byte[]> BaseAdjacentSide { get; set; } // Direct
    public short[] AxialMaterialNum { get; set; } = new short[6];
    public byte[] FirstAdjacentSideOffsets { get; set; } = new byte[6];
    public byte[] EdgeCount { get; set; } = new byte[6];
}

public sealed class CBrushSide
{
    public XPointer<CPlane> Plane { get; set; } // Direct
    public ushort MaterialNum { get; set; }
    public byte FirstAdjacentSideOffset { get; set; }
    public byte EdgeCount { get; set; }
}

public sealed class CPlane
{
    public Vec3 Normal { get; set; }
    public float Dist { get; set; }
    public byte Type { get; set; }
    public byte[] Padding { get; set; } = new byte[3];
}

public sealed class PhysMass
{
    public Vec3 CenterOfMass { get; set; }
    public Vec3 MomentsOfInertia { get; set; }
    public Vec3 ProductsOfInertia { get; set; }
}
