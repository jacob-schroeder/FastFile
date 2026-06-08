using FastFile.Models.Data;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.XModels;

public class XModel() : BaseAsset(XAssetType.XModel)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public byte NumBones { get; set; }
    public byte NumRootBones { get; set; }
    public byte NumSurfs { get; set; }
    public byte LodRampType { get; set; }
    public float Scale { get; set; }
    public int[] NoScalePartBits { get; set; } = new int[6];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumBones))]
    public DirectPointer<ushort[]> BoneNames { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<XModelParent[]> ParentList { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<XModelQuat[]> Quats { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<Vec3[]> Trans { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumBones))]
    public DirectPointer<XModelPartClassification[]> PartClassification { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumBones))]
    public DirectPointer<DObjAnimMat[]> BaseMat { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumSurfs))]
    public DirectPointer<ZonePointer<MaterialAsset>[]> MaterialHandles { get; set; }
    public XModelLodInfo[] LodInfo { get; set; } = new XModelLodInfo[4];
    public byte MaxLoadedLod { get; set; }
    public byte NumLods { get; set; }
    public byte CollLod { get; set; }
    public byte Flags { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumCollSurfs))]
    public DirectPointer<XModelCollSurf[]> CollSurfs { get; set; }
    public int NumCollSurfs { get; set; }
    public int Contents { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumBones))]
    public DirectPointer<XBoneInfo[]> BoneInfo { get; set; }
    public float Radius { get; set; }
    public Bounds Bounds { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumSurfs))]
    public DirectPointer<ushort[]> InvHighMipRadius { get; set; }
    public int MemUsage { get; set; }
    public AliasPointer<PhysPreset> PhysPreset { get; set; }
    public AliasPointer<PhysCollmap> PhysCollmap { get; set; }

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
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<XModelSurfs> ModelSurfs { get; set; }
    public int[] PartBits { get; set; } = new int[6];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(NumSurfs))]
    public DirectPointer<XSurface[]> Surfs { get; set; }
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
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<ushort[]> TriIndices { get; set; }
    public XSurfaceVertexInfo VertInfo { get; set; } = new();
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<byte[]> Verts0 { get; set; }
    public XSurfaceGpuBuffer Vb0 { get; set; } = new();
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<byte[]> Verts1 { get; set; }
    public XSurfaceGpuBuffer Vb1 { get; set; } = new();
    public int VertListCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(VertListCount))]
    public DirectPointer<XRigidVertList[]> VertList { get; set; }
    public XSurfaceGpuBuffer IndexBuffer { get; set; } = new();
    public int[] PartBits { get; set; } = new int[5];
}

public sealed class XSurfaceVertexInfo
{
    public short[] VertCount { get; set; } = new short[4];
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<ushort[]> VertsBlend { get; set; }
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
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<XSurfaceCollisionTree> CollisionTree { get; set; }
}

public sealed class XSurfaceCollisionTree
{
    public Vec3 Trans { get; set; }
    public Vec3 Scale { get; set; }
    public uint NodeCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(NodeCount))]
    public DirectPointer<XSurfaceCollisionNode[]> Nodes { get; set; }
    public uint LeafCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(LeafCount))]
    public DirectPointer<XSurfaceCollisionLeaf[]> Leafs { get; set; }
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
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(NumCollTris))]
    public DirectPointer<XModelCollTri[]> CollTris { get; set; }
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
