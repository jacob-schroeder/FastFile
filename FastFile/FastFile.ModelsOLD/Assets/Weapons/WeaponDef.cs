using FastFile.ModelsOLD.Assets.Effects;
using FastFile.ModelsOLD.Assets.Physics;
using FastFile.ModelsOLD.Assets.Tracers;
using FastFile.ModelsOLD.Assets.XModels;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Utils;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;

namespace FastFile.ModelsOLD.Assets.Weapons;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x684)]
[XEbootEvidence(
    "0x114678",
    "eboot/traces/weapon_loaders_114678_1152f8.txt",
    Detail = "WeaponDef body: Load_Stream size 0x684; XString at +0x00; model/anim arrays at +0x04/+0x0c/+0x10; mode string +0x14; many XString/alias/array offsets verified through +0x61c, including +0x1d8 model array, +0x444/+0x448 float arrays, and +0x5f8/+0x608 string pointer arrays.")]
public sealed class WeaponDef
{
    public int Offset { get; set; }

    [XField(Offset = 0x000)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> InternalNamePtr { get; set; } = null!; // Direct

    [XField(Offset = 0x004)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Alias,
        ElementTarget = XPointerTarget.Object,
        CountMember = nameof(GunModelCount))]
    public XPointer<XPointer<XModel>[]> gunXModel { get; set; } = null!; // Count = 16, Direct

    [XField(Offset = 0x008)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<XModel> handXModel { get; set; } = null!; // Alias

    [XField(Offset = 0x00C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(WeaponAnimCount))]
    public XPointer<XPointer<string>[]> szXAnimsR { get; set; } = null!; // Count = 37, Direct

    [XField(Offset = 0x010)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(WeaponAnimCount))]
    public XPointer<XPointer<string>[]> szXAnimsL { get; set; } = null!; // Count = 37, Direct

    [XField(Offset = 0x014)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> ModeNamePtr { get; set; } = null!; // Direct

    [XField(Offset = 0x018, Count = 4)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(NoteTrackMapCount))]
    public XPointer<ushort[]>[] NoteTrackMaps { get; set; } = new XPointer<ushort[]>[4]; // Direct
    public int[] PlayerAnimTypeThroughStance { get; set; } = new int[8];

    [XField(Offset = 0x048, Count = 2)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef>[] FlashEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias

    [XField(Offset = 0x050, Count = WeaponSoundAliasCount)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string>[] SoundAliases { get; set; } = new XPointer<string>[WeaponSoundAliasCount]; // Load_SndAliasCustom name

    [XField(Offset = 0x10C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(SurfaceCount))]
    public XPointer<XPointer<string>[]> BounceSound { get; set; } = null!;

    [XField(Offset = 0x110, Count = 4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef>[] EffectPointersA { get; set; } = new XPointer<FxEffectDef>[4]; // Alias

    [XField(Offset = 0x120, Count = 2)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<Material.Material>[] MaterialPointersA { get; set; } = new XPointer<Material.Material>[2]; // Alias
    public int[] ReticleFields { get; set; } = new int[4];
    public int[] ViewMovementRotationFields { get; set; } = new int[30];
    public int[] PositionalMovementRotationFields { get; set; } = new int[10];

    [XField(Offset = 0x1D8)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Alias,
        ElementTarget = XPointerTarget.Object,
        CountMember = nameof(GunModelCount))]
    public XPointer<XPointer<XModel>[]> WorldGunXModel { get; set; } = null!; // Direct -> ?

    [XField(Offset = 0x1DC, Count = 4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<XModel>[] WorldModelPointers { get; set; } = new XPointer<XModel>[4]; // Alias

    [XField(Offset = 0x1EC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<Material.Material> AmmoCounterIcon { get; set; } = null!; // Alias
    public int AmmoCounterIconRatio { get; set; }

    [XField(Offset = 0x1F4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<Material.Material> CompassIcon { get; set; } = null!; // Alias
    public int CompassIconRatio { get; set; }

    [XField(Offset = 0x1FC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<Material.Material> OverlayMaterial { get; set; } = null!; // Alias
    public int[] OverlayFieldsA { get; set; } = new int[3];

    [XField(Offset = 0x20C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> OverlayReticle { get; set; } = null!; // Direct
    public int OverlayReticleField { get; set; }

    [XField(Offset = 0x214)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> OverlayInterface { get; set; } = null!; // Direct
    public int[] OverlayFieldsB { get; set; } = new int[3];

    [XField(Offset = 0x224)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> ModeNameAlt { get; set; } = null!; // Direct
    public int[] ModeFields { get; set; } = new int[6];
    public int[] WeaponTimingFields { get; set; } = new int[40];
    public int[] AimMovementTuningFields { get; set; } = new int[10];

    [XField(Offset = 0x308, Count = 4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<Material.Material>[] OverlayMaterials { get; set; } = new XPointer<Material.Material>[4]; // Alias
    public int[] OverlayDimensionFields { get; set; } = new int[6];
    public int[] BobSpreadIdleSwayAdsViewErrorFields { get; set; } = new int[38];

    [XField(Offset = 0x3C8)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<PhysCollmap> PhysCollmap { get; set; } = null!; // Alias
    public int[] PhysicsFieldsA { get; set; } = new int[2];
    public int[] PhysicsFieldsB { get; set; } = new int[5];
    public int[] PhysicsFieldsC { get; set; } = new int[7];
    public int[] PhysicsFieldsD { get; set; } = new int[7];

    [XField(Offset = 0x420)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<XModel> ProjectileModel { get; set; } = null!; // Alias
    public int ProjectileModelField { get; set; }

    [XField(Offset = 0x428, Count = 2)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef>[] ProjectileEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias

    [XField(Offset = 0x430, Count = 2)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string>[] ProjectileSoundAliases { get; set; } = new XPointer<string>[2]; // Load_SndAliasCustom name
    public int[] ProjectileFieldsA { get; set; } = new int[3];

    [XField(Offset = 0x444)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(SurfaceCount))]
    public XPointer<float[]> ParallelBounce { get; set; } = null!; // Direct

    [XField(Offset = 0x448)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(SurfaceCount))]
    public XPointer<float[]> PerpendicularBounce { get; set; } = null!; // Direct

    [XField(Offset = 0x44C, Count = 2)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef>[] ImpactEffects { get; set; } = new XPointer<FxEffectDef>[2]; // Alias
    public int[] ImpactFieldsA { get; set; } = new int[3];
    public int ImpactFieldB { get; set; }
    public int[] ImpactFieldsC { get; set; } = new int[2];

    [XField(Offset = 0x46C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef> ViewShellEjectEffect { get; set; } = null!; // Alias

    [XField(Offset = 0x470)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> ShellEjectSound { get; set; } = null!; // Load_SndAliasCustom name
    public int[] ShellEjectFields { get; set; } = new int[3];
    public int[] AdsHipGunKickAiDistanceFields { get; set; } = new int[35];

    [XField(Offset = 0x50C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> AccuracyGraphName0 { get; set; } = null!; // Direct

    [XField(Offset = 0x510)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> AccuracyGraphName1 { get; set; } = null!; // Direct

    [XField(Offset = 0x514)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(accuracyGraphKnotCount))]
    public XPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!; // Direct

    [XField(Offset = 0x518)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(originalAccuracyGraphKnotCount))]
    public XPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!; // Direct

    [XField(Offset = 0x51C)]
    public ushort accuracyGraphKnotCount { get; set; }

    [XField(Offset = 0x51E)]
    public ushort originalAccuracyGraphKnotCount { get; set; }

    [XField(Offset = 0x520)]
    public int AccuracyGraphField { get; set; }

    [XField(Offset = 0x524)]
    public float LeftArc { get; set; }

    [XField(Offset = 0x528)]
    public float RightArc { get; set; }

    [XField(Offset = 0x52C)]
    public float TopArc { get; set; }

    [XField(Offset = 0x530)]
    public float BottomArc { get; set; }

    [XField(Offset = 0x534)]
    public float Accuracy { get; set; }

    [XField(Offset = 0x538)]
    public float AiSpread { get; set; }

    [XField(Offset = 0x53C)]
    public float PlayerSpread { get; set; }

    [XField(Offset = 0x540, Count = 2)]
    public float[] MinTurnSpeed { get; set; } = new float[2];

    [XField(Offset = 0x548, Count = 2)]
    public float[] MaxTurnSpeed { get; set; } = new float[2];

    [XField(Offset = 0x550)]
    public float PitchConvergenceTime { get; set; }

    [XField(Offset = 0x554)]
    public float YawConvergenceTime { get; set; }

    [XField(Offset = 0x558)]
    public float SuppressTime { get; set; }

    [XField(Offset = 0x55C)]
    public float MaxRange { get; set; }

    [XField(Offset = 0x560)]
    public float AnimHorizontalRotateInc { get; set; }

    [XField(Offset = 0x564)]
    public float PlayerPositionDist { get; set; }

    [XField(Offset = 0x568)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> UseHintString { get; set; } = null!; // Direct

    [XField(Offset = 0x56C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> DropHintString { get; set; } = null!; // Direct
    public int[] HintFieldsA { get; set; } = new int[2];
    public int[] HintFieldsB { get; set; } = new int[5];

    [XField(Offset = 0x58C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> ScriptName { get; set; } = null!; // Direct
    public int[] ScriptFieldsA { get; set; } = new int[2];
    public int[] ScriptFieldsB { get; set; } = new int[6];
    public int HitLocationField { get; set; }

    [XField(Offset = 0x5B4)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(HitLocationCount))]
    public XPointer<float[]> LocationDamageMultipliers { get; set; } = null!; // Direct

    [XField(Offset = 0x5B8)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> FireRumble { get; set; } = null!; // Direct

    [XField(Offset = 0x5BC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> MeleeImpactRumble { get; set; } = null!; // Direct

    [XField(Offset = 0x5C0)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<TracerDef> Tracer { get; set; } = null!; // Alias

    public int[] TracerFields { get; set; } = new int[6];

    [XField(Offset = 0x5DC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> TurretOverheatSound { get; set; } = null!; // Load_SndAliasCustom name

    [XField(Offset = 0x5E0)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FxEffectDef> TurretOverheatEffect { get; set; } = null!; // Alias

    [XField(Offset = 0x5E4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> TurretBarrelSpinRumble { get; set; } = null!; // Direct
    public int[] TurretFields { get; set; } = new int[3];

    [XField(Offset = 0x5F4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> TurretBarrelSpinMaxSnd { get; set; } = null!; // Load_SndAliasCustom name

    [XField(Offset = 0x5F8)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(TurretBarrelSpinSoundCount))]
    public XPointer<XPointer<string>[]> TurretBarrelSpinUpSnd { get; set; } = null!; // Load_SndAliasCustom name array

    [XField(Offset = 0x608)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(TurretBarrelSpinSoundCount))]
    public XPointer<XPointer<string>[]> TurretBarrelSpinDownSnd { get; set; } = null!; // Load_SndAliasCustom name array

    [XField(Offset = 0x618)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> MissileConeSoundAlias { get; set; } = null!; // Load_SndAliasCustom name

    [XField(Offset = 0x61C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> MissileConeSoundAliasAtBase { get; set; } = null!; // Load_SndAliasCustom name
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
    public const int NoteTrackMapCount = 16;
    public const int SurfaceCount = 31;
    public const int HitLocationCount = 20;
    public const int WeaponSoundAliasCount = 47;
    public const int TurretBarrelSpinSoundCount = 4;
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
