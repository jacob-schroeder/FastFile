using FastFile.Models.Data;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Eboot;

/*
public abstract class EbootAssetRoot(XAssetType type) : BaseAsset(type)
{
    public abstract int? EbootRootSize { get; }
    public abstract bool IsHandledByEbootDispatch { get; }
    public byte[] RawRoot { get; set; } = [];
    public DirectPointer<string>? NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name) ? Type.ToString() : Name;
}

public sealed class XAnimParts() : EbootAssetRoot(XAssetType.XAnim)
{
    public const int RootSize = 0x58;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;

    public ushort DataByteCount { get; set; }
    public ushort DataShortCount { get; set; }
    public ushort DataIntCount { get; set; }
    public ushort RandomDataByteCount { get; set; }
    public ushort RandomDataIntCount { get; set; }
    public ushort FrameCount { get; set; }
    public byte[] RootBytes10To1F { get; set; } = new byte[0x10];
    public int RandomDataShortCount { get; set; }
    public int IndexCount { get; set; }
    public float Framerate { get; set; }
    public float Frequency { get; set; }
    public byte BoneNameCount => RootBytes10To1F.Length > 0x0c ? RootBytes10To1F[0x0c] : (byte)0;
    public byte NotifyCount => RootBytes10To1F.Length > 0x0d ? RootBytes10To1F[0x0d] : (byte)0;

    public DirectPointer<byte[]>? NamesPtr { get; set; }
    public DirectPointer<byte[]>? DataBytePtr { get; set; }
    public DirectPointer<byte[]>? DataShortPtr { get; set; }
    public DirectPointer<byte[]>? DataIntPtr { get; set; }
    public DirectPointer<byte[]>? RandomDataShortPtr { get; set; }
    public DirectPointer<byte[]>? RandomDataBytePtr { get; set; }
    public DirectPointer<byte[]>? RandomDataIntPtr { get; set; }
    public DirectPointer<byte[]>? IndicesPtr { get; set; }
    public DirectPointer<byte[]>? NotifyPtr { get; set; }
    public DirectPointer<XAnimDeltaPart>? DeltaPartPtr { get; set; }
}

public sealed class XAnimDeltaPart
{
    public int Offset { get; set; }
    public byte[] Raw { get; set; } = new byte[0x0c];
    public DirectPointer<XAnimDeltaTrans>? TransPtr { get; set; }
    public DirectPointer<XAnimDeltaQuat>? QuatPtr { get; set; }
    public DirectPointer<XAnimDeltaQuat>? Quat2Ptr { get; set; }
}

public sealed class XAnimDeltaTrans
{
    public int Offset { get; set; }
    public byte[] Header { get; set; } = new byte[0x04];
    public byte[] Frame0 { get; set; } = [];
    public byte[] FrameTable { get; set; } = [];
    public byte[] FrameIndices { get; set; } = [];
    public DirectPointer<byte[]>? FrameDataPtr { get; set; }
    public ushort Size { get; set; }
    public bool IsSmall { get; set; }
}

public sealed class XAnimDeltaQuat
{
    public int Offset { get; set; }
    public byte[] Header { get; set; } = new byte[0x04];
    public byte[] Frame0 { get; set; } = [];
    public byte[] FrameTable { get; set; } = [];
    public byte[] FrameIndices { get; set; } = [];
    public DirectPointer<byte[]>? FrameDataPtr { get; set; }
    public ushort Size { get; set; }
}

public sealed class GfxPixelShader() : EbootAssetRoot(XAssetType.PixelShader)
{
    public const int RootSize = 0x18;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class GfxVertexShader() : EbootAssetRoot(XAssetType.VertexShader)
{
    public const int RootSize = 0x0c;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class ColMapSpAsset() : EbootAssetRoot(XAssetType.ColMapSp)
{
    public const int RootSize = 0x100;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class ColMapMpAsset() : EbootAssetRoot(XAssetType.ColMapMp)
{
    public const int RootSize = 0x100;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class ComWorld() : EbootAssetRoot(XAssetType.ComMap)
{
    public const int RootSize = 0x10;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;

    public int IsInUse { get; set; }
    public int PrimaryLightCount { get; set; }
    public DirectPointer<ComPrimaryLight[]> PrimaryLights { get; set; } = new(0);
}

public sealed class ComPrimaryLight
{
    public const int RowSize = 0x44;

    public byte Type { get; set; }
    public byte CanUseShadowMap { get; set; }
    public byte Exponent { get; set; }
    public byte Unused { get; set; }
    public float[] Color { get; set; } = new float[3];
    public float[] Dir { get; set; } = new float[3];
    public float[] Origin { get; set; } = new float[3];
    public float Radius { get; set; }
    public float CosHalfFovOuter { get; set; }
    public float CosHalfFovInner { get; set; }
    public float CosHalfFovExpanded { get; set; }
    public float RotationLimit { get; set; }
    public float TranslationLimit { get; set; }
    public DirectPointer<string> DefName { get; set; } = new(0);
}

public sealed class GameWorldSp() : EbootAssetRoot(XAssetType.GameMapSp)
{
    public const int RootSize = 0x38;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class GameWorldMp() : EbootAssetRoot(XAssetType.GameMapMp)
{
    public const int RootSize = 0x08;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class MapEntsAsset() : EbootAssetRoot(XAssetType.MapEnts)
{
    public const int RootSize = 0x2c;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class FxWorld() : EbootAssetRoot(XAssetType.FxMap)
{
    public const int RootSize = 0x74;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;

    public FxGlassSystem GlassSystem { get; set; } = new();
}

public sealed class FxGlassSystem
{
    public const int RootSize = 0x70;
    public const int FxGlassDefSize = 0x2c;
    public const int PiecePlaceSize = 0x20;
    public const int PieceStateSize = 0x20;
    public const int PieceDynamicsSize = 0x24;
    public const int GeometryDataSize = 0x04;
    public const int LinkOrgSize = 0x0c;
    public const int InitPieceStateSize = 0x34;

    public int Time { get; set; }
    public int PrevTime { get; set; }
    public int DefCount { get; set; }
    public int PieceLimit { get; set; }
    public int PieceWordCount { get; set; }
    public int InitPieceCount { get; set; }
    public int CellCount { get; set; }
    public int ActivePieceCount { get; set; }
    public int FirstFreePiece { get; set; }
    public int GeoDataLimit { get; set; }
    public int GeoDataCount { get; set; }
    public int InitGeoDataCount { get; set; }
    public DirectPointer<FxGlassDef[]> Defs { get; set; } = new(0);
    public DirectPointer<byte[]> PiecePlaces { get; set; } = new(0);
    public DirectPointer<byte[]> PieceStates { get; set; } = new(0);
    public DirectPointer<byte[]> PieceDynamics { get; set; } = new(0);
    public DirectPointer<byte[]> GeoData { get; set; } = new(0);
    public DirectPointer<byte[]> IsInUse { get; set; } = new(0);
    public DirectPointer<byte[]> CellBits { get; set; } = new(0);
    public DirectPointer<byte[]> VisData { get; set; } = new(0);
    public DirectPointer<byte[]> LinkOrg { get; set; } = new(0);
    public DirectPointer<byte[]> HalfThickness { get; set; } = new(0);
    public DirectPointer<byte[]> LightingHandles { get; set; } = new(0);
    public DirectPointer<byte[]> InitPieceStates { get; set; } = new(0);
    public DirectPointer<byte[]> InitGeoData { get; set; } = new(0);
    public byte NeedToCompactData { get; set; }
    public byte InitCount { get; set; }
    public byte[] Padding66To67 { get; set; } = new byte[2];
    public float EffectChanceAccum { get; set; }
    public int LastPieceDeletionTime { get; set; }
}

public sealed class FxGlassDef
{
    public float HalfThickness { get; set; }
    public float[] TexVecs { get; set; } = new float[4];
    public int Color { get; set; }
    public AliasPointer<MaterialAsset> Material { get; set; } = new(0);
    public AliasPointer<MaterialAsset> MaterialShattered { get; set; } = new(0);
    public AliasPointer<PhysPreset> PhysPreset { get; set; } = new(0);
    public float InvHighMipRadius { get; set; }
    public float ShatteredInvHighMipRadius { get; set; }
}

public sealed class GfxWorld() : EbootAssetRoot(XAssetType.GfxMap)
{
    public const int RootSize = 0x288;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class GfxLightDef() : EbootAssetRoot(XAssetType.LightDef)
{
    public const int RootSize = 0x10;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;

    public AliasPointer<GfxImage> AttenuationImage { get; set; } = new(0);
    public byte AttenuationSamplerState { get; set; }
    public byte[] AttenuationPadding { get; set; } = new byte[3];
    public int LmapLookupStart { get; set; }
}

public sealed class UiMapAsset() : EbootAssetRoot(XAssetType.UiMap)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class FontDef() : EbootAssetRoot(XAssetType.Font)
{
    public const int RootSize = 0x18;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed class SndDriverGlobalsAsset() : EbootAssetRoot(XAssetType.SndDriverGlobals)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class FxImpactTable() : EbootAssetRoot(XAssetType.ImpactFx)
{
    public const int RootSize = 0x08;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
    public DirectPointer<FxImpactEntry[]> Table { get; set; } = new(0);

    public const int EntryCount = 15;
}

public sealed class FxImpactEntry
{
    public AliasPointer<FxEffectDef>[] NonFlesh { get; set; } = new AliasPointer<FxEffectDef>[31];
    public AliasPointer<FxEffectDef>[] Flesh { get; set; } = new AliasPointer<FxEffectDef>[4];
}

public sealed class AiTypeAsset() : EbootAssetRoot(XAssetType.AiType)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class MpTypeAsset() : EbootAssetRoot(XAssetType.MpType)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class CharacterAsset() : EbootAssetRoot(XAssetType.Character)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class XModelAliasAsset() : EbootAssetRoot(XAssetType.XModelAlias)
{
    public override int? EbootRootSize => null;
    public override bool IsHandledByEbootDispatch => false;
}

public sealed class LeaderboardDef() : EbootAssetRoot(XAssetType.LeaderboardDef)
{
    public const int RootSize = 0x18;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed partial class VehicleDef() : EbootAssetRoot(XAssetType.Vehicle)
{
    public const int RootSize = 0x2d0;
    public const int SurfaceSoundCount = 31;

    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;

    public DirectPointer<string> UseHintString { get; set; } = new(0);
    public VehiclePhysDef VehPhysDef { get; set; } = new();
    public DirectPointer<string> TurretWeaponName { get; set; } = new(0);
    public AliasPointer<WeaponVariantDef> TurretWeapon { get; set; } = new(0);
    public DirectPointer<string> TurretSpinSnd { get; set; } = new(0);
    public DirectPointer<string> TurretStopSnd { get; set; } = new(0);
    public ushort[] TrophyTags { get; set; } = new ushort[4];
    public AliasPointer<MaterialAsset> CompassFriendlyIcon { get; set; } = new(0);
    public AliasPointer<MaterialAsset> CompassEnemyIcon { get; set; } = new(0);
    public DirectPointer<string> IdleLowSnd { get; set; } = new(0);
    public DirectPointer<string> IdleHighSnd { get; set; } = new(0);
    public DirectPointer<string> EngineLowSnd { get; set; } = new(0);
    public DirectPointer<string> EngineHighSnd { get; set; } = new(0);
    public DirectPointer<string> EngineStartUpSnd { get; set; } = new(0);
    public DirectPointer<string> EngineShutdownSnd { get; set; } = new(0);
    public DirectPointer<string> EngineIdleSnd { get; set; } = new(0);
    public DirectPointer<string> EngineSustainSnd { get; set; } = new(0);
    public DirectPointer<string> EngineRampUpSnd { get; set; } = new(0);
    public DirectPointer<string> EngineRampDownSnd { get; set; } = new(0);
    public DirectPointer<string> SuspensionSoftSnd { get; set; } = new(0);
    public DirectPointer<string> SuspensionHardSnd { get; set; } = new(0);
    public DirectPointer<string> CollisionSnd { get; set; } = new(0);
    public DirectPointer<string> SpeedSnd { get; set; } = new(0);
    public DirectPointer<string> SurfaceSndPrefix { get; set; } = new(0);
    public DirectPointer<string>[] SurfaceSnds { get; set; } = new DirectPointer<string>[SurfaceSoundCount];
}

public sealed class AddonMapEnts() : EbootAssetRoot(XAssetType.AddonMapEnts)
{
    public const int RootSize = 0x24;
    public override int? EbootRootSize => RootSize;
    public override bool IsHandledByEbootDispatch => true;
}

public sealed record EbootXAssetDispatchInfo(
    XAssetType Type,
    int? RootSize,
    bool HasDispatchHandler);

public static class EbootXAssetDispatch
{
    public static readonly IReadOnlyDictionary<XAssetType, EbootXAssetDispatchInfo> Assets =
        new Dictionary<XAssetType, EbootXAssetDispatchInfo>
        {
            [XAssetType.PhysPreset] = new(XAssetType.PhysPreset, 0x2c, true),
            [XAssetType.PhysCollmap] = new(XAssetType.PhysCollmap, 0x48, true),
            [XAssetType.XAnim] = new(XAssetType.XAnim, XAnimParts.RootSize, true),
            [XAssetType.XModelSurfs] = new(XAssetType.XModelSurfs, 0x24, true),
            [XAssetType.XModel] = new(XAssetType.XModel, 0x120, true),
            [XAssetType.Material] = new(XAssetType.Material, 0xa8, true),
            [XAssetType.PixelShader] = new(XAssetType.PixelShader, GfxPixelShader.RootSize, true),
            [XAssetType.VertexShader] = new(XAssetType.VertexShader, GfxVertexShader.RootSize, true),
            [XAssetType.Techset] = new(XAssetType.Techset, 0x9c, true),
            [XAssetType.Image] = new(XAssetType.Image, 0x50, true),
            [XAssetType.Sound] = new(XAssetType.Sound, 0x0c, true),
            [XAssetType.SndCurve] = new(XAssetType.SndCurve, 0x88, true),
            [XAssetType.LoadedSound] = new(XAssetType.LoadedSound, 0x1c, true),
            [XAssetType.ColMapSp] = new(XAssetType.ColMapSp, ColMapSpAsset.RootSize, true),
            [XAssetType.ColMapMp] = new(XAssetType.ColMapMp, ColMapMpAsset.RootSize, true),
            [XAssetType.ComMap] = new(XAssetType.ComMap, ComWorld.RootSize, true),
            [XAssetType.GameMapSp] = new(XAssetType.GameMapSp, GameWorldSp.RootSize, true),
            [XAssetType.GameMapMp] = new(XAssetType.GameMapMp, GameWorldMp.RootSize, true),
            [XAssetType.MapEnts] = new(XAssetType.MapEnts, MapEntsAsset.RootSize, true),
            [XAssetType.FxMap] = new(XAssetType.FxMap, FxWorld.RootSize, true),
            [XAssetType.GfxMap] = new(XAssetType.GfxMap, GfxWorld.RootSize, true),
            [XAssetType.LightDef] = new(XAssetType.LightDef, GfxLightDef.RootSize, true),
            [XAssetType.UiMap] = new(XAssetType.UiMap, null, false),
            [XAssetType.Font] = new(XAssetType.Font, FontDef.RootSize, true),
            [XAssetType.MenuFile] = new(XAssetType.MenuFile, 0x0c, true),
            [XAssetType.Menu] = new(XAssetType.Menu, 0x2f0, true),
            [XAssetType.Localize] = new(XAssetType.Localize, 0x08, true),
            [XAssetType.Weapon] = new(XAssetType.Weapon, 0x74, true),
            [XAssetType.SndDriverGlobals] = new(XAssetType.SndDriverGlobals, null, false),
            [XAssetType.Fx] = new(XAssetType.Fx, 0x20, true),
            [XAssetType.ImpactFx] = new(XAssetType.ImpactFx, FxImpactTable.RootSize, true),
            [XAssetType.AiType] = new(XAssetType.AiType, null, false),
            [XAssetType.MpType] = new(XAssetType.MpType, null, false),
            [XAssetType.Character] = new(XAssetType.Character, null, false),
            [XAssetType.XModelAlias] = new(XAssetType.XModelAlias, null, false),
            [XAssetType.RawFile] = new(XAssetType.RawFile, 0x10, true),
            [XAssetType.StringTable] = new(XAssetType.StringTable, 0x10, true),
            [XAssetType.LeaderboardDef] = new(XAssetType.LeaderboardDef, LeaderboardDef.RootSize, true),
            [XAssetType.StructuredDataDef] = new(XAssetType.StructuredDataDef, 0x0c, true),
            [XAssetType.Tracer] = new(XAssetType.Tracer, 0x70, true),
            [XAssetType.Vehicle] = new(XAssetType.Vehicle, VehicleDef.RootSize, true),
            [XAssetType.AddonMapEnts] = new(XAssetType.AddonMapEnts, AddonMapEnts.RootSize, true),
        };
}
*/