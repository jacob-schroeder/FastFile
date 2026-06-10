using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;
using XModelAsset = FastFile.Models.Assets.XModels.XModel;

namespace FastFile.Models.Assets.Effects;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x20)]
public class FxEffectDef() : BaseAsset(XAssetType.Fx)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct Pointer
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int Flags { get; set; }

    [XField(Offset = 0x08)]
    public int TotalSize { get; set; }

    [XField(Offset = 0x0C)]
    public int MsecLoopingLife { get; set; }

    [XField(Offset = 0x10)]
    public int ElemDefCountLooping { get; set; }

    [XField(Offset = 0x14)]
    public int ElemDefCountOneShot { get; set; }

    [XField(Offset = 0x18)]
    public int ElemDefCountEmission { get; set; }

    [XField(Offset = 0x1C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(ElemDefCount))]
    public XPointer<FxElemDef[]> ElemDefs { get; set; } // Direct Pointer

    public int ElemDefCount => ElemDefCountLooping + ElemDefCountOneShot + ElemDefCountEmission;

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
    public XPointer<FxElemVelStateSample[]> VelSamples { get; set; } // Direct Pointer
    public XPointer<FxElemVisStateSample[]> VisSamples { get; set; } // Direct Pointer
    public XPointer<FxElemVisual[]> Visuals { get; set; } // Direct Pointer
    public Bounds CollBounds { get; set; }
    public XPointer<FxEffectDefRef> EffectOnImpact { get; set; } // Direct Pointer
    public XPointer<FxEffectDefRef> EffectOnDeath { get; set; } // Direct Pointer
    public XPointer<FxEffectDefRef> EffectEmitted { get; set; } // Direct Pointer
    public FxFloatRange EmitDist { get; set; }
    public FxFloatRange EmitDistVariance { get; set; }
    public XPointer<FxElemExtendedDef> Extended { get; set; } // Direct Pointer
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
    public XPointer<FxEffectDef> Handle { get; set; } // Unknown
    public XPointer<string> Name { get; set; } // Direct
}

public sealed class FxElemVisual
{
    public XPointer<MaterialAsset> Material { get; set; } // Alias
    public XPointer<XModelAsset> Model { get; set; } // Alias
    public FxEffectDefRef EffectDef { get; set; }
    public XPointer<string> SoundName { get; set; } // Direct
    public XPointer<FxUnknownVisual> Anonymous { get; set; } // Direct
    public XPointer<MaterialAsset> DecalMaterial0 { get; set; } // Alias
    public XPointer<MaterialAsset> DecalMaterial1 { get; set; } // Alias
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
    public XPointer<FxTrailVertex[]> Verts { get; set; } // Direct 
    public int IndCount { get; set; }
    public XPointer<ushort[]> Inds { get; set; } // Direct
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
