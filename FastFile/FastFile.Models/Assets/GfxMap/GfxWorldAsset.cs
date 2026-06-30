using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.XModel;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.GfxMap;

public sealed class GfxWorldAsset : BaseAsset
{
    public const int SerializedSize = 0x288;

    public XAssetType Type => XAssetType.GfxMap;

    // 0x00, 0x04: PS3 root stores each cell into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<string> BaseNamePointer { get; init; }
    public string? BaseName { get; init; }

    public int PlaneCount { get; init; }
    public int NodeCount { get; init; }
    public int SurfaceCount { get; init; }
    public uint SkyCount { get; init; }
    public XPointer<GfxSky[]> SkiesPointer { get; init; }
    public IReadOnlyList<GfxSky> Skies { get; init; } = [];
    public int PrimaryLightFirstShadowable { get; init; }
    public int PrimaryLightCount { get; init; }
    public int SortKeyLitDecal { get; init; }
    public int SortKeyEffectDecal { get; init; }
    public int SortKeyEffectAuto { get; init; }
    public int SortKeyDistortion { get; init; }

    public GfxWorldDpvsPlanes DpvsPlanes { get; init; } = new();
    public XPointer<GfxCellTreeCount[]> CellTreeCountsPointer { get; init; }
    public IReadOnlyList<GfxCellTreeCount> CellTreeCounts { get; init; } = [];
    public XPointer<GfxAabbTree[]> CellTreesPointer { get; init; }
    public IReadOnlyList<GfxCellTree> CellTrees { get; init; } = [];
    public XPointer<GfxCell[]> CellsPointer { get; init; }
    public IReadOnlyList<GfxCell> Cells { get; init; } = [];

    public GfxWorldDraw WorldDraw { get; init; } = new();
    public GfxLightGrid LightGrid { get; init; } = new();
    public int ModelCount { get; init; }
    public XPointer<GfxBrushModel[]> ModelsPointer { get; init; }
    public IReadOnlyList<GfxBrushModel> Models { get; init; } = [];
    public IReadOnlyList<float> Mins { get; init; } = [];
    public IReadOnlyList<float> Maxs { get; init; } = [];
    public uint Checksum { get; init; }
    public int MaterialMemoryCount { get; init; }
    public XPointer<MaterialMemory[]> MaterialMemoryPointer { get; init; }
    public IReadOnlyList<MaterialMemory> MaterialMemory { get; init; } = [];
    public Sunflare Sun { get; init; } = new();
    public IReadOnlyList<float> OutdoorLookupMatrix { get; init; } = [];
    public XPointer<GfxImageAsset> OutdoorImagePointer { get; init; }
    public GfxImageAsset? OutdoorImage { get; init; }

    public XPointer<uint[]> CellCasterBitsPointer { get; init; }
    public IReadOnlyList<uint> CellCasterBits { get; init; } = [];
    public XPointer<uint[]> CellCasterBits2Pointer { get; init; }
    public IReadOnlyList<uint> CellCasterBits2 { get; init; } = [];
    public XPointer<GfxSceneDynModel[]> SceneDynModelPointer { get; init; }
    public IReadOnlyList<GfxSceneDynModel> SceneDynModels { get; init; } = [];
    public XPointer<GfxSceneDynBrush[]> SceneDynBrushPointer { get; init; }
    public IReadOnlyList<GfxSceneDynBrush> SceneDynBrushes { get; init; } = [];
    public XPointer<uint[]> PrimaryLightEntityShadowVisPointer { get; init; }
    public IReadOnlyList<uint> PrimaryLightEntityShadowVis { get; init; } = [];
    public XPointer<uint[]> PrimaryLightDynEntShadowVis0Pointer { get; init; }
    public IReadOnlyList<uint> PrimaryLightDynEntShadowVis0 { get; init; } = [];
    public XPointer<uint[]> PrimaryLightDynEntShadowVis1Pointer { get; init; }
    public IReadOnlyList<uint> PrimaryLightDynEntShadowVis1 { get; init; } = [];
    public XPointer<byte[]> PrimaryLightForModelDynEntPointer { get; init; }
    public IReadOnlyList<byte> PrimaryLightForModelDynEnt { get; init; } = [];
    public XPointer<GfxShadowGeometry[]> ShadowGeomPointer { get; init; }
    public IReadOnlyList<GfxShadowGeometry> ShadowGeom { get; init; } = [];
    public XPointer<GfxLightRegion[]> LightRegionPointer { get; init; }
    public IReadOnlyList<GfxLightRegion> LightRegions { get; init; } = [];

    public GfxWorldDpvsStatic Dpvs { get; init; } = new();
    public GfxWorldDpvsDynamic DpvsDyn { get; init; } = new();
    public uint Unknown26C { get; init; }
    public uint HeroOnlyLightCount { get; init; }
    public XPointer<GfxHeroOnlyLight[]> HeroOnlyLightsPointer { get; init; }
    public IReadOnlyList<GfxHeroOnlyLight> HeroOnlyLights { get; init; } = [];
    public uint Unknown278 { get; init; }
    public int UmbraGateCount { get; init; }
    public XPointer<byte[]> UmbraGateDataPointer { get; init; }
    public IReadOnlyList<byte> UmbraGateData { get; init; } = [];
    public XPointer<byte[]> UmbraGateData2Pointer { get; init; }
    public IReadOnlyList<byte> UmbraGateData2 { get; init; } = [];
}

public sealed class GfxSky
{
    public const int SerializedSize = 0x10;

    public int SkySurfCount { get; init; }
    public XPointer<int[]> SkyStartSurfsPointer { get; init; }
    public IReadOnlyList<int> SkyStartSurfs { get; init; } = [];
    public XPointer<GfxImageAsset> SkyImagePointer { get; init; }
    public GfxImageAsset? SkyImage { get; init; }
    public int SkySamplerState { get; init; }
}

public sealed class GfxWorldDpvsPlanes
{
    public const int SerializedSize = 0x10;

    public int CellCount { get; init; }
    public XPointer<DpvsPlane[]> PlanesPointer { get; init; }
    public IReadOnlyList<DpvsPlane> Planes { get; init; } = [];
    public XPointer<ushort[]> NodesPointer { get; init; }
    public IReadOnlyList<ushort> Nodes { get; init; } = [];
    public XPointer<uint[]> SceneEntCellBitsPointer { get; init; }
    public IReadOnlyList<uint> SceneEntCellBits { get; init; } = [];
}

public sealed record DpvsPlane(
    float NormalX,
    float NormalY,
    float NormalZ,
    float Distance,
    byte Type,
    byte SignBits,
    ushort Pad12)
{
    public const int SerializedSize = 0x14;
}

public sealed record GfxCellTreeCount(uint AabbTreeCount)
{
    public const int SerializedSize = 0x04;
}

public sealed class GfxCellTree
{
    public const int SerializedSize = 0x04;

    public XPointer<GfxAabbTree[]> AabbTreesPointer { get; init; }
    public IReadOnlyList<GfxAabbTree> AabbTrees { get; init; } = [];
}

public sealed class GfxAabbTree
{
    public const int SerializedSize = 0x28;

    public IReadOnlyList<float> Mins { get; init; } = [];
    public IReadOnlyList<float> Maxs { get; init; } = [];
    public ushort ChildCount { get; init; }
    public ushort SurfaceCount { get; init; }
    public ushort StartSurfIndex { get; init; }
    public ushort SModelIndexCount { get; init; }
    public XPointer<ushort[]> SModelIndexesPointer { get; init; }
    public IReadOnlyList<ushort> SModelIndexes { get; init; } = [];
    public int ChildrenOffset { get; init; }
}

public sealed class GfxCell
{
    public const int SerializedSize = 0x28;

    public IReadOnlyList<float> Mins { get; init; } = [];
    public IReadOnlyList<float> Maxs { get; init; } = [];
    public int PortalCount { get; init; }
    public XPointer<GfxPortal[]> PortalsPointer { get; init; }
    public IReadOnlyList<GfxPortal> Portals { get; init; } = [];
    public byte ReflectionProbeCount { get; init; }
    public IReadOnlyList<byte> Pad21 { get; init; } = [];
    public XPointer<byte[]> ReflectionProbesPointer { get; init; }
    public IReadOnlyList<byte> ReflectionProbes { get; init; } = [];
}

public sealed class GfxPortal
{
    public const int SerializedSize = 0x3C;

    public bool IsQueued { get; init; }
    public bool IsAncestor { get; init; }
    public byte RecursionDepth { get; init; }
    public byte HullPointCount { get; init; }
    public int HullPointsRuntimePointer { get; init; }
    public DpvsPlane Plane { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public XPointer<GfxPortalVertex[]> VerticesPointer { get; init; }
    public IReadOnlyList<GfxPortalVertex> Vertices { get; init; } = [];
    public IReadOnlyList<byte> Unknown20To21 { get; init; } = [];
    public byte VertexCount { get; init; }
    public byte Pad23 { get; init; }
    public IReadOnlyList<float> HullAxis { get; init; } = [];
}

public sealed record GfxPortalVertex(float X, float Y, float Z)
{
    public const int SerializedSize = 0x0C;
}

public sealed class GfxWorldDraw
{
    public const int SerializedSize = 0x54;

    public uint ReflectionProbeCount { get; init; }
    public XPointer<GfxImageAsset[]> ReflectionImagesPointer { get; init; }
    public IReadOnlyList<GfxImageAsset?> ReflectionImages { get; init; } = [];
    public XPointer<GfxReflectionProbe[]> ReflectionProbesPointer { get; init; }
    public IReadOnlyList<GfxReflectionProbe> ReflectionProbes { get; init; } = [];
    public XPointer<GfxTexture[]> ReflectionProbeTexturesPointer { get; init; }
    public IReadOnlyList<GfxTexture> ReflectionProbeTextures { get; init; } = [];
    public int LightmapCount { get; init; }
    public XPointer<GfxLightmapArray[]> LightmapsPointer { get; init; }
    public IReadOnlyList<GfxLightmapArray> Lightmaps { get; init; } = [];
    public XPointer<GfxTexture[]> LightmapPrimaryTexturesPointer { get; init; }
    public IReadOnlyList<GfxTexture> LightmapPrimaryTextures { get; init; } = [];
    public XPointer<GfxTexture[]> LightmapSecondaryTexturesPointer { get; init; }
    public IReadOnlyList<GfxTexture> LightmapSecondaryTextures { get; init; } = [];
    public XPointer<GfxImageAsset> SkyImagePointer { get; init; }
    public GfxImageAsset? SkyImage { get; init; }
    public XPointer<GfxImageAsset> OutdoorImagePointer { get; init; }
    public GfxImageAsset? OutdoorImage { get; init; }
    public uint VertexCount { get; init; }
    public GfxWorldVertexData VertexData { get; init; } = new();
    public uint VertexLayerDataSize { get; init; }
    public GfxWorldVertexLayerData VertexLayerData { get; init; } = new();
    public int IndexCount { get; init; }
    public XPointer<ushort[]> IndicesPointer { get; init; }
    public IReadOnlyList<ushort> Indices { get; init; } = [];
    public int IndexBufferRaw { get; init; }
}

public sealed record GfxReflectionProbe(float OffsetX, float OffsetY, float OffsetZ)
{
    public const int SerializedSize = 0x0C;
}

public sealed record GfxTexture(IReadOnlyList<uint> Words)
{
    public const int SerializedSize = 0x18;
}

public sealed class GfxLightmapArray
{
    public const int SerializedSize = 0x08;

    public XPointer<GfxImageAsset> PrimaryPointer { get; init; }
    public GfxImageAsset? Primary { get; init; }
    public XPointer<GfxImageAsset> SecondaryPointer { get; init; }
    public GfxImageAsset? Secondary { get; init; }
}

public sealed class GfxWorldVertexData
{
    public const int SerializedSize = 0x0C;

    public XPointer<byte[]> VerticesPointer { get; init; }
    public IReadOnlyList<byte> PackedVertices { get; init; } = [];
    public int WorldVbHandle { get; init; }
    public int WorldVbOffset { get; init; }
}

public sealed class GfxWorldVertexLayerData
{
    public const int SerializedSize = 0x0C;

    public XPointer<byte[]> DataPointer { get; init; }
    public IReadOnlyList<byte> PackedLayerData { get; init; } = [];
    public int LayerVbHandle { get; init; }
    public int LayerVbOffset { get; init; }
}

public sealed class GfxLightGrid
{
    public const int SerializedSize = 0x38;

    public uint HasLightRegions { get; init; }
    public uint SunPrimaryLightIndex { get; init; }
    public IReadOnlyList<ushort> Mins { get; init; } = [];
    public IReadOnlyList<ushort> Maxs { get; init; } = [];
    public uint RowAxis { get; init; }
    public uint ColAxis { get; init; }
    public XPointer<ushort[]> RowDataStartPointer { get; init; }
    public IReadOnlyList<ushort> RowDataStart { get; init; } = [];
    public uint RawRowDataSize { get; init; }
    public XPointer<byte[]> RawRowDataPointer { get; init; }
    public IReadOnlyList<byte> RawRowData { get; init; } = [];
    public uint EntryCount { get; init; }
    public XPointer<GfxLightGridEntry[]> EntriesPointer { get; init; }
    public IReadOnlyList<GfxLightGridEntry> Entries { get; init; } = [];
    public uint ColorCount { get; init; }
    public XPointer<GfxLightGridColors[]> ColorsPointer { get; init; }
    public IReadOnlyList<GfxLightGridColors> Colors { get; init; } = [];
}

public sealed record GfxLightGridEntry(ushort ColorsIndex, byte PrimaryLightIndex, byte NeedsTrace)
{
    public const int SerializedSize = 0x04;
}

public sealed record GfxLightGridColors(IReadOnlyList<byte> RgbBytes)
{
    public const int SerializedSize = 0xA8;
}

public sealed class GfxBrushModel
{
    public const int SerializedSize = 0x38;

    public IReadOnlyList<float> WritableMins { get; init; } = [];
    public IReadOnlyList<float> WritableMaxs { get; init; } = [];
    public IReadOnlyList<float> BoundsMins { get; init; } = [];
    public IReadOnlyList<float> BoundsMaxs { get; init; } = [];
    public uint SurfaceCount { get; init; }
    public uint StartSurfIndex { get; init; }
}

public sealed class MaterialMemory
{
    public const int SerializedSize = 0x08;

    public XPointer<MaterialAsset> MaterialPointer { get; init; }
    public MaterialAsset? Material { get; init; }
    public int Memory { get; init; }
}

public sealed class Sunflare
{
    public const int SerializedSize = 0x60;

    public uint HasValidData { get; init; }
    public XPointer<MaterialAsset> SpriteMaterialPointer { get; init; }
    public MaterialAsset? SpriteMaterial { get; init; }
    public XPointer<MaterialAsset> FlareMaterialPointer { get; init; }
    public MaterialAsset? FlareMaterial { get; init; }
    public float SpriteSize { get; init; }
    public float FlareMinSize { get; init; }
    public float FlareMinDot { get; init; }
    public float FlareMaxSize { get; init; }
    public float FlareMaxDot { get; init; }
    public float FlareMaxAlpha { get; init; }
    public int FlareFadeInTime { get; init; }
    public int FlareFadeOutTime { get; init; }
    public float BlindMinDot { get; init; }
    public float BlindMaxDot { get; init; }
    public float BlindMaxDarken { get; init; }
    public int BlindFadeInTime { get; init; }
    public int BlindFadeOutTime { get; init; }
    public float GlareMinDot { get; init; }
    public float GlareMaxDot { get; init; }
    public float GlareMaxLighten { get; init; }
    public int GlareFadeInTime { get; init; }
    public int GlareFadeOutTime { get; init; }
    public IReadOnlyList<float> SunFxPosition { get; init; } = [];
}

public sealed record GfxSceneDynModel(ushort Lod, ushort SurfId, ushort DynEntId)
{
    public const int SerializedSize = 0x06;
}

public sealed record GfxSceneDynBrush(ushort SurfId, ushort DynEntId)
{
    public const int SerializedSize = 0x04;
}

public sealed class GfxShadowGeometry
{
    public const int SerializedSize = 0x0C;

    public ushort SurfaceCount { get; init; }
    public ushort SModelCount { get; init; }
    public XPointer<ushort[]> SortedSurfIndexPointer { get; init; }
    public IReadOnlyList<ushort> SortedSurfIndex { get; init; } = [];
    public XPointer<ushort[]> SModelIndexPointer { get; init; }
    public IReadOnlyList<ushort> SModelIndex { get; init; } = [];
}

public sealed class GfxLightRegion
{
    public const int SerializedSize = 0x08;

    public int HullCount { get; init; }
    public XPointer<GfxLightRegionHull[]> HullsPointer { get; init; }
    public IReadOnlyList<GfxLightRegionHull> Hulls { get; init; } = [];
}

public sealed class GfxLightRegionHull
{
    public const int SerializedSize = 0x50;

    public IReadOnlyList<float> KdopMidPoint { get; init; } = [];
    public IReadOnlyList<float> KdopHalfSize { get; init; } = [];
    public uint AxisCount { get; init; }
    public XPointer<GfxLightRegionAxis[]> AxesPointer { get; init; }
    public IReadOnlyList<GfxLightRegionAxis> Axes { get; init; } = [];
}

public sealed class GfxLightRegionAxis
{
    public const int SerializedSize = 0x14;

    public IReadOnlyList<float> Dir { get; init; } = [];
    public float MidPoint { get; init; }
    public float HalfSize { get; init; }
}

public sealed class GfxWorldDpvsStatic
{
    public const int SerializedSize = 0x68;

    public uint SModelCount { get; init; }
    public uint StaticSurfaceCount { get; init; }
    public uint LitSurfsBegin { get; init; }
    public uint LitSurfsEnd { get; init; }
    public IReadOnlyList<uint> VisibilityCounts { get; init; } = [];
    public IReadOnlyList<XPointer<uint[]>> SModelVisDataPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<uint>> SModelVisData { get; init; } = [];
    public IReadOnlyList<XPointer<uint[]>> SurfaceVisDataPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<uint>> SurfaceVisData { get; init; } = [];
    public XPointer<ushort[]> SortedSurfIndexPointer { get; init; }
    public IReadOnlyList<ushort> SortedSurfIndex { get; init; } = [];
    public XPointer<GfxStaticModelInst[]> SModelInstsPointer { get; init; }
    public IReadOnlyList<GfxStaticModelInst> SModelInsts { get; init; } = [];
    public XPointer<GfxSurface[]> SurfacesPointer { get; init; }
    public IReadOnlyList<GfxSurface> Surfaces { get; init; } = [];
    public XPointer<GfxCullGroup[]> CullGroupsPointer { get; init; }
    public IReadOnlyList<GfxCullGroup> CullGroups { get; init; } = [];
    public XPointer<GfxStaticModelDrawInst[]> SModelDrawInstsPointer { get; init; }
    public IReadOnlyList<GfxStaticModelDrawInst> SModelDrawInsts { get; init; } = [];
    public XPointer<GfxMapDrawSurf[]> SurfaceMaterialsPointer { get; init; }
    public IReadOnlyList<GfxMapDrawSurf> SurfaceMaterials { get; init; } = [];
    public XPointer<uint[]> SurfaceCastsSunShadowPointer { get; init; }
    public IReadOnlyList<uint> SurfaceCastsSunShadow { get; init; } = [];
    public uint UsageCount { get; init; }
}

public sealed class GfxStaticModelInst
{
    public const int SerializedSize = 0x24;

    public IReadOnlyList<float> Mins { get; init; } = [];
    public IReadOnlyList<float> Maxs { get; init; } = [];
    public uint GroundLighting { get; init; }
    public IReadOnlyList<byte> Unknown1CTo23 { get; init; } = [];
}

public sealed class GfxSurface
{
    public const int SerializedSize = 0x1C;

    public SrfTriangles Triangles { get; init; } = new();
    public XPointer<MaterialAsset> MaterialPointer { get; init; }
    public MaterialAsset? Material { get; init; }
    public byte LightmapIndex { get; init; }
    public byte ReflectionProbeIndex { get; init; }
    public byte PrimaryLightIndex { get; init; }
    public byte CastsSunShadow { get; init; }
}

public readonly record struct GfxMapDrawSurf(ulong Packed)
{
    public const int SerializedSize = 0x08;
}

public sealed class SrfTriangles
{
    public const int SerializedSize = 0x14;

    public int VertexLayerData { get; init; }
    public int FirstVertex { get; init; }
    public uint Unknown08 { get; init; }
    public ushort VertexCount { get; init; }
    public ushort TriCount { get; init; }
    public int BaseIndex { get; init; }
}

public sealed class GfxCullGroup
{
    public const int SerializedSize = 0x20;

    public IReadOnlyList<float> Mins { get; init; } = [];
    public IReadOnlyList<float> Maxs { get; init; } = [];
    public int SurfaceCount { get; init; }
    public int StartSurfIndex { get; init; }
}

public sealed class GfxStaticModelDrawInst
{
    public const int SerializedSize = 0x2C;

    public GfxPackedPlacement Placement { get; init; } = new();
    public XPointer<XModelAsset> ModelPointer { get; init; }
    public XModelAsset? Model { get; init; }
    public float CullDist { get; init; }
    public byte ReflectionProbeIndex { get; init; }
    public byte PrimaryLightIndex { get; init; }
    public ushort LightingHandle { get; init; }
    public byte Flags { get; init; }
    public IReadOnlyList<byte> Pad2B { get; init; } = [];
}

public sealed class GfxPackedPlacement
{
    public const int SerializedSize = 0x1C;

    public IReadOnlyList<float> Origin { get; init; } = [];
    public IReadOnlyList<uint> PackedAxis { get; init; } = [];
    public float Scale { get; init; }
}

public sealed class GfxWorldDpvsDynamic
{
    public const int SerializedSize = 0x30;

    public IReadOnlyList<uint> DynEntClientWordCount { get; init; } = [];
    public IReadOnlyList<uint> DynEntClientCount { get; init; } = [];
    public IReadOnlyList<XPointer<uint[]>> DynEntCellBitsPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<uint>> DynEntCellBits { get; init; } = [];
    public IReadOnlyList<XPointer<byte[]>> DynEntVisDataPointers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<byte>> DynEntVisData { get; init; } = [];
}

public sealed record GfxHeroOnlyLight(IReadOnlyList<byte> Bytes)
{
    public const int SerializedSize = 0x38;
}
