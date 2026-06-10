using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponDef
{
    public int Offset { get; set; }

    public XPointer<string> InternalNamePtr { get; set; } = null!; // Direct

    public XPointer<XPointer<XModel>[]> gunXModel { get; set; } = null!; // Count = 16, Direct
    public XPointer<XModel> handXModel { get; set; } = null!; // Alias
    public XPointer<XPointer<string>[]> szXAnimsR { get; set; } = null!; // Count = 37, Direct
    public XPointer<XPointer<string>[]> szXAnimsL { get; set; } = null!; // Count = 37, Direct
    public XPointer<string> ModeNamePtr { get; set; } = null!; // Direct

    public XPointer<ushort[]>[] NoteTrackMaps { get; set; } = new XPointer<ushort[]>[4]; // Direct
    public int[] PlayerAnimTypeThroughStance { get; set; } = new int[8];
    public XPointer<FxEffectDef>[] FlashEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias
    public XPointer<string>[] SoundAliases { get; set; } = new XPointer<string>[WeaponSoundAliasCount]; // Direct
    public XPointer<XPointer<string>[]> BounceSound { get; set; } = null!;
    public XPointer<FxEffectDef>[] EffectPointersA { get; set; } = new XPointer<FxEffectDef>[4]; // Alias
    public XPointer<MaterialAsset>[] MaterialPointersA { get; set; } = new XPointer<MaterialAsset>[2]; // Alias
    public int[] ReticleFields { get; set; } = new int[4];
    public int[] ViewMovementRotationFields { get; set; } = new int[30];
    public int[] PositionalMovementRotationFields { get; set; } = new int[10];

    public XPointer<XPointer<XModel>[]> WorldGunXModel { get; set; } = null!; // Direct -> ?
    public XPointer<XModel>[] WorldModelPointers { get; set; } = new XPointer<XModel>[4]; // Alias
    public XPointer<MaterialAsset> AmmoCounterIcon { get; set; } = null!; // Alias
    public int AmmoCounterIconRatio { get; set; }
    public XPointer<MaterialAsset> CompassIcon { get; set; } = null!; // Alias
    public int CompassIconRatio { get; set; }
    public XPointer<MaterialAsset> OverlayMaterial { get; set; } = null!; // Alias
    public int[] OverlayFieldsA { get; set; } = new int[3];
    public XPointer<string> OverlayReticle { get; set; } = null!; // Direct
    public int OverlayReticleField { get; set; }
    public XPointer<string> OverlayInterface { get; set; } = null!; // Direct
    public int[] OverlayFieldsB { get; set; } = new int[3];
    public XPointer<string> ModeNameAlt { get; set; } = null!; // Direct
    public int[] ModeFields { get; set; } = new int[6];
    public int[] WeaponTimingFields { get; set; } = new int[40];
    public int[] AimMovementTuningFields { get; set; } = new int[10];

    public XPointer<MaterialAsset>[] OverlayMaterials { get; set; } = new XPointer<MaterialAsset>[4]; // Alias
    public int[] OverlayDimensionFields { get; set; } = new int[6];
    public int[] BobSpreadIdleSwayAdsViewErrorFields { get; set; } = new int[38];
    public XPointer<PhysCollmap> PhysCollmap { get; set; } = null!; // Alias
    public int[] PhysicsFieldsA { get; set; } = new int[2];
    public int[] PhysicsFieldsB { get; set; } = new int[5];
    public int[] PhysicsFieldsC { get; set; } = new int[7];
    public int[] PhysicsFieldsD { get; set; } = new int[7];
    public XPointer<XModel> ProjectileModel { get; set; } = null!; // Alias
    public int ProjectileModelField { get; set; }
    public XPointer<FxEffectDef>[] ProjectileEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias
    public XPointer<string>[] ProjectileSoundAliases { get; set; } = new XPointer<string>[2]; // Direct
    public int[] ProjectileFieldsA { get; set; } = new int[3];
    public XPointer<float[]> ParallelBounce { get; set; } = null!; // Direct
    public XPointer<float[]> PerpendicularBounce { get; set; } = null!; // Direct
    public XPointer<FxEffectDef>[] ImpactEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias
    public int[] ImpactFieldsA { get; set; } = new int[3];
    public int ImpactFieldB { get; set; }
    public int[] ImpactFieldsC { get; set; } = new int[2];
    public XPointer<FxEffectDef> ViewShellEjectEffect { get; set; } = null!; // Alias
    public XPointer<string> ShellEjectSound { get; set; } = null!; // Direct
    public int[] ShellEjectFields { get; set; } = new int[3];
    public int[] AdsHipGunKickAiDistanceFields { get; set; } = new int[35];

    public XPointer<string> AccuracyGraphName0 { get; set; } = null!; // Direct
    public XPointer<string> AccuracyGraphName1 { get; set; } = null!; // Direct
    public XPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!; // Direct
    public XPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!; // Direct
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
    public XPointer<string> UseHintString { get; set; } = null!; // Direct
    public XPointer<string> DropHintString { get; set; } = null!; // Direct
    public int[] HintFieldsA { get; set; } = new int[2];
    public int[] HintFieldsB { get; set; } = new int[5];
    public XPointer<string> ScriptName { get; set; } = null!; // Direct
    public int[] ScriptFieldsA { get; set; } = new int[2];
    public int[] ScriptFieldsB { get; set; } = new int[6];
    public int HitLocationField { get; set; }
    public XPointer<float[]> LocationDamageMultipliers { get; set; } = null!; // Direct
    public XPointer<string> FireRumble { get; set; } = null!; // Direct
    public XPointer<string> MeleeImpactRumble { get; set; } = null!; // Direct
    public XPointer<TracerDef> Tracer { get; set; } = null!; // Alias

    public int[] TracerFields { get; set; } = new int[6];
    public XPointer<string> TurretOverheatSound { get; set; } = null!; // Direct
    public XPointer<FxEffectDef> TurretOverheatEffect { get; set; } = null!; // Alias
    public XPointer<string> TurretBarrelSpinRumble { get; set; } = null!; // Direct
    public int[] TurretFields { get; set; } = new int[3];
    public XPointer<string> TurretBarrelSpinMaxSnd { get; set; } = null!; // Direct
    public XPointer<string>[] TurretBarrelSpinUpSnd { get; set; } = new XPointer<string>[4]; // Direct
    public XPointer<string>[] TurretBarrelSpinDownSnd { get; set; } = new XPointer<string>[4]; // Direct
    public XPointer<string> MissileConeSoundAlias { get; set; } = null!; // Direct
    public XPointer<string> MissileConeSoundAliasAtBase { get; set; } = null!; // Direct
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
        ? InternalNamePtr.Value ?? string.Empty
        : string.Empty;

    public const int GunModelCount = 16;
    public const int WeaponAnimCount = 37;
    public const int SurfaceCount = 31;
    public const int HitLocationCount = 20;
    public const int WeaponSoundAliasCount = 47;
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
