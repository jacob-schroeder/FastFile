using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;
using XModelAsset = FastFile.Models.Assets.XModels.XModel;

namespace FastFile.Models.Assets.Effects;

public class FxEffectDef() : BaseAsset(XAssetType.Fx)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public int Flags { get; set; }
    public int TotalSize { get; set; }
    public int MsecLoopingLife { get; set; }
    public int ElemDefCountLooping { get; set; }
    public int ElemDefCountOneShot { get; set; }
    public int ElemDefCountEmission { get; set; }
    public ZonePointer<FxElemDef[]> ElemDefs { get; set; }

    public override string? GetDisplayName => Name;
}

public sealed class FxElemDef
{
    public int Flags { get; set; }
    public FxSpawnDef Spawn { get; set; }
    public FxFloatRange SpawnRange { get; set; }
    public FxFloatRange FadeInRange { get; set; }
    public FxFloatRange FadeOutRange { get; set; }
    public float SpawnFrustumCullRadius { get; set; }
    public FxIntRange SpawnDelayMsec { get; set; }
    public FxIntRange LifeSpanMsec { get; set; }
    public FxFloatRange[] SpawnOrigin { get; set; } = new FxFloatRange[3];
    public FxFloatRange SpawnOffsetRadius { get; set; }
    public FxFloatRange SpawnOffsetHeight { get; set; }
    public FxFloatRange[] SpawnAngles { get; set; } = new FxFloatRange[3];
    public FxFloatRange[] AngularVelocity { get; set; } = new FxFloatRange[3];
    public FxFloatRange InitialRotation { get; set; }
    public FxFloatRange Gravity { get; set; }
    public FxFloatRange ReflectionFactor { get; set; }
    public FxElemAtlas Atlas { get; set; }
    public byte ElemType { get; set; }
    public byte VisualCount { get; set; }
    public byte VelIntervalCount { get; set; }
    public byte VisStateIntervalCount { get; set; }
    public ZonePointer<FxElemVelStateSample[]> VelSamples { get; set; }
    public ZonePointer<FxElemVisStateSample[]> VisSamples { get; set; }
    public ZonePointer<FxElemVisual[]> Visuals { get; set; }
    public Bounds CollBounds { get; set; }
    public ZonePointer<FxEffectDefRef> EffectOnImpact { get; set; }
    public ZonePointer<FxEffectDefRef> EffectOnDeath { get; set; }
    public ZonePointer<FxEffectDefRef> EffectEmitted { get; set; }
    public FxFloatRange EmitDist { get; set; }
    public FxFloatRange EmitDistVariance { get; set; }
    public ZonePointer<FxElemExtendedDef> Extended { get; set; }
    public byte SortOrder { get; set; }
    public byte LightingFrac { get; set; }
    public byte UseItemClip { get; set; }
    public byte FadeInfo { get; set; }
}

public sealed class FxIntRange
{
    public int Base { get; set; }
    public int Amplitude { get; set; }
}

public sealed class FxSpawnDef
{
    public int LoopingIntervalMsec { get; set; }
    public int Count { get; set; }
}

public sealed class FxFloatRange
{
    public float Base { get; set; }
    public float Amplitude { get; set; }
}

public sealed class FxElemAtlas
{
    public byte Behavior { get; set; }
    public byte Index { get; set; }
    public byte Fps { get; set; }
    public byte LoopCount { get; set; }
    public byte ColIndexBits { get; set; }
    public byte RowIndexBits { get; set; }
    public short EntryCount { get; set; }
}

public sealed class FxElemVec3Range
{
    public Vec3 Base { get; set; }
    public Vec3 Amplitude { get; set; }
}

public sealed class FxElemVelStateInFrame
{
    public FxElemVec3Range Velocity { get; set; }
    public FxElemVec3Range TotalDelta { get; set; }
}

public sealed class FxElemVelStateSample
{
    public FxElemVelStateInFrame Local { get; set; }
    public FxElemVelStateInFrame World { get; set; }
}

public sealed class FxElemColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
}

public sealed class FxElemVisualState
{
    public FxElemColor Color { get; set; }
    public float RotationDelta { get; set; }
    public float RotationTotal { get; set; }
    public float Size0 { get; set; }
    public float Size1 { get; set; }
    public float Scale { get; set; }
}

public sealed class FxElemVisStateSample
{
    public FxElemVisualState Base { get; set; }
    public FxElemVisualState Amplitude { get; set; }
}

public sealed class FxEffectDefRef
{
    public ZonePointer<FxEffectDef> Handle { get; set; }
    public ZonePointer<string> Name { get; set; }
}

public sealed class FxElemVisual
{
    public ZonePointer<MaterialAsset> Material { get; set; }
    public ZonePointer<XModelAsset> Model { get; set; }
    public FxEffectDefRef EffectDef { get; set; }
    public ZonePointer<string> SoundName { get; set; }
    public ZonePointer<FxUnknownVisual> Anonymous { get; set; }
    public ZonePointer<MaterialAsset> DecalMaterial0 { get; set; }
    public ZonePointer<MaterialAsset> DecalMaterial1 { get; set; }
}

public sealed class FxUnknownVisual
{
}

public sealed class FxTrailVertex
{
    public float Pos0 { get; set; }
    public float Pos1 { get; set; }
    public float Normal0 { get; set; }
    public float Normal1 { get; set; }
    public float TexCoord { get; set; }
}

public sealed class FxTrailDef
{
    public int ScrollTimeMsec { get; set; }
    public int RepeatDist { get; set; }
    public float InvSplitDist { get; set; }
    public float InvSplitArcDist { get; set; }
    public float InvSplitTime { get; set; }
    public int VertCount { get; set; }
    public ZonePointer<FxTrailVertex[]> Verts { get; set; }
    public int IndCount { get; set; }
    public ZonePointer<ushort[]> Inds { get; set; }
}

public sealed class FxSparkFountainDef
{
    public float Gravity { get; set; }
    public float BounceFrac { get; set; }
    public float BounceRand { get; set; }
    public float SparkSpacing { get; set; }
    public float SparkLength { get; set; }
    public int SparkCount { get; set; }
    public float LoopTime { get; set; }
    public float VelMin { get; set; }
    public float VelMax { get; set; }
    public float VelConeFrac { get; set; }
    public float RestSpeed { get; set; }
    public float BoostTime { get; set; }
    public float BoostFactor { get; set; }
}

public sealed class FxElemExtendedDef
{
    public FxTrailDef TrailDef { get; set; }
    public FxSparkFountainDef SparkFountainDef { get; set; }
    public byte UnknownDef { get; set; }
}
