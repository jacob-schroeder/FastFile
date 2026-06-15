using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Physics;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x2C)]
public class PhysPreset() : BaseAsset(XAssetType.PhysPreset)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int PresetType { get; set; }

    [XField(Offset = 0x08)]
    public float Mass { get; set; }

    [XField(Offset = 0x0C)]
    public float Bounce { get; set; }

    [XField(Offset = 0x10)]
    public float Friction { get; set; }

    [XField(Offset = 0x14)]
    public float BulletForceScale { get; set; }

    [XField(Offset = 0x18)]
    public float ExplosiveForceScale { get; set; }

    [XField(Offset = 0x1C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> SndAliasPrefix { get; set; } // Direct

    [XField(Offset = 0x20)]
    public float PiecesSpreadFraction { get; set; }

    [XField(Offset = 0x24)]
    public float PiecesUpwardVelocity { get; set; }

    [XField(Offset = 0x28)]
    public bool TempDefaultToCylinder { get; set; }

    [XField(Offset = 0x29)]
    public bool PerSurfaceSndAlias { get; set; }

    [XField(Offset = 0x2A)]
    public ushort BoolAlignmentPadding { get; set; }

    public override string? GetDisplayName => Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x48)]
public class PhysCollmap() : BaseAsset(XAssetType.PhysCollmap)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int Count { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(Count))]
    public XPointer<PhysGeomInfo[]> Geoms { get; set; } // Direct

    [XField(Offset = 0x0C)]
    public PhysMass Mass { get; set; }

    [XField(Offset = 0x30)]
    public Bounds Bounds { get; set; }

    public override string? GetDisplayName => Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x44)]
public sealed class PhysGeomInfo
{
    public const int Size = 0x44;

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<BrushWrapper> BrushWrapper { get; set; } = null!; // Direct

    [XField(Offset = 0x04)]
    public int Type { get; set; }

    [XField(Offset = 0x08, Count = 3)]
    public Vec3[] Orientation { get; set; } = new Vec3[3];

    [XField(Offset = 0x2C)]
    public Bounds Bounds { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x44)]
public sealed class BrushWrapper
{
    public const int Size = 0x44;

    [XField(Offset = 0x00)]
    public Bounds Bounds { get; set; }

    [XField(Offset = 0x18)]
    public CBrush Brush { get; set; } = new();

    [XField(Offset = 0x3C)]
    public int TotalEdgeCount { get; set; }

    [XField(Offset = 0x40)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(PlaneCount))]
    public XPointer<CPlane[]> Planes { get; set; } = null!; // Direct

    public int PlaneCount => Brush.NumSides;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x24)]
public sealed class CBrush
{
    public const int Size = 0x24;

    [XField(Offset = 0x00)]
    public ushort NumSides { get; set; }

    [XField(Offset = 0x02)]
    public ushort GlassPieceIndex { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(NumSides))]
    public XPointer<CBrushSide[]> Sides { get; set; } = null!; // Direct

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(TotalEdgeCount))]
    public XPointer<byte[]> BaseAdjacentSide { get; set; } = null!; // Direct

    [XField(Offset = 0x0C, Count = 6)]
    public short[] AxialMaterialNum { get; set; } = new short[6];

    [XField(Offset = 0x18, Count = 6)]
    public byte[] FirstAdjacentSideOffsets { get; set; } = new byte[6];

    [XField(Offset = 0x1E, Count = 6)]
    public byte[] EdgeCount { get; set; } = new byte[6];

    public int TotalEdgeCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class CBrushSide
{
    public const int Size = 0x08;

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<CPlane> Plane { get; set; } = null!; // Direct

    [XField(Offset = 0x04)]
    public ushort MaterialNum { get; set; }

    [XField(Offset = 0x06)]
    public byte FirstAdjacentSideOffset { get; set; }

    [XField(Offset = 0x07)]
    public byte EdgeCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x14)]
public sealed class CPlane
{
    public const int Size = 0x14;

    [XField(Offset = 0x00)]
    public Vec3 Normal { get; set; }

    [XField(Offset = 0x0C)]
    public float Dist { get; set; }

    [XField(Offset = 0x10)]
    public byte Type { get; set; }

    [XField(Offset = 0x11)]
    public byte SignBits { get; set; }

    [XField(Offset = 0x12, Count = 2)]
    public byte[] Pad12 { get; set; } = new byte[2];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x24)]
public sealed class PhysMass
{
    [XField(Offset = 0x00)]
    public Vec3 CenterOfMass { get; set; }

    [XField(Offset = 0x0C)]
    public Vec3 MomentsOfInertia { get; set; }

    [XField(Offset = 0x18)]
    public Vec3 ProductsOfInertia { get; set; }
}
