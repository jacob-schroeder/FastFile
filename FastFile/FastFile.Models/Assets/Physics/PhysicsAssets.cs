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
    public uint Count { get; set; }

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
