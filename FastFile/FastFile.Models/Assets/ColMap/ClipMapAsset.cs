using FastFile.Models.Assets.Fx;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.XModel;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using ModelBounds = FastFile.Models.Math.Bounds;
using ModelVec2 = FastFile.Models.Math.Vec2;
using ModelVec3 = FastFile.Models.Math.Vec3;
using VehiclePhysPresetAsset = FastFile.Models.Assets.Vehicle.PhysPresetAsset;

namespace FastFile.Models.Assets.ColMap;

public sealed class ClipMapAsset : BaseAsset
{
    public const int SerializedSize = 0x100;

    public XAssetType Type => XAssetType.ColMapMp;
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int IsInUse { get; init; }
    public int PlaneCount { get; init; }
    public XPointer<CPlane[]> PlanesPointer { get; init; }
    public IReadOnlyList<CPlane> Planes { get; init; } = [];
    public int NumStaticModels { get; init; }
    public XPointer<ClipStaticModel[]> StaticModelListPointer { get; init; }
    public IReadOnlyList<ClipStaticModel> StaticModelList { get; init; } = [];
    public int NumMaterials { get; init; }
    public XPointer<ClipMaterial[]> MaterialsPointer { get; init; }
    public IReadOnlyList<ClipMaterial> Materials { get; init; } = [];
    public int NumBrushSides { get; init; }
    public XPointer<CBrushSide[]> BrushSidesPointer { get; init; }
    public IReadOnlyList<CBrushSide> BrushSides { get; init; } = [];
    public int NumBrushEdges { get; init; }
    public XPointer<byte[]> BrushEdgesPointer { get; init; }
    public IReadOnlyList<byte> BrushEdges { get; init; } = [];
    public int NumNodes { get; init; }
    public XPointer<CNode[]> NodesPointer { get; init; }
    public IReadOnlyList<CNode> Nodes { get; init; } = [];
    public int NumLeafs { get; init; }
    public XPointer<CLeaf[]> LeafsPointer { get; init; }
    public IReadOnlyList<CLeaf> Leafs { get; init; } = [];
    public int LeafBrushNodesCount { get; init; }
    public XPointer<CLeafBrushNode[]> LeafBrushNodesPointer { get; init; }
    public IReadOnlyList<CLeafBrushNode> LeafBrushNodes { get; init; } = [];
    public int NumLeafBrushes { get; init; }
    public XPointer<ushort[]> LeafBrushesPointer { get; init; }
    public IReadOnlyList<ushort> LeafBrushes { get; init; } = [];
    public int NumLeafSurfaces { get; init; }
    public XPointer<uint[]> LeafSurfacesPointer { get; init; }
    public IReadOnlyList<uint> LeafSurfaces { get; init; } = [];
    public int VertCount { get; init; }
    public XPointer<ModelVec3[]> VertsPointer { get; init; }
    public IReadOnlyList<ModelVec3> Verts { get; init; } = [];
    public int TriCount { get; init; }
    public XPointer<ushort[]> TriIndicesPointer { get; init; }
    public IReadOnlyList<ushort> TriIndices { get; init; } = [];
    public XPointer<byte[]> TriEdgeIsWalkablePointer { get; init; }
    public IReadOnlyList<byte> TriEdgeIsWalkable { get; init; } = [];
    public int BorderCount { get; init; }
    public XPointer<CollisionBorder[]> BordersPointer { get; init; }
    public IReadOnlyList<CollisionBorder> Borders { get; init; } = [];
    public int PartitionCount { get; init; }
    public XPointer<CollisionPartition[]> PartitionsPointer { get; init; }
    public IReadOnlyList<CollisionPartition> Partitions { get; init; } = [];
    public int AabbTreeCount { get; init; }
    public XPointer<CollisionAabbTree[]> AabbTreesPointer { get; init; }
    public IReadOnlyList<CollisionAabbTree> AabbTrees { get; init; } = [];
    public int NumSubModels { get; init; }
    public XPointer<CModel[]> CModelsPointer { get; init; }
    public IReadOnlyList<CModel> CModels { get; init; } = [];
    public ushort NumBrushes { get; init; }
    public ushort Pad8ETo8F { get; init; }
    public XPointer<CBrush[]> BrushesPointer { get; init; }
    public IReadOnlyList<CBrush> Brushes { get; init; } = [];
    public XPointer<ModelBounds[]> BrushBoundsPointer { get; init; }
    public IReadOnlyList<ModelBounds> BrushBounds { get; init; } = [];
    public XPointer<uint[]> BrushContentsPointer { get; init; }
    public IReadOnlyList<uint> BrushContents { get; init; } = [];
    public XPointer<MapEnts> MapEntsPointer { get; init; }
    public MapEnts? MapEnts { get; init; }
    public ushort SModelNodeCount { get; init; }
    public ushort PadA2ToA3 { get; init; }
    public XPointer<SModelAabbNode[]> SModelNodesPointer { get; init; }
    public IReadOnlyList<SModelAabbNode> SModelNodes { get; init; } = [];
    public IReadOnlyList<ushort> DynEntCount { get; init; } = [];
    public IReadOnlyList<XPointer<DynEntityDef[]>> DynEntDefListPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<DynEntityDef>> DynEntDefList { get; init; } = [];
    public IReadOnlyList<XPointer<DynEntityPose[]>> DynEntPoseListPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<DynEntityPose>> DynEntPoseList { get; init; } = [];
    public IReadOnlyList<XPointer<DynEntityClient[]>> DynEntClientListPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<DynEntityClient>> DynEntClientList { get; init; } = [];
    public IReadOnlyList<XPointer<DynEntityColl[]>> DynEntCollListPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<DynEntityColl>> DynEntCollList { get; init; } = [];
    public uint Checksum { get; init; }
    public IReadOnlyList<byte> UnknownD0ToFF { get; init; } = [];
}

public sealed class ClipStaticModel
{
    public const int SerializedSize = 0x4C;

    public XPointer<XModelAsset> XModelPointer { get; init; }
    public XModelAsset? XModel { get; init; }
    public ModelVec3 Origin { get; init; }
    public IReadOnlyList<ModelVec3> InvScaledAxis { get; init; } = [];
    public ModelVec3 AbsMin { get; init; }
    public ModelVec3 AbsMax { get; init; }
}

public sealed class ClipMaterial
{
    public const int SerializedSize = 0x0C;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int SurfaceFlags { get; init; }
    public int Contents { get; init; }
}

public sealed class CNode
{
    public const int SerializedSize = 0x08;

    public XPointer<CPlane> PlanePointer { get; init; }
    public CPlane? Plane { get; init; }
    public IReadOnlyList<short> Children { get; init; } = [];
}

public sealed class CLeaf
{
    public const int SerializedSize = 0x28;

    public ushort FirstCollAabbIndex { get; init; }
    public ushort CollAabbCount { get; init; }
    public int BrushContents { get; init; }
    public int TerrainContents { get; init; }
    public ModelVec3 Mins { get; init; }
    public ModelVec3 Maxs { get; init; }
    public int LeafBrushNode { get; init; }
}

public sealed class CLeafBrushNode
{
    public const int SerializedSize = 0x14;

    public byte Axis { get; init; }
    public byte Pad01 { get; init; }
    public short LeafBrushCount { get; init; }
    public int Contents { get; init; }
    public CLeafBrushNodeData Data { get; init; } = new();
}

public sealed class CLeafBrushNodeData
{
    public XPointer<ushort[]> BrushesPointer { get; init; }
    public IReadOnlyList<ushort> Brushes { get; init; } = [];
    public IReadOnlyList<byte> LeafUnionPad { get; init; } = [];
    public CLeafBrushNodeChildren? Children { get; init; }
}

public sealed class CLeafBrushNodeChildren
{
    public const int SerializedSize = 0x0C;

    public IReadOnlyList<ushort> ChildOffsets { get; init; } = [];
}

public sealed class CollisionBorder
{
    public const int SerializedSize = 0x1C;

    public IReadOnlyList<float> DistEq { get; init; } = [];
    public float ZBase { get; init; }
    public float ZSlope { get; init; }
    public float Start { get; init; }
    public float Length { get; init; }
}

public sealed class CollisionPartition
{
    public const int SerializedSize = 0x0C;

    public byte TriCount { get; init; }
    public byte BorderCount { get; init; }
    public byte FirstVertSegment { get; init; }
    public byte Pad03 { get; init; }
    public int FirstTri { get; init; }
    public XPointer<CollisionBorder[]> BordersPointer { get; init; }
    public IReadOnlyList<CollisionBorder> Borders { get; init; } = [];
}

public sealed class CollisionAabbTree
{
    public const int SerializedSize = 0x20;

    public ModelVec3 Origin { get; init; }
    public ModelVec3 HalfSize { get; init; }
    public ushort MaterialIndex { get; init; }
    public ushort ChildCount { get; init; }
    public int FirstChildOrPartitionIndex { get; init; }
}

public sealed class CModel
{
    public const int SerializedSize = 0x44;

    public ModelVec3 Mins { get; init; }
    public ModelVec3 Maxs { get; init; }
    public float Radius { get; init; }
    public CLeaf Leaf { get; init; } = new();
}

public sealed class SModelAabbNode
{
    public const int SerializedSize = 0x1C;

    public ModelBounds Bounds { get; init; } = new();
    public ushort FirstChild { get; init; }
    public ushort ChildCount { get; init; }
}

public sealed class GfxPlacement
{
    public const int SerializedSize = 0x1C;

    public IReadOnlyList<float> Quat { get; init; } = [];
    public ModelVec3 Origin { get; init; }
}

public sealed class DynEntityDef
{
    public const int SerializedSize = 0x5C;

    public int Type { get; init; }
    public GfxPlacement Pose { get; init; } = new();
    public XPointer<XModelAsset> XModelPointer { get; init; }
    public XModelAsset? XModel { get; init; }
    public ushort BrushModel { get; init; }
    public ushort PhysicsBrushModel { get; init; }
    public XPointer<FxEffectDefAsset> DestroyFxPointer { get; init; }
    public FxEffectDefAsset? DestroyFx { get; init; }
    public XPointer<VehiclePhysPresetAsset> PhysPresetPointer { get; init; }
    public VehiclePhysPresetAsset? PhysPreset { get; init; }
    public int Health { get; init; }
    public PhysMass Mass { get; init; } = new();
    public int Contents { get; init; }
}

public sealed class DynEntityPose
{
    public const int SerializedSize = 0x20;

    public GfxPlacement Pose { get; init; } = new();
    public float Radius { get; init; }
}

public sealed class DynEntityClient
{
    public const int SerializedSize = 0x0C;

    public int PhysObjId { get; init; }
    public ushort Flags { get; init; }
    public ushort LightingHandle { get; init; }
    public int Health { get; init; }
}

public sealed class DynEntityColl
{
    public const int SerializedSize = 0x14;

    public ushort Sector { get; init; }
    public ushort NextEntInSector { get; init; }
    public ModelVec2 LinkMins { get; init; }
    public ModelVec2 LinkMaxs { get; init; }
}

public sealed class MapEnts
{
    public const int SerializedSize = 0x2C;

    public int Offset { get; init; }
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<byte[]> EntityStringPointer { get; init; }
    public IReadOnlyList<byte> EntityStringBytes { get; init; } = [];
    public string? EntityString { get; init; }
    public int NumEntityChars { get; init; }
    public MapTriggers Trigger { get; init; } = new();
    public XPointer<Stage[]> StagesPointer { get; init; }
    public IReadOnlyList<Stage> Stages { get; init; } = [];
    public byte StageCount { get; init; }
    public IReadOnlyList<byte> Pad29To2B { get; init; } = [];
}

public sealed class MapTriggers
{
    public const int SerializedSize = 0x18;

    public uint Count { get; init; }
    public XPointer<TriggerModel[]> ModelsPointer { get; init; }
    public IReadOnlyList<TriggerModel> Models { get; init; } = [];
    public uint HullCount { get; init; }
    public XPointer<TriggerHull[]> HullsPointer { get; init; }
    public IReadOnlyList<TriggerHull> Hulls { get; init; } = [];
    public uint SlabCount { get; init; }
    public XPointer<TriggerSlab[]> SlabsPointer { get; init; }
    public IReadOnlyList<TriggerSlab> Slabs { get; init; } = [];
}

public sealed class TriggerModel
{
    public const int SerializedSize = 0x08;

    public int Contents { get; init; }
    public ushort HullCount { get; init; }
    public ushort FirstHull { get; init; }
}

public sealed class TriggerHull
{
    public const int SerializedSize = 0x20;

    public ModelBounds Bounds { get; init; } = new();
    public int Contents { get; init; }
    public ushort SlabCount { get; init; }
    public ushort FirstSlab { get; init; }
}

public sealed class TriggerSlab
{
    public const int SerializedSize = 0x14;

    public ModelVec3 Dir { get; init; }
    public float MidPoint { get; init; }
    public float HalfSize { get; init; }
}

public sealed class Stage
{
    public const int SerializedSize = 0x14;

    public XPointer<string> StageNamePointer { get; init; }
    public string? StageName { get; init; }
    public ModelVec3 Origin { get; init; }
    public ushort TriggerIndex { get; init; }
    public byte SunPrimaryLightIndex { get; init; }
    public byte Pad13 { get; init; }
}
