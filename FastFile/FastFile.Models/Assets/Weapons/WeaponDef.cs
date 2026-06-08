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

    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> InternalNamePtr { get; set; } = null!;

    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(GunModelCount))]
    public DirectPointer<ZonePointer<XModel>[]> gunXModel { get; set; } = null!; // Count = 16
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<XModel> handXModel { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(WeaponAnimCount))]
    public DirectPointer<ZonePointer<string>[]> szXAnimsR { get; set; } = null!; // Count = 37
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(WeaponAnimCount))]
    public DirectPointer<ZonePointer<string>[]> szXAnimsL { get; set; } = null!; // Count = 37
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> ModeNamePtr { get; set; } = null!;

    public DirectPointer<ushort[]>[] NoteTrackMaps { get; set; } = new DirectPointer<ushort[]>[4];
    public int[] PlayerAnimTypeThroughStance { get; set; } = new int[8];
    public AliasPointer<FxEffectDef>[] FlashEffects { get; set; } = new AliasPointer<FxEffectDef>[2];
    public DirectPointer<string>[] SoundAliases { get; set; } = new DirectPointer<string>[WeaponSoundAliasCount];
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(SurfaceCount))]
    public DirectPointer<ZonePointer<string>[]> BounceSound { get; set; } = null!;
    public AliasPointer<FxEffectDef>[] EffectPointersA { get; set; } = new AliasPointer<FxEffectDef>[4];
    public AliasPointer<MaterialAsset>[] MaterialPointersA { get; set; } = new AliasPointer<MaterialAsset>[2];
    public int[] ReticleFields { get; set; } = new int[4];
    public int[] ViewMovementRotationFields { get; set; } = new int[30];
    public int[] PositionalMovementRotationFields { get; set; } = new int[10];

    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(GunModelCount))]
    public DirectPointer<ZonePointer<XModel>[]> WorldGunXModel { get; set; } = null!;
    public AliasPointer<XModel>[] WorldModelPointers { get; set; } = new AliasPointer<XModel>[4];
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<MaterialAsset> AmmoCounterIcon { get; set; } = null!;
    public int AmmoCounterIconRatio { get; set; }
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<MaterialAsset> CompassIcon { get; set; } = null!;
    public int CompassIconRatio { get; set; }
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<MaterialAsset> OverlayMaterial { get; set; } = null!;
    public int[] OverlayFieldsA { get; set; } = new int[3];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> OverlayReticle { get; set; } = null!;
    public int OverlayReticleField { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> OverlayInterface { get; set; } = null!;
    public int[] OverlayFieldsB { get; set; } = new int[3];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> ModeNameAlt { get; set; } = null!;
    public int[] ModeFields { get; set; } = new int[6];
    public int[] WeaponTimingFields { get; set; } = new int[40];
    public int[] AimMovementTuningFields { get; set; } = new int[10];

    public AliasPointer<MaterialAsset>[] OverlayMaterials { get; set; } = new AliasPointer<MaterialAsset>[4];
    public int[] OverlayDimensionFields { get; set; } = new int[6];
    public int[] BobSpreadIdleSwayAdsViewErrorFields { get; set; } = new int[38];
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<PhysCollmap> PhysCollmap { get; set; } = null!;
    public int[] PhysicsFieldsA { get; set; } = new int[2];
    public int[] PhysicsFieldsB { get; set; } = new int[5];
    public int[] PhysicsFieldsC { get; set; } = new int[7];
    public int[] PhysicsFieldsD { get; set; } = new int[7];
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<XModel> ProjectileModel { get; set; } = null!;
    public int ProjectileModelField { get; set; }
    public AliasPointer<FxEffectDef>[] ProjectileEffects { get; set; } = new AliasPointer<FxEffectDef>[2];
    public DirectPointer<string>[] ProjectileSoundAliases { get; set; } = new DirectPointer<string>[2];
    public int[] ProjectileFieldsA { get; set; } = new int[3];
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(SurfaceCount))]
    public DirectPointer<float[]> ParallelBounce { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(SurfaceCount))]
    public DirectPointer<float[]> PerpendicularBounce { get; set; } = null!;
    public AliasPointer<FxEffectDef>[] ImpactEffects { get; set; } = new AliasPointer<FxEffectDef>[2];
    public int[] ImpactFieldsA { get; set; } = new int[3];
    public int ImpactFieldB { get; set; }
    public int[] ImpactFieldsC { get; set; } = new int[2];
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<FxEffectDef> ViewShellEjectEffect { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> ShellEjectSound { get; set; } = null!;
    public int[] ShellEjectFields { get; set; } = new int[3];
    public int[] AdsHipGunKickAiDistanceFields { get; set; } = new int[35];

    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> AccuracyGraphName0 { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> AccuracyGraphName1 { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(accuracyGraphKnotCount))]
    public DirectPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(originalAccuracyGraphKnotCount))]
    public DirectPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!;
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
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> UseHintString { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> DropHintString { get; set; } = null!;
    public int[] HintFieldsA { get; set; } = new int[2];
    public int[] HintFieldsB { get; set; } = new int[5];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> ScriptName { get; set; } = null!;
    public int[] ScriptFieldsA { get; set; } = new int[2];
    public int[] ScriptFieldsB { get; set; } = new int[6];
    public int HitLocationField { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(HitLocationCount))]
    public DirectPointer<float[]> LocationDamageMultipliers { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> FireRumble { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> MeleeImpactRumble { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<TracerDef> Tracer { get; set; } = null!;

    public int[] TracerFields { get; set; } = new int[6];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> TurretOverheatSound { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<FxEffectDef> TurretOverheatEffect { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> TurretBarrelSpinRumble { get; set; } = null!;
    public int[] TurretFields { get; set; } = new int[3];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> TurretBarrelSpinMaxSnd { get; set; } = null!;
    public DirectPointer<string>[] TurretBarrelSpinUpSnd { get; set; } = new DirectPointer<string>[4];
    public DirectPointer<string>[] TurretBarrelSpinDownSnd { get; set; } = new DirectPointer<string>[4];
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> MissileConeSoundAlias { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> MissileConeSoundAliasAtBase { get; set; } = null!;
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
