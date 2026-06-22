using FastFile.ModelsOLD.Assets.XModels;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Utils;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;
using XModelAsset = FastFile.ModelsOLD.Assets.XModels.XModel;

namespace FastFile.ModelsOLD.Assets.Effects;

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
        UseCurrentStream = true,
        CountMember = nameof(ElemDefCount))]
    public XPointer<FxElemDef[]> ElemDefs { get; set; } // Direct Pointer

    public int ElemDefCount => ElemDefCountLooping + ElemDefCountOneShot + ElemDefCountEmission;

    public override string? GetDisplayName => Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0xFC)]
[XEbootEvidence(
    "0x112e20",
    "eboot/graphs/fx_loader_map.md",
    Detail = "PS3 FxElemDef body reads 0xFC bytes, then resolves +0xB4 velocity samples, +0xB8 visual-state samples, +0xBC visuals union, +0xD8/+0xDC/+0xE0 effect refs, and +0xF4 extended union.")]
public sealed class FxElemDef
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    public int Flags { get; set; }

    [XField(Offset = 0x04)]
    public FxSpawnDef Spawn { get; set; }

    [XField(Offset = 0x0C)]
    public FxFloatRange SpawnRange { get; set; }

    [XField(Offset = 0x14)]
    public FxFloatRange FadeInRange { get; set; }

    [XField(Offset = 0x1C)]
    public FxFloatRange FadeOutRange { get; set; }

    [XField(Offset = 0x24)]
    public float SpawnFrustumCullRadius { get; set; }

    [XField(Offset = 0x28)]
    public FxIntRange SpawnDelayMsec { get; set; }

    [XField(Offset = 0x30)]
    public FxIntRange LifeSpanMsec { get; set; }

    [XField(Offset = 0x38)]
    public FxFloatRange[] SpawnOrigin { get; set; } = new FxFloatRange[3];

    [XField(Offset = 0x50)]
    public FxFloatRange SpawnOffsetRadius { get; set; }

    [XField(Offset = 0x58)]
    public FxFloatRange SpawnOffsetHeight { get; set; }

    [XField(Offset = 0x60)]
    public FxFloatRange[] SpawnAngles { get; set; } = new FxFloatRange[3];

    [XField(Offset = 0x78)]
    public FxFloatRange[] AngularVelocity { get; set; } = new FxFloatRange[3];

    [XField(Offset = 0x90)]
    public FxFloatRange InitialRotation { get; set; }

    [XField(Offset = 0x98)]
    public FxFloatRange Gravity { get; set; }

    [XField(Offset = 0xA0)]
    public FxFloatRange ReflectionFactor { get; set; }

    [XField(Offset = 0xA8)]
    public FxElemAtlas Atlas { get; set; }

    [XField(Offset = 0xB0)]
    public FxElemType ElemType { get; set; }

    [XField(Offset = 0xB1)]
    public byte VisualCount { get; set; }

    [XField(Offset = 0xB2)]
    public byte VelIntervalCount { get; set; }

    [XField(Offset = 0xB3)]
    public byte VisStateIntervalCount { get; set; }

    [XField(Offset = 0xB4)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        CountMember = nameof(VelSampleCount))]
    public XPointer<FxElemVelStateSample[]> VelSamples { get; set; } // Direct Pointer

    [XField(Offset = 0xB8)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        CountMember = nameof(VisStateSampleCount))]
    public XPointer<FxElemVisStateSample[]> VisSamples { get; set; } // Direct Pointer

    [XField(Offset = 0xBC)]
    public FxElemDefVisuals Visuals { get; set; }

    [XField(Offset = 0xC0)]
    public Bounds CollBounds { get; set; }

    [XField(Offset = 0xD8)]
    public FxEffectDefRef EffectOnImpact { get; set; }

    [XField(Offset = 0xDC)]
    public FxEffectDefRef EffectOnDeath { get; set; }

    [XField(Offset = 0xE0)]
    public FxEffectDefRef EffectEmitted { get; set; }

    [XField(Offset = 0xE4)]
    public FxFloatRange EmitDist { get; set; }

    [XField(Offset = 0xEC)]
    public FxFloatRange EmitDistVariance { get; set; }

    [XField(Offset = 0xF4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<FxElemExtendedDef> Extended { get; set; } // Direct Pointer

    [XField(Offset = 0xF8)]
    public byte SortOrder { get; set; }

    [XField(Offset = 0xF9)]
    public byte LightingFrac { get; set; }

    [XField(Offset = 0xFA)]
    public byte UseItemClip { get; set; }

    [XField(Offset = 0xFB)]
    public byte FadeInfo { get; set; }

    public int VelSampleCount => VelIntervalCount + 1;
    public int VisStateSampleCount => VisStateIntervalCount + 1;
    public int VisualCountValue => VisualCount;
    public XPointer<FxElemDefVisuals[]>? VisualArray { get; set; }
    public XPointer<FxElemMarkVisuals[]>? MarkVisualArray { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class FxIntRange
{
    [XField(Offset = 0x00)]
    public int Base { get; set; }

    [XField(Offset = 0x04)]
    public int Amplitude { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class FxSpawnDef
{
    [XField(Offset = 0x00)]
    public int LoopingIntervalMsec { get; set; }

    [XField(Offset = 0x04)]
    public int Count { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class FxFloatRange
{
    [XField(Offset = 0x00)]
    public float Base { get; set; }

    [XField(Offset = 0x04)]
    public float Amplitude { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class FxElemAtlas
{
    [XField(Offset = 0x00)]
    public byte Behavior { get; set; }

    [XField(Offset = 0x01)]
    public byte Index { get; set; }

    [XField(Offset = 0x02)]
    public byte Fps { get; set; }

    [XField(Offset = 0x03)]
    public byte LoopCount { get; set; }

    [XField(Offset = 0x04)]
    public byte ColIndexBits { get; set; }

    [XField(Offset = 0x05)]
    public byte RowIndexBits { get; set; }

    [XField(Offset = 0x06)]
    public short EntryCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public sealed class FxElemVec3Range
{
    [XField(Offset = 0x00)]
    public Vec3 Base { get; set; }

    [XField(Offset = 0x0C)]
    public Vec3 Amplitude { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x30)]
public sealed class FxElemVelStateInFrame
{
    [XField(Offset = 0x00)]
    public FxElemVec3Range Velocity { get; set; }

    [XField(Offset = 0x18)]
    public FxElemVec3Range TotalDelta { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x60)]
public sealed class FxElemVelStateSample
{
    [XField(Offset = 0x00)]
    public FxElemVelStateInFrame Local { get; set; }

    [XField(Offset = 0x30)]
    public FxElemVelStateInFrame World { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class FxElemColor
{
    [XField(Offset = 0x00)]
    public byte R { get; set; }

    [XField(Offset = 0x01)]
    public byte G { get; set; }

    [XField(Offset = 0x02)]
    public byte B { get; set; }

    [XField(Offset = 0x03)]
    public byte A { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public sealed class FxElemVisualState
{
    [XField(Offset = 0x00)]
    public FxElemColor Color { get; set; }

    [XField(Offset = 0x04)]
    public float RotationDelta { get; set; }

    [XField(Offset = 0x08)]
    public float RotationTotal { get; set; }

    [XField(Offset = 0x0C)]
    public float Size0 { get; set; }

    [XField(Offset = 0x10)]
    public float Size1 { get; set; }

    [XField(Offset = 0x14)]
    public float Scale { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x30)]
public sealed class FxElemVisStateSample
{
    [XField(Offset = 0x00)]
    public FxElemVisualState Base { get; set; }

    [XField(Offset = 0x18)]
    public FxElemVisualState Amplitude { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class FxEffectDefRef
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct XString cell

    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
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

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class FxElemDefVisuals
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<object> Raw { get; set; }

    public XPointer<Material.Material>? Material { get; set; }
    public XPointer<XModel>? Model { get; set; }
    public FxEffectDefRef? EffectDef { get; set; }
    public XPointer<string>? SoundName { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class FxElemMarkVisuals
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.None)]
    public XPointer<Material.Material> Materials0 { get; set; } // Alias wrapper

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.None)]
    public XPointer<Material.Material> Materials1 { get; set; } // Alias wrapper
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x14)]
public sealed class FxTrailVertex
{
    [XField(Offset = 0x00)]
    public float Pos0 { get; set; }

    [XField(Offset = 0x04)]
    public float Pos1 { get; set; }

    [XField(Offset = 0x08)]
    public float Normal0 { get; set; }

    [XField(Offset = 0x0C)]
    public float Normal1 { get; set; }

    [XField(Offset = 0x10)]
    public float TexCoord { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x24)]
public sealed class FxTrailDef
{
    [XField(Offset = 0x00)]
    public int ScrollTimeMsec { get; set; }

    [XField(Offset = 0x04)]
    public int RepeatDist { get; set; }

    [XField(Offset = 0x08)]
    public float InvSplitDist { get; set; }

    [XField(Offset = 0x0C)]
    public float InvSplitArcDist { get; set; }

    [XField(Offset = 0x10)]
    public float InvSplitTime { get; set; }

    [XField(Offset = 0x14)]
    public int VertCount { get; set; }

    [XField(Offset = 0x18)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        CountMember = nameof(VertCount))]
    public XPointer<FxTrailVertex[]> Verts { get; set; } // Direct

    [XField(Offset = 0x1C)]
    public int IndCount { get; set; }

    [XField(Offset = 0x20)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(IndCount))]
    public XPointer<ushort[]> Inds { get; set; } // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x34)]
public sealed class FxSparkFountainDef
{
    [XField(Offset = 0x00)]
    public float Gravity { get; set; }

    [XField(Offset = 0x04)]
    public float BounceFrac { get; set; }

    [XField(Offset = 0x08)]
    public float BounceRand { get; set; }

    [XField(Offset = 0x0C)]
    public float SparkSpacing { get; set; }

    [XField(Offset = 0x10)]
    public float SparkLength { get; set; }

    [XField(Offset = 0x14)]
    public int SparkCount { get; set; }

    [XField(Offset = 0x18)]
    public float LoopTime { get; set; }

    [XField(Offset = 0x1C)]
    public float VelMin { get; set; }

    [XField(Offset = 0x20)]
    public float VelMax { get; set; }

    [XField(Offset = 0x24)]
    public float VelConeFrac { get; set; }

    [XField(Offset = 0x28)]
    public float RestSpeed { get; set; }

    [XField(Offset = 0x2C)]
    public float BoostTime { get; set; }

    [XField(Offset = 0x30)]
    public float BoostFactor { get; set; }
}

public sealed class FxElemExtendedDef
{
    public FxElemExtendedDefKind Kind { get; set; }
    public FxTrailDef? TrailDef { get; set; }
    public FxSparkFountainDef? SparkFountainDef { get; set; }
    public FxElemExtendedUnknown? UnknownDef { get; set; }
}

public enum FxElemExtendedDefKind
{
    None,
    Trail,
    SparkFountain,
    Unknown
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x01)]
public sealed class FxElemExtendedUnknown
{
    [XField(Offset = 0x00)]
    public byte Value { get; set; }
}
