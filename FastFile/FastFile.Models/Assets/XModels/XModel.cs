using FastFile.Models.Data;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.XModels;

public class XModel() : BaseAsset(XAssetType.XModel)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public byte NumBones { get; set; }
    public byte NumRootBones { get; set; }
    public byte NumSurfs { get; set; }
    public byte LodRampType { get; set; }
    public float Scale { get; set; }
    public int[] NoScalePartBits { get; set; } = new int[6];
    public XPointer<ushort[]> BoneNames { get; set; } // Direct
    public XPointer<XModelParent[]> ParentList { get; set; } // Direct
    public XPointer<XModelQuat[]> Quats { get; set; } // Direct
    public XPointer<Vec3[]> Trans { get; set; } // Direct
    public XPointer<XModelPartClassification[]> PartClassification { get; set; } // Direct
    public XPointer<DObjAnimMat[]> BaseMat { get; set; } // Direct
    public XPointer<XPointer<MaterialAsset>[]> MaterialHandles { get; set; } // Direct -> ?
    public XModelLodInfo[] LodInfo { get; set; } = new XModelLodInfo[4];
    public byte MaxLoadedLod { get; set; }
    public byte NumLods { get; set; }
    public byte CollLod { get; set; }
    public byte Flags { get; set; }
    public XPointer<XModelCollSurf[]> CollSurfs { get; set; } // Direct
    public int NumCollSurfs { get; set; }
    public int Contents { get; set; }
    public XPointer<XBoneInfo[]> BoneInfo { get; set; } // Direct
    public float Radius { get; set; }
    public Bounds Bounds { get; set; }
    public XPointer<ushort[]> InvHighMipRadius { get; set; } // Direct
    public int MemUsage { get; set; }
    public XPointer<PhysPreset> PhysPreset { get; set; } // Alias
    public XPointer<PhysCollmap> PhysCollmap { get; set; } // Alias

    public override string? GetDisplayName => Name;
}

public readonly struct XModelParent(byte boneIndex)
{
    public byte BoneIndex { get; } = boneIndex;
}

public readonly struct XModelPartClassification(byte value)
{
    public byte Value { get; } = value;
}

public readonly struct XModelQuat(short x, short y, short z, short w)
{
    public short X { get; } = x;
    public short Y { get; } = y;
    public short Z { get; } = z;
    public short W { get; } = w;
}

public sealed class DObjAnimMat
{
    public Vec4 Quat { get; set; }
    public Vec3 Trans { get; set; }
    public float TransWeight { get; set; }
}

public sealed class XModelLodInfo
{
    public float Dist { get; set; }
    public ushort NumSurfs { get; set; }
    public ushort SurfIndex { get; set; }
    public XPointer<XModelSurfs> ModelSurfs { get; set; } // Alias
    public int[] PartBits { get; set; } = new int[6];
    public XPointer<XSurface[]> Surfs { get; set; } // Direct
}

public sealed class XSurface
{
    public int Offset { get; set; }
    public byte TileMode { get; set; }
    public byte Deformed { get; set; }
    public byte StreamFlags { get; set; }
    public byte Unknown03 { get; set; }
    public ushort VertCount { get; set; }
    public ushort TriCount { get; set; }
    public XPointer<ushort[]> TriIndices { get; set; } // Direct
    public XSurfaceVertexInfo VertInfo { get; set; } = new();
    public XPointer<byte[]> Verts0 { get; set; } // Direct
    public XSurfaceGpuBuffer Vb0 { get; set; } = new();
    public XPointer<byte[]> Verts1 { get; set; } // Direct
    public XSurfaceGpuBuffer Vb1 { get; set; } = new();
    public int VertListCount { get; set; }
    public XPointer<XRigidVertList[]> VertList { get; set; } // Direct
    public XSurfaceGpuBuffer IndexBuffer { get; set; } = new();
    public int[] PartBits { get; set; } = new int[5];
}

public sealed class XSurfaceVertexInfo
{
    public short[] VertCount { get; set; } = new short[4];
    public XPointer<ushort[]> VertsBlend { get; set; } // Direct
}

public sealed class XSurfaceGpuBuffer
{
    public int Word0 { get; set; }
    public int Word1 { get; set; }
}

public sealed class XRigidVertList
{
    public ushort BoneOffset { get; set; }
    public ushort VertCount { get; set; }
    public ushort TriOffset { get; set; }
    public ushort TriCount { get; set; }
    public XPointer<XSurfaceCollisionTree> CollisionTree { get; set; } // Direct
}

public sealed class XSurfaceCollisionTree
{
    public Vec3 Trans { get; set; }
    public Vec3 Scale { get; set; }
    public uint NodeCount { get; set; }
    public XPointer<XSurfaceCollisionNode[]> Nodes { get; set; } // Direct
    public uint LeafCount { get; set; }
    public XPointer<XSurfaceCollisionLeaf[]> Leafs { get; set; } // Direct
}

public sealed class XSurfaceCollisionNode
{
    public ushort[] Mins { get; set; } = new ushort[3];
    public ushort[] Maxs { get; set; } = new ushort[3];
    public ushort ChildBeginIndex { get; set; }
    public ushort ChildCount { get; set; }
}

public sealed class XSurfaceCollisionLeaf
{
    public ushort TriangleBeginIndex { get; set; }
}

public sealed class XModelCollSurf
{
    public byte[] RawBytes { get; set; } = [];
    public XPointer<XModelCollTri[]> CollTris { get; set; } // Direct
    public int NumCollTris { get; set; }
    public Bounds Bounds { get; set; }
    public int BoneIdx { get; set; }
    public int Contents { get; set; }
    public int SurfFlags { get; set; }
}

public sealed class XModelCollTri
{
    public Vec4 Plane { get; set; }
    public Vec4 SVec { get; set; }
    public Vec4 TVec { get; set; }
}

public sealed class XBoneInfo
{
    public Bounds Bounds { get; set; }
    public float RadiusSquared { get; set; }
}
