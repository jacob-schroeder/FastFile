using FastFile.Models.Math;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Physics;

public sealed class PhysCollmapAsset : BaseAsset
{
    public const int SerializedSize = 0x48;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int Count { get; init; }
    public XPointer<PhysGeomInfo[]> GeomsPointer { get; init; }
    public IReadOnlyList<PhysGeomInfo> Geoms { get; init; } = [];
    public PhysMass Mass { get; init; } = new();
    public Bounds Bounds { get; init; } = new();
}

public sealed class PhysMass
{
    public Vec3 CenterOfMass { get; init; }
    public Vec3 MomentsOfInertia { get; init; }
    public Vec3 ProductsOfInertia { get; init; }
}

public sealed class PhysGeomInfo
{
    public const int SerializedSize = 0x44;

    public XPointer<BrushWrapper> BrushWrapperPointer { get; init; }
    public BrushWrapper? BrushWrapper { get; init; }
    public int Type { get; init; }
    public IReadOnlyList<Vec3> Orientation { get; init; } = [];
    public Bounds Bounds { get; init; } = new();
}

public sealed class BrushWrapper
{
    public const int SerializedSize = 0x44;

    public Bounds Bounds { get; init; } = new();
    public CBrush Brush { get; init; } = new();
    public int TotalEdgeCount { get; init; }
    public XPointer<CPlane[]> PlanesPointer { get; init; }
    public IReadOnlyList<CPlane> Planes { get; init; } = [];
}

public sealed class CBrush
{
    public const int SerializedSize = 0x24;

    public ushort NumSides { get; init; }
    public ushort GlassPieceIndex { get; init; }
    public XPointer<CBrushSide[]> SidesPointer { get; init; }
    public IReadOnlyList<CBrushSide> Sides { get; init; } = [];
    public XPointer<byte[]> BaseAdjacentSidePointer { get; init; }
    public IReadOnlyList<byte> BaseAdjacentSide { get; init; } = [];
    public IReadOnlyList<short> AxialMaterialNum { get; init; } = [];
    public IReadOnlyList<byte> FirstAdjacentSideOffsets { get; init; } = [];
    public IReadOnlyList<byte> EdgeCount { get; init; } = [];
}

public sealed class CBrushSide
{
    public const int SerializedSize = 0x08;

    public XPointer<CPlane> PlanePointer { get; init; }
    public CPlane? Plane { get; init; }
    public ushort MaterialNum { get; init; }
    public byte FirstAdjacentSideOffset { get; init; }
    public byte EdgeCount { get; init; }
}

public sealed class CPlane
{
    public const int SerializedSize = 0x14;

    public Vec3 Normal { get; init; }
    public float Dist { get; init; }
    public byte Type { get; init; }
    public byte SignBits { get; init; }
    public IReadOnlyList<byte> Pad12 { get; init; } = [];
}
