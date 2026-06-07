using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponDef
{
    public int Offset { get; set; }
    public ZonePointer<string> InternalNamePtr { get; set; } = null!;

    public ZonePointer<ZonePointer<XModel>[]> gunXModel { get; set; } = null!; // Count = 16
    public ZonePointer<XModel> handXModel { get; set; } = null!;
    public ZonePointer<ZonePointer<string>[]> szXAnimsR { get; set; } = null!; // Count = 37
    public ZonePointer<ZonePointer<string>[]> szXAnimsL { get; set; } = null!; // Count = 37
    public ZonePointer<string> ModeNamePtr { get; set; } = null!;

    public ZonePointer<ushort[]>[] NoteTrackMaps { get; set; } = new ZonePointer<ushort[]>[4];
    public int[] PlayerAnimTypeThroughStance { get; set; } = new int[8];
    public ZonePointer<FxEffectDef>[] FlashEffects { get; set; } = new ZonePointer<FxEffectDef>[2];
    public ZonePointer<string>[] SoundAliases { get; set; } = new ZonePointer<string>[47];
    public ZonePointer<ZonePointer<string>[]> BounceSound { get; set; } = null!;
    public ZonePointer<FxEffectDef>[] EffectPointersA { get; set; } = new ZonePointer<FxEffectDef>[4];
    public ZonePointer<MaterialAsset>[] MaterialPointersA { get; set; } = new ZonePointer<MaterialAsset>[2];
    public int[] ReticleFields { get; set; } = new int[4];
    public int[] ViewMovementRotationFields { get; set; } = new int[30];
    public int[] PositionalMovementRotationFields { get; set; } = new int[10];

    public ZonePointer<ZonePointer<XModel>[]> WorldGunXModel { get; set; } = null!;
    public ZonePointer<XModel>[] WorldModelPointers { get; set; } = new ZonePointer<XModel>[4];
    public ZonePointer<MaterialAsset> AmmoCounterIcon { get; set; } = null!;
    public int AmmoCounterIconRatio { get; set; }
    public ZonePointer<MaterialAsset> CompassIcon { get; set; } = null!;
    public int CompassIconRatio { get; set; }
    public ZonePointer<MaterialAsset> OverlayMaterial { get; set; } = null!;
    public int[] OverlayFieldsA { get; set; } = new int[3];
    public ZonePointer<string> OverlayReticle { get; set; } = null!;
    public int OverlayReticleField { get; set; }
    public ZonePointer<string> OverlayInterface { get; set; } = null!;
    public int[] OverlayFieldsB { get; set; } = new int[3];
    public ZonePointer<string> ModeNameAlt { get; set; } = null!;
    public int[] ModeFields { get; set; } = new int[6];
    public int[] WeaponTimingFields { get; set; } = new int[40];
    public int[] AimMovementTuningFields { get; set; } = new int[10];

    public ZonePointer<MaterialAsset>[] OverlayMaterials { get; set; } = new ZonePointer<MaterialAsset>[4];
    public int[] OverlayDimensionFields { get; set; } = new int[6];
    public int[] BobSpreadIdleSwayAdsViewErrorFields { get; set; } = new int[38];
    public ZonePointer<PhysCollmap> PhysCollmap { get; set; } = null!;
    public int[] PhysicsFieldsA { get; set; } = new int[2];
    public int[] PhysicsFieldsB { get; set; } = new int[5];
    public int[] PhysicsFieldsC { get; set; } = new int[7];
    public int[] PhysicsFieldsD { get; set; } = new int[7];
    public ZonePointer<XModel> ProjectileModel { get; set; } = null!;
    public int ProjectileModelField { get; set; }
    public ZonePointer<FxEffectDef>[] ProjectileEffects { get; set; } = new ZonePointer<FxEffectDef>[2];
    public ZonePointer<string>[] ProjectileSoundAliases { get; set; } = new ZonePointer<string>[2];
    public int[] ProjectileFieldsA { get; set; } = new int[3];
    public ZonePointer<float[]> ParallelBounce { get; set; } = null!;
    public ZonePointer<float[]> PerpendicularBounce { get; set; } = null!;
    public ZonePointer<FxEffectDef>[] ImpactEffects { get; set; } = new ZonePointer<FxEffectDef>[2];
    public int[] ImpactFieldsA { get; set; } = new int[3];
    public int ImpactFieldB { get; set; }
    public int[] ImpactFieldsC { get; set; } = new int[2];
    public ZonePointer<FxEffectDef> ViewShellEjectEffect { get; set; } = null!;
    public ZonePointer<string> ShellEjectSound { get; set; } = null!;
    public int[] ShellEjectFields { get; set; } = new int[3];
    public int[] AdsHipGunKickAiDistanceFields { get; set; } = new int[35];

    public ZonePointer<string> AccuracyGraphName0 { get; set; } = null!;
    public ZonePointer<string> AccuracyGraphName1 { get; set; } = null!;
    public ZonePointer<Vec2[]> accuracyGraphKnots { get; set; } = null!;
    public ZonePointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!;
    public ushort accuracyGraphKnotCount { get; set; }
    public ushort originalAccuracyGraphKnotCount { get; set; }
    public int AccuracyGraphField { get; set; }
    public float LeftArc { get; set; }
    public float RightArc { get; set; }
    public float TopArc { get; set; }
    public float BottomArc { get; set; }
    public float Accuracy { get; set; }
    public float AiSpread { get; set; }
    public float PlayerSpread { get; set; }
    public float[] MinTurnSpeed { get; set; } = new float[2];
    public float[] MaxTurnSpeed { get; set; } = new float[2];
    public float PitchConvergenceTime { get; set; }
    public float YawConvergenceTime { get; set; }
    public float SuppressTime { get; set; }
    public float MaxRange { get; set; }
    public float AnimHorizontalRotateInc { get; set; }
    public float PlayerPositionDist { get; set; }
    public ZonePointer<string> UseHintString { get; set; } = null!;
    public ZonePointer<string> DropHintString { get; set; } = null!;
    public int[] HintFieldsA { get; set; } = new int[2];
    public int[] HintFieldsB { get; set; } = new int[5];
    public ZonePointer<string> ScriptName { get; set; } = null!;
    public int[] ScriptFieldsA { get; set; } = new int[2];
    public int[] ScriptFieldsB { get; set; } = new int[6];
    public int HitLocationField { get; set; }
    public ZonePointer<float[]> LocationDamageMultipliers { get; set; } = null!;
    public ZonePointer<string> FireRumble { get; set; } = null!;
    public ZonePointer<string> MeleeImpactRumble { get; set; } = null!;
    public ZonePointer<TracerDef> Tracer { get; set; } = null!;

    public int[] TracerFields { get; set; } = new int[6];
    public ZonePointer<string> TurretOverheatSound { get; set; } = null!;
    public ZonePointer<FxEffectDef> TurretOverheatEffect { get; set; } = null!;
    public ZonePointer<string> TurretBarrelSpinRumble { get; set; } = null!;
    public int[] TurretFields { get; set; } = new int[3];
    public ZonePointer<string> TurretBarrelSpinMaxSnd { get; set; } = null!;
    public ZonePointer<string>[] TurretBarrelSpinUpSnd { get; set; } = new ZonePointer<string>[4];
    public ZonePointer<string>[] TurretBarrelSpinDownSnd { get; set; } = new ZonePointer<string>[4];
    public ZonePointer<string> MissileConeSoundAlias { get; set; } = null!;
    public ZonePointer<string> MissileConeSoundAliasAtBase { get; set; } = null!;
    public float MissileConeSoundRadiusAtTop { get; set; }
    public float MissileConeSoundRadiusAtBase { get; set; }
    public float MissileConeSoundHeight { get; set; }
    public float MissileConeSoundOriginOffset { get; set; }
    public float MissileConeSoundVolumescaleAtCore { get; set; }
    public float MissileConeSoundVolumescaleAtEdge { get; set; }
    public float MissileConeSoundVolumescaleCoreSize { get; set; }
    public float MissileConeSoundPitchAtTop { get; set; }
    public float MissileConeSoundPitchAtBottom { get; set; }
    public float MissileConeSoundPitchTopSize { get; set; }
    public float MissileConeSoundPitchBottomSize { get; set; }
    public float MissileConeSoundCrossfadeTopSize { get; set; }
    public float MissileConeSoundCrossfadeBottomSize { get; set; }
    public byte[] BooleanTailBytes { get; set; } = [];
    public bool SharedAmmo { get; set; }
    public bool LockonSupported { get; set; }
    public bool RequireLockonToFire { get; set; }
    public bool BigExplosion { get; set; }
    public WeaponBooleanFlags BooleanFlags { get; set; } = new();

    public string InternalName => InternalNamePtr is { IsResolved: true }
        ? InternalNamePtr.Result ?? string.Empty
        : string.Empty;
}

public sealed class WeaponBooleanFlags
{
    public bool NoAdsWhenMagEmpty { get; set; }
    public bool AvoidDropCleanup { get; set; }
    public bool InheritsPerks { get; set; }
    public bool CrosshairColorChange { get; set; }
    public bool RifleBullet { get; set; }
    public bool ArmorPiercing { get; set; }
    public bool BoltAction { get; set; }
    public bool AimDownSight { get; set; }
    public bool RechamberWhileAds { get; set; }
    public bool BulletExplosiveDamage { get; set; }
    public bool CookOffHold { get; set; }
    public bool ClipOnly { get; set; }
    public bool NoAmmoPickup { get; set; }
    public bool AdsFireOnly { get; set; }
    public bool CancelAutoHolsterWhenEmpty { get; set; }
    public bool DisableSwitchToWhenEmpty { get; set; }
    public bool SuppressAmmoReserveDisplay { get; set; }
    public bool LaserSightDuringNightvision { get; set; }
    public bool MarkableViewmodel { get; set; }
    public bool NoDualWield { get; set; }
    public bool FlipKillIcon { get; set; }
    public bool NoPartialReload { get; set; }
    public bool SegmentedReload { get; set; }
    public bool BlocksProne { get; set; }
    public bool Silenced { get; set; }
    public bool IsRollingGrenade { get; set; }
    public bool ProjExplosionEffectForceNormalUp { get; set; }
    public bool ProjImpactExplode { get; set; }
    public bool StickToPlayers { get; set; }
    public bool HasDetonator { get; set; }
    public bool DisableFiring { get; set; }
    public bool TimedDetonation { get; set; }
    public bool Rotate { get; set; }
    public bool HoldButtonToThrow { get; set; }
    public bool FreezeMovementWhenFiring { get; set; }
    public bool ThermalScope { get; set; }
    public bool AltModeSameWeapon { get; set; }
    public bool TurretBarrelSpinEnabled { get; set; }
    public bool MissileConeSoundEnabled { get; set; }
    public bool MissileConeSoundPitchshiftEnabled { get; set; }
    public bool MissileConeSoundCrossfadeEnabled { get; set; }
    public bool OffhandHoldIsCancelable { get; set; }
    public byte Ps3TailFlag0 { get; set; }
    public byte Ps3TailFlag1 { get; set; }
}
