using FastFile.Models.Data;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.XModels;

public class XModel() : BaseAsset(XAssetType.XModel)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public byte NumBones { get; set; }
    public byte NumRootBones { get; set; }
    public byte NumSurfs { get; set; }
    public byte LodRampType { get; set; }
    public float Scale { get; set; }
    public int[] NoScalePartBits { get; set; } = new int[6];
    public ZonePointer<ushort[]> BoneNames { get; set; }
    public ZonePointer<XModelParent[]> ParentList { get; set; }
    public ZonePointer<XModelQuat[]> Quats { get; set; }
    public ZonePointer<Vec3[]> Trans { get; set; }
    public ZonePointer<XModelPartClassification[]> PartClassification { get; set; }
    public ZonePointer<DObjAnimMat[]> BaseMat { get; set; }
    public ZonePointer<ZonePointer<MaterialAsset>[]> MaterialHandles { get; set; }
    public XModelLodInfo[] LodInfo { get; set; } = new XModelLodInfo[4];
    public byte MaxLoadedLod { get; set; }
    public byte NumLods { get; set; }
    public byte CollLod { get; set; }
    public byte Flags { get; set; }
    public ZonePointer<XModelCollSurf[]> CollSurfs { get; set; }
    public int NumCollSurfs { get; set; }
    public int Contents { get; set; }
    public ZonePointer<XBoneInfo[]> BoneInfo { get; set; }
    public float Radius { get; set; }
    public Bounds Bounds { get; set; }
    public int MemUsage { get; set; }
    public bool Bad { get; set; }
    public byte BadPadding0 { get; set; }
    public byte BadPadding1 { get; set; }
    public byte BadPadding2 { get; set; }
    public ZonePointer<PhysPreset> PhysPreset { get; set; }
    public ZonePointer<PhysCollmap> PhysCollmap { get; set; }

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
    public ZonePointer<XModelSurfs> ModelSurfs { get; set; }
    public int[] PartBits { get; set; } = new int[6];
    public ZonePointer<XSurface[]> Surfs { get; set; }
}

public sealed class XSurface
{
    public byte TileMode { get; set; }
}

public sealed class XModelCollSurf
{
    public ZonePointer<XModelCollTri[]> CollTris { get; set; }
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
