using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Math;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.XModel;

public sealed class XModelAsset : BaseAsset
{
    public const int SerializedSize = 0x120;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public byte NumBones { get; init; }
    public byte NumRootBones { get; init; }
    public byte NumSurfs { get; init; }
    public byte Pad07 { get; init; }
    public float Scale { get; init; }
    public IReadOnlyList<uint> NoScalePartBits { get; init; } = [];
    public XPointer<ushort[]> BoneNamesPointer { get; init; }
    public IReadOnlyList<ushort> BoneNames { get; init; } = [];
    public XPointer<byte[]> ParentListPointer { get; init; }
    public IReadOnlyList<byte> ParentList { get; init; } = [];
    public XPointer<short[]> QuatsPointer { get; init; }
    public IReadOnlyList<short> Quats { get; init; } = [];
    public XPointer<float[]> TransPointer { get; init; }
    public IReadOnlyList<float> Trans { get; init; } = [];
    public XPointer<byte[]> PartClassificationPointer { get; init; }
    public IReadOnlyList<byte> PartClassification { get; init; } = [];
    public XPointer<byte[]> BaseMatPointer { get; init; }
    public IReadOnlyList<DObjAnimMat> BaseMat { get; init; } = [];
    public XPointer<XPointer<MaterialAsset>[]> MaterialHandlesPointer { get; init; }
    public IReadOnlyList<XPointer<MaterialAsset>> MaterialPointers { get; init; } = [];
    public IReadOnlyList<MaterialAsset?> Materials { get; init; } = [];
    public IReadOnlyList<XModelLodInfo> Lods { get; init; } = [];
    public byte MaxLoadedLod { get; init; }
    public byte NumLods { get; init; }
    public byte CollLod { get; init; }
    public byte Flags { get; init; }
    public XPointer<byte[]> CollSurfsPointer { get; init; }
    public int NumCollSurfs { get; init; }
    public int Contents { get; init; }
    public IReadOnlyList<XModelCollSurf> CollSurfs { get; init; } = [];
    public XPointer<byte[]> BoneInfoPointer { get; init; }
    public IReadOnlyList<XBoneInfo> BoneInfo { get; init; } = [];
    public float Radius { get; init; }
    public Bounds Bounds { get; init; } = new();
    public XPointer<ushort[]> InvHighMipRadiusPointer { get; init; }
    public IReadOnlyList<ushort> InvHighMipRadius { get; init; } = [];
    public int MemUsage { get; init; }
    public XPointer<PhysPresetAsset> PhysPresetPointer { get; init; }
    public PhysPresetAsset? PhysPreset { get; init; }
    public XPointer<PhysCollmapAsset> PhysCollmapPointer { get; init; }
    public PhysCollmapAsset? PhysCollmap { get; init; }
}

public sealed class XModelSurfsAsset : BaseAsset
{
    public const int SerializedSize = 0x24;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<byte[]> SurfsPointer { get; init; }
    public ushort NumSurfs { get; init; }
    public ushort Pad0A { get; init; }
    public IReadOnlyList<uint> PartBits { get; init; } = [];
    public IReadOnlyList<XSurface> Surfaces { get; init; } = [];
}

public sealed class PhysPresetAsset : BaseAsset
{
    public const int SerializedSize = 0x2c;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<string> SndAliasPrefixPointer { get; init; }
    public string? SndAliasPrefix { get; init; }
}

public sealed class XModelLodInfo
{
    public const int SerializedSize = 0x28;

    public float Dist { get; init; }
    public ushort NumSurfs { get; init; }
    public ushort SurfIndex { get; init; }
    public XPointer<XModelSurfsAsset> ModelSurfsPointer { get; init; }
    public IReadOnlyList<uint> PartBits { get; init; } = [];
    public XPointer<byte[]> SurfsRuntimePointer { get; init; }
    public XModelSurfsAsset? ModelSurfs { get; init; }
}

public sealed record DObjAnimMat(DObjQuat Quat, Vec3 Trans, float TransWeight)
{
    public const int SerializedSize = 0x20;
}

public sealed record DObjQuat(float X, float Y, float Z, float W);

public sealed record XModelCollSurf(Bounds Bounds, int BoneIndex, int Contents, int SurfaceFlags)
{
    public const int SerializedSize = 0x24;
}

public sealed record XBoneInfo(Bounds Bounds, float RadiusSquared)
{
    public const int SerializedSize = 0x1c;
}

public sealed class XSurface
{
    public const int SerializedSize = 0x54;

    public ushort FlagsOrPad00 { get; init; }
    public byte StreamFlags { get; init; }
    public byte Pad03 { get; init; }
    public ushort VertCount { get; init; }
    public ushort TriCount { get; init; }
    public XPointer<ushort[]> TriIndicesPointer { get; init; }
    public IReadOnlyList<ushort> TriIndices { get; init; } = [];
    public XSurfaceVertexInfo VertexInfo { get; init; } = new();
    public XPointer<byte[]> Verts0Pointer { get; init; }
    public IReadOnlyList<byte> Verts0 { get; init; } = [];
    public GfxVertexBuffer Vb0 { get; init; } = new();
    public XPointer<byte[]> Verts1Pointer { get; init; }
    public IReadOnlyList<byte> Verts1 { get; init; } = [];
    public GfxVertexBuffer Vb1 { get; init; } = new();
    public int VertListCount { get; init; }
    public XPointer<XRigidVertList[]> VertListPointer { get; init; }
    public IReadOnlyList<XRigidVertList> VertList { get; init; } = [];
    public GfxIndexBuffer IndexBuffer { get; init; } = new();
    public IReadOnlyList<uint> PartBits { get; init; } = [];
}

public sealed class XSurfaceVertexInfo
{
    public ushort Blend0 { get; init; }
    public ushort Blend1 { get; init; }
    public ushort Blend2 { get; init; }
    public ushort Blend3 { get; init; }
    public XPointer<ushort[]> VertsBlendPointer { get; init; }
    public IReadOnlyList<ushort> VertsBlend { get; init; } = [];
}

public sealed class GfxVertexBuffer
{
    public int StreamSource { get; init; }
    public int DataOffset { get; init; }
}

public sealed class GfxIndexBuffer
{
    public int DataOffset { get; init; }
}

public sealed class XRigidVertList
{
    public const int SerializedSize = 0x0c;

    public ushort BoneOffset { get; init; }
    public ushort VertCount { get; init; }
    public ushort TriOffset { get; init; }
    public ushort TriCount { get; init; }
    public XPointer<XSurfaceCollisionTree> CollisionTreePointer { get; init; }
    public XSurfaceCollisionTree? CollisionTree { get; init; }
}

public sealed class XSurfaceCollisionTree
{
    public const int SerializedSize = 0x28;

    public Vec3 Trans { get; init; } = new();
    public Vec3 Scale { get; init; } = new();
    public int NodeCount { get; init; }
    public XPointer<XSurfaceCollisionNode[]> NodesPointer { get; init; }
    public IReadOnlyList<XSurfaceCollisionNode> Nodes { get; init; } = [];
    public int LeafCount { get; init; }
    public XPointer<XSurfaceCollisionLeaf[]> LeafsPointer { get; init; }
    public IReadOnlyList<XSurfaceCollisionLeaf> Leafs { get; init; } = [];
}

public sealed record XSurfaceCollisionAabb(
    ushort MinsX,
    ushort MinsY,
    ushort MinsZ,
    ushort MaxsX,
    ushort MaxsY,
    ushort MaxsZ)
{
    public const int SerializedSize = 0x0c;
}

public sealed record XSurfaceCollisionNode(
    XSurfaceCollisionAabb Aabb,
    ushort ChildBeginIndex,
    ushort ChildCount)
{
    public const int SerializedSize = 0x10;
}

public sealed record XSurfaceCollisionLeaf(ushort TriangleBeginIndex)
{
    public const int SerializedSize = 0x02;
}
