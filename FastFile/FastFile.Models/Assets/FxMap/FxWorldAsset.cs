using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Vehicle;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.FxMap;

public sealed class FxWorldAsset : BaseAsset
{
    public const int SerializedSize = 0x74;

    public XAssetType Type => XAssetType.FxMap;

    // 0x00: XString. PS3 FxWorld body stores root+0x00 into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }

    // 0x04: embedded FxGlassSystem. PS3 sets varFxGlassSystem to root+0x04 and calls Load_FxGlassSystem.
    public FxGlassSystem GlassSystem { get; init; } = new();
}

public sealed class FxGlassSystem
{
    public const int SerializedSize = 0x70;

    public int Offset { get; init; }
    public int Time { get; init; }
    public int PrevTime { get; init; }
    public uint DefCount { get; init; }
    public uint PieceLimit { get; init; }
    public uint PieceWordCount { get; init; }
    public uint InitPieceCount { get; init; }
    public uint CellCount { get; init; }
    public uint ActivePieceCount { get; init; }
    public uint FirstFreePiece { get; init; }
    public uint GeoDataLimit { get; init; }
    public uint GeoDataCount { get; init; }
    public uint InitGeoDataCount { get; init; }
    public XPointer<FxGlassDef[]> DefsPointer { get; init; }
    public IReadOnlyList<FxGlassDef> Defs { get; init; } = [];
    public XPointer<FxGlassPiecePlace[]> PiecePlacesPointer { get; init; }
    public IReadOnlyList<FxGlassPiecePlace> PiecePlaces { get; init; } = [];
    public XPointer<FxGlassPieceState[]> PieceStatesPointer { get; init; }
    public IReadOnlyList<FxGlassPieceState> PieceStates { get; init; } = [];
    public XPointer<FxGlassPieceDynamics[]> PieceDynamicsPointer { get; init; }
    public IReadOnlyList<FxGlassPieceDynamics> PieceDynamics { get; init; } = [];
    public XPointer<FxGlassGeometryData[]> GeoDataPointer { get; init; }
    public IReadOnlyList<FxGlassGeometryData> GeoData { get; init; } = [];
    public XPointer<uint[]> IsInUsePointer { get; init; }
    public IReadOnlyList<uint> IsInUse { get; init; } = [];
    public XPointer<uint[]> CellBitsPointer { get; init; }
    public IReadOnlyList<uint> CellBits { get; init; } = [];
    public XPointer<byte[]> VisDataPointer { get; init; }
    public IReadOnlyList<byte> VisData { get; init; } = [];
    public XPointer<FxVec3[]> LinkOrgPointer { get; init; }
    public IReadOnlyList<FxVec3> LinkOrg { get; init; } = [];
    public XPointer<float[]> HalfThicknessPointer { get; init; }
    public IReadOnlyList<float> HalfThickness { get; init; } = [];
    public XPointer<ushort[]> LightingHandlesPointer { get; init; }
    public IReadOnlyList<ushort> LightingHandles { get; init; } = [];
    public XPointer<FxGlassInitPieceState[]> InitPieceStatesPointer { get; init; }
    public IReadOnlyList<FxGlassInitPieceState> InitPieceStates { get; init; } = [];
    public XPointer<FxGlassGeometryData[]> InitGeoDataPointer { get; init; }
    public IReadOnlyList<FxGlassGeometryData> InitGeoData { get; init; } = [];
    public byte NeedToCompactData { get; init; }
    public byte InitCount { get; init; }
    public ushort Pad66 { get; init; }
    public float EffectChanceAccum { get; init; }
    public int LastPieceDeletionTime { get; init; }
}

public sealed class FxGlassDef
{
    public const int SerializedSize = 0x2C;

    public int Offset { get; init; }
    public float HalfThickness { get; init; }
    public IReadOnlyList<FxVec2> TexVecs { get; init; } = [];
    public uint Color { get; init; }
    public XPointer<MaterialAsset> MaterialPointer { get; init; }
    public MaterialAsset? Material { get; init; }
    public XPointer<MaterialAsset> MaterialShatteredPointer { get; init; }
    public MaterialAsset? MaterialShattered { get; init; }
    public XPointer<PhysPresetAsset> PhysPresetPointer { get; init; }
    public PhysPresetAsset? PhysPreset { get; init; }
    public float InvHighMipRadius { get; init; }
    public float ShatteredInvHighMipRadius { get; init; }
}

public sealed record FxGlassPiecePlace(FxSpatialFrame Frame, float Radius, uint NextFree)
{
    public const int SerializedSize = 0x20;
}

public sealed class FxGlassPieceState
{
    public const int SerializedSize = 0x20;

    public FxVec2 TexCoordOrigin { get; init; }
    public uint SupportMask { get; init; }
    public ushort InitIndex { get; init; }
    public ushort GeoDataStart { get; init; }
    public byte DefIndex { get; init; }
    public IReadOnlyList<byte> Pad11 { get; init; } = [];
    public byte VertCount { get; init; }
    public byte HoleDataCount { get; init; }
    public byte CrackDataCount { get; init; }
    public byte FanDataCount { get; init; }
    public ushort Flags { get; init; }
    public float AreaX2 { get; init; }
}

public sealed class FxGlassInitPieceState
{
    public const int SerializedSize = 0x34;

    public FxSpatialFrame Frame { get; init; }
    public float Radius { get; init; }
    public FxVec2 TexCoordOrigin { get; init; }
    public uint SupportMask { get; init; }
    public float AreaX2 { get; init; }
    public byte DefIndex { get; init; }
    public byte VertCount { get; init; }
    public byte FanDataCount { get; init; }
    public byte Pad33 { get; init; }
}

public sealed record FxGlassPieceDynamics(
    int FallTime,
    int PhysObjId,
    int PhysJointId,
    FxVec3 Vel,
    FxVec3 AVel)
{
    public const int SerializedSize = 0x24;
}

public readonly record struct FxGlassGeometryData(uint PackedValue)
{
    public const int SerializedSize = 0x04;

    // Xbox-correlated FxGlassGeometryData is a packed union used as vertex,
    // hole, crack, and fan geometry data by the glass draw/crack consumers.
}

public readonly record struct FxSpatialFrame(FxQuat Quat, FxVec3 Origin)
{
    public const int SerializedSize = 0x1C;
}

public readonly record struct FxQuat(float X, float Y, float Z, float W);
public readonly record struct FxVec3(float X, float Y, float Z);
public readonly record struct FxVec2(float X, float Y);
