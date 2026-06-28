using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.XModel;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Fx;

public sealed class FxEffectDefAsset : BaseAsset
{
    public const int SerializedSize = 0x20;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int Flags { get; init; }
    public int TotalSize { get; init; }
    public int MsecLoopingLife { get; init; }
    public int ElemDefCountLooping { get; init; }
    public int ElemDefCountOneShot { get; init; }
    public int ElemDefCountEmission { get; init; }
    public int ElemDefCount => checked(ElemDefCountLooping + ElemDefCountOneShot + ElemDefCountEmission);
    public XPointer<FxElemDef[]> ElemDefsPointer { get; init; }
    public IReadOnlyList<FxElemDef> ElemDefs { get; init; } = [];
}

public sealed class FxElemDef
{
    public const int SerializedSize = 0xFC;

    public int Offset { get; init; }
    public int Flags { get; init; }
    public FxSpawnDef Spawn { get; init; }
    public FxFloatRange SpawnRange { get; init; }
    public FxFloatRange FadeInRange { get; init; }
    public FxFloatRange FadeOutRange { get; init; }
    public float SpawnFrustumCullRadius { get; init; }
    public FxIntRange SpawnDelayMsec { get; init; }
    public FxIntRange LifeSpanMsec { get; init; }
    public IReadOnlyList<FxFloatRange> SpawnOrigin { get; init; } = [];
    public FxFloatRange SpawnOffsetRadius { get; init; }
    public FxFloatRange SpawnOffsetHeight { get; init; }
    public IReadOnlyList<FxFloatRange> SpawnAngles { get; init; } = [];
    public IReadOnlyList<FxFloatRange> AngularVelocity { get; init; } = [];
    public FxFloatRange InitialRotation { get; init; }
    public FxFloatRange Gravity { get; init; }
    public FxFloatRange ReflectionFactor { get; init; }
    public FxElemAtlas Atlas { get; init; }
    public FxElemType ElemType { get; init; }
    public byte VisualCount { get; init; }
    public byte VelIntervalCount { get; init; }
    public byte VisStateIntervalCount { get; init; }
    public int VelSampleCount => VelIntervalCount + 1;
    public int VisStateSampleCount => VisStateIntervalCount + 1;
    public XPointer<FxElemVelStateSample[]> VelSamplesPointer { get; init; }
    public IReadOnlyList<FxElemVelStateSample> VelSamples { get; init; } = [];
    public XPointer<FxElemVisStateSample[]> VisSamplesPointer { get; init; }
    public IReadOnlyList<FxElemVisStateSample> VisSamples { get; init; } = [];
    public FxElemDefVisuals Visuals { get; init; } = new();
    public IReadOnlyList<FxElemDefVisuals> VisualArray { get; init; } = [];
    public IReadOnlyList<FxElemMarkVisuals> MarkVisualArray { get; init; } = [];
    public Bounds CollBounds { get; init; }
    public FxEffectDefRef EffectOnImpact { get; init; } = new();
    public FxEffectDefRef EffectOnDeath { get; init; } = new();
    public FxEffectDefRef EffectEmitted { get; init; } = new();
    public FxFloatRange EmitDist { get; init; }
    public FxFloatRange EmitDistVariance { get; init; }
    public XPointer<FxElemExtendedDef> ExtendedPointer { get; init; }
    public FxElemExtendedDef? Extended { get; init; }
    public byte SortOrder { get; init; }
    public byte LightingFrac { get; init; }
    public byte UseItemClip { get; init; }
    public byte FadeInfo { get; init; }
}

public sealed record FxIntRange(int Base, int Amplitude);
public sealed record FxSpawnDef(int LoopingIntervalMsec, int Count);
public sealed record FxFloatRange(float Base, float Amplitude);
public sealed record Vec3(float X, float Y, float Z);
public sealed record Bounds(Vec3 MidPoint, Vec3 HalfSize);

public sealed record FxElemAtlas(
    byte Behavior,
    byte Index,
    byte Fps,
    byte LoopCount,
    byte ColIndexBits,
    byte RowIndexBits,
    short EntryCount);

public sealed record FxElemVec3Range(Vec3 Base, Vec3 Amplitude);
public sealed record FxElemVelStateInFrame(FxElemVec3Range Velocity, FxElemVec3Range TotalDelta);
public sealed record FxElemVelStateSample(FxElemVelStateInFrame Local, FxElemVelStateInFrame World);
public sealed record FxElemColor(byte R, byte G, byte B, byte A);
public sealed record FxElemVisualState(FxElemColor Color, float RotationDelta, float RotationTotal, float Size0, float Size1, float Scale);
public sealed record FxElemVisStateSample(FxElemVisualState Base, FxElemVisualState Amplitude);

public sealed class FxEffectDefRef
{
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
}

public enum FxElemType : byte
{
    SpriteBillboard = 0,
    SpriteOriented = 1,
    Tail = 2,
    Trail = 3,
    Cloud = 4,
    SparkCloud = 5,
    SparkFountain = 6,
    Model = 7,
    OmniLight = 8,
    SpotLight = 9,
    Sound = 10,
    Decal = 11,
    Runner = 12
}

public sealed class FxElemDefVisuals
{
    public const int SerializedSize = 0x04;

    public int Offset { get; init; }
    public XPointer<object> Raw { get; init; }
    public XPointer<MaterialAsset>? MaterialPointer { get; init; }
    public MaterialAsset? Material { get; init; }
    public XPointer<XModelAsset>? ModelPointer { get; init; }
    public XModelAsset? Model { get; init; }
    public FxEffectDefRef? EffectDef { get; init; }
    public XPointer<string>? SoundNamePointer { get; init; }
    public string? SoundName { get; init; }
}

public sealed class FxElemMarkVisuals
{
    public const int SerializedSize = 0x08;

    public int Offset { get; init; }
    public XPointer<MaterialAsset> Material0Pointer { get; init; }
    public MaterialAsset? Material0 { get; init; }
    public XPointer<MaterialAsset> Material1Pointer { get; init; }
    public MaterialAsset? Material1 { get; init; }
}

public sealed record FxTrailVertex(float Pos0, float Pos1, float Normal0, float Normal1, float TexCoord);

public sealed class FxTrailDef
{
    public const int SerializedSize = 0x24;
    public const int VertexSerializedSize = 0x14;

    public int ScrollTimeMsec { get; init; }
    public int RepeatDist { get; init; }
    public float InvSplitDist { get; init; }
    public float InvSplitArcDist { get; init; }
    public float InvSplitTime { get; init; }
    public int VertCount { get; init; }
    public XPointer<FxTrailVertex[]> VertsPointer { get; init; }
    public IReadOnlyList<FxTrailVertex> Verts { get; init; } = [];
    public int IndCount { get; init; }
    public XPointer<ushort[]> IndsPointer { get; init; }
    public IReadOnlyList<ushort> Inds { get; init; } = [];
}

public sealed record FxSparkFountainDef(
    float Gravity,
    float BounceFrac,
    float BounceRand,
    float SparkSpacing,
    float SparkLength,
    int SparkCount,
    float LoopTime,
    float VelMin,
    float VelMax,
    float VelConeFrac,
    float RestSpeed,
    float BoostTime,
    float BoostFactor)
{
    public const int SerializedSize = 0x34;
}

public sealed class FxElemExtendedDef
{
    public FxElemExtendedDefKind Kind { get; init; }
    public FxTrailDef? TrailDef { get; init; }
    public FxSparkFountainDef? SparkFountainDef { get; init; }
    public byte? UnknownValue { get; init; }
}

public enum FxElemExtendedDefKind
{
    None,
    Trail,
    SparkFountain,
    Unknown
}
