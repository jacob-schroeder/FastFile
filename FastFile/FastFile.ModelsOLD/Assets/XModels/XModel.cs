using FastFile.ModelsOLD.Assets.Physics;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Utils;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;

namespace FastFile.ModelsOLD.Assets.XModels;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x120)]
public class XModel() : BaseAsset(XAssetType.XModel)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public byte NumBones { get; set; }

    [XField(Offset = 0x05)]
    public byte NumRootBones { get; set; }

    [XField(Offset = 0x06)]
    public byte NumSurfs { get; set; }

    [XField(Offset = 0x07)]
    public byte LodRampType { get; set; }

    [XField(Offset = 0x08)]
    public float Scale { get; set; }

    [XField(Offset = 0x0C)]
    public int[] NoScalePartBits { get; set; } = new int[6];

    [XField(Offset = 0x24)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(BoneNameCount))]
    public XPointer<ushort[]> BoneNames { get; set; } // Direct

    [XField(Offset = 0x28)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(ParentCount))]
    public XPointer<byte[]> ParentList { get; set; } // Direct

    [XField(Offset = 0x2C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(QuatComponentCount))]
    public XPointer<short[]> Quats { get; set; } // Direct

    [XField(Offset = 0x30)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(PartCount))]
    public XPointer<Vec3[]> Trans { get; set; } // Direct

    [XField(Offset = 0x34)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(BoneNameCount))]
    public XPointer<byte[]> PartClassification { get; set; } // Direct

    [XField(Offset = 0x38)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(BoneNameCount))]
    public XPointer<DObjAnimMat[]> BaseMat { get; set; } // Direct

    [XField(Offset = 0x3C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Alias,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(MaterialHandleCount))]
    public XPointer<XPointer<Material.Material>[]> MaterialHandles { get; set; } // Direct -> ?

    [XField(Offset = 0x40)]
    public XModelLodInfo[] LodInfo { get; set; } = new XModelLodInfo[4];

    [XField(Offset = 0xE0)]
    public byte MaxLoadedLod { get; set; }

    [XField(Offset = 0xE1)]
    public byte NumLods { get; set; }

    [XField(Offset = 0xE2)]
    public byte CollLod { get; set; }

    [XField(Offset = 0xE3)]
    public byte Flags { get; set; }

    [XField(Offset = 0xE4)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(NumCollSurfs))]
    public XPointer<XModelCollSurf[]> CollSurfs { get; set; } // Direct

    [XField(Offset = 0xE8)]
    public int NumCollSurfs { get; set; }

    [XField(Offset = 0xEC)]
    public int Contents { get; set; }

    [XField(Offset = 0xF0)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(BoneNameCount))]
    public XPointer<XBoneInfo[]> BoneInfo { get; set; } // Direct

    [XField(Offset = 0xF4)]
    public float Radius { get; set; }

    [XField(Offset = 0xF8)]
    public Bounds Bounds { get; set; }

    [XField(Offset = 0x110)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(MaterialHandleCount))]
    public XPointer<ushort[]> InvHighMipRadius { get; set; } // Direct

    [XField(Offset = 0x114)]
    public int MemUsage { get; set; }

    [XField(Offset = 0x118)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<PhysPreset> PhysPreset { get; set; } // Alias

    [XField(Offset = 0x11C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<PhysCollmap> PhysCollmap { get; set; } // Alias

    public int BoneNameCount => NumBones;
    public int ParentCount => Math.Max(0, NumBones - NumRootBones);
    public int PartCount => Math.Max(0, NumBones - NumRootBones);
    public int QuatComponentCount => PartCount * 4;
    public int MaterialHandleCount => NumSurfs;

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

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x20)]
public sealed class DObjAnimMat
{
    [XField(Offset = 0x00)]
    public Vec4 Quat { get; set; }

    [XField(Offset = 0x10)]
    public Vec3 Trans { get; set; }

    [XField(Offset = 0x1C)]
    public float TransWeight { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x28)]
public sealed class XModelLodInfo
{
    [XField(Offset = 0x00)]
    public float Dist { get; set; }

    [XField(Offset = 0x04)]
    public ushort NumSurfs { get; set; }

    [XField(Offset = 0x06)]
    public ushort SurfIndex { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<XModelSurfs> ModelSurfs { get; set; } // Alias

    [XField(Offset = 0x0C)]
    public int[] PartBits { get; set; } = new int[6];

    [XField(Offset = 0x24)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.None,
        CountMember = nameof(NumSurfs))]
    public XPointer<XSurface[]> Surfs { get; set; } // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x54)]
public sealed class XSurface
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    public byte TileMode { get; set; }

    [XField(Offset = 0x01)]
    public byte Deformed { get; set; }

    [XField(Offset = 0x02)]
    public byte StreamFlags { get; set; }

    [XField(Offset = 0x03)]
    public byte Unknown03 { get; set; }

    [XField(Offset = 0x04)]
    public ushort VertCount { get; set; }

    [XField(Offset = 0x06)]
    public ushort TriCount { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<ushort[]> TriIndices { get; set; } = null!; // Direct

    [XField(Offset = 0x0C)]
    public XSurfaceVertexInfo VertInfo { get; set; } = new();

    [XField(Offset = 0x18)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<byte[]> Verts0 { get; set; } = null!; // Direct

    [XField(Offset = 0x1C)]
    public XSurfaceGpuBuffer Vb0 { get; set; } = new();

    [XField(Offset = 0x24)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<byte[]> Verts1 { get; set; } = null!; // Direct

    [XField(Offset = 0x28)]
    public XSurfaceGpuBuffer Vb1 { get; set; } = new();

    [XField(Offset = 0x30)]
    public int VertListCount { get; set; }

    [XField(Offset = 0x34)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<XRigidVertList[]> VertList { get; set; } = null!; // Direct

    [XField(Offset = 0x38)]
    public XSurfaceGpuBuffer IndexBuffer { get; set; } = new();

    [XField(Offset = 0x40)]
    public int[] PartBits { get; set; } = new int[5];

    public int TriIndexCount => TriCount * 3;
    public int VertexByteCount => VertCount * 0x10;
    public bool TriIndicesInCurrentBlock => (StreamFlags & 0x04) != 0;
    public bool Verts0InCurrentBlock => (StreamFlags & 0x01) != 0;
    public bool Verts1InCurrentBlock => (StreamFlags & 0x02) != 0;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class XSurfaceVertexInfo
{
    [XField(Offset = 0x00)]
    public ushort[] VertCount { get; set; } = new ushort[4];

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<ushort[]> VertsBlend { get; set; } = null!; // Direct

    public int BlendVertCount =>
        VertCount[0] +
        (VertCount[1] * 3) +
        (VertCount[2] * 5) +
        (VertCount[3] * 7);
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class XSurfaceGpuBuffer
{
    [XField(Offset = 0x00)]
    public int Word0 { get; set; }

    [XField(Offset = 0x04)]
    public int Word1 { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class XRigidVertList
{
    [XField(Offset = 0x00)]
    public ushort BoneOffset { get; set; }

    [XField(Offset = 0x02)]
    public ushort VertCount { get; set; }

    [XField(Offset = 0x04)]
    public ushort TriOffset { get; set; }

    [XField(Offset = 0x06)]
    public ushort TriCount { get; set; }

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<XSurfaceCollisionTree> CollisionTree { get; set; } = null!; // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x28)]
public sealed class XSurfaceCollisionTree
{
    [XField(Offset = 0x00)]
    public Vec3 Trans { get; set; }

    [XField(Offset = 0x0C)]
    public Vec3 Scale { get; set; }

    [XField(Offset = 0x18)]
    public uint NodeCount { get; set; }

    [XField(Offset = 0x1C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 16,
        CountMember = nameof(NodeCount))]
    public XPointer<XSurfaceCollisionNode[]> Nodes { get; set; } = null!; // Direct

    [XField(Offset = 0x20)]
    public uint LeafCount { get; set; }

    [XField(Offset = 0x24)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(LeafCount))]
    public XPointer<XSurfaceCollisionLeaf[]> Leafs { get; set; } = null!; // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class XSurfaceCollisionNode
{
    [XField(Offset = 0x00)]
    public ushort[] Mins { get; set; } = new ushort[3];

    [XField(Offset = 0x06)]
    public ushort[] Maxs { get; set; } = new ushort[3];

    [XField(Offset = 0x0C)]
    public ushort ChildBeginIndex { get; set; }

    [XField(Offset = 0x0E)]
    public ushort ChildCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x02)]
public sealed class XSurfaceCollisionLeaf
{
    [XField(Offset = 0x00)]
    public ushort TriangleBeginIndex { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x24)]
public sealed class XModelCollSurf
{
    [XField(Offset = 0x00, Count = 0x24)]
    public byte[] RawBytes { get; set; } = new byte[0x24];
}

public sealed class XModelCollTri
{
    public Vec4 Plane { get; set; }
    public Vec4 SVec { get; set; }
    public Vec4 TVec { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1C)]
public sealed class XBoneInfo
{
    [XField(Offset = 0x00)]
    public Bounds Bounds { get; set; }

    [XField(Offset = 0x18)]
    public float RadiusSquared { get; set; }
}
