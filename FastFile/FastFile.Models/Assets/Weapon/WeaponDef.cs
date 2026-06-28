using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Weapon;

public sealed class WeaponDef
{
    public const int SerializedSize = 0x684;
    public const int GunModelCount = 16;
    public const int WeaponAnimCount = 37;
    public const int NoteTrackMapCount = 16;
    public const int SurfaceCount = 31;
    public const int HitLocationCount = 20;
    public const int WeaponSoundAliasCount = 47;
    public const int TurretBarrelSpinSoundCount = 4;

    public int Offset { get; init; }

    // 0x000: XString.
    public XString InternalNamePointer { get; init; }
    public string? InternalName { get; init; }

    // 0x004: direct XModelPtr[16].
    public XPointer<XPointer<XModelAsset>[]> GunModelsPointer { get; init; }
    public IReadOnlyList<XPointer<XModelAsset>> GunModelPointers { get; init; } = [];

    // 0x008: alias-cell XModel pointer.
    public XPointer<XModelAsset> HandModelPointer { get; init; }

    // 0x00C / 0x010: direct XString pointer arrays, count 37.
    public XPointer<XString[]> RightHandAnimationNamesPointer { get; init; }
    public IReadOnlyList<XString> RightHandAnimationNamePointers { get; init; } = [];
    public IReadOnlyList<string?> RightHandAnimationNames { get; init; } = [];
    public XPointer<XString[]> LeftHandAnimationNamesPointer { get; init; }
    public IReadOnlyList<XString> LeftHandAnimationNamePointers { get; init; } = [];
    public IReadOnlyList<string?> LeftHandAnimationNames { get; init; } = [];

    // 0x014: XString.
    public XString ModeNamePointer { get; init; }
    public string? ModeName { get; init; }

    public WeaponNoteTrackMaps NoteTrackMaps { get; init; } = new(); // 0x018..0x024

    public int Unknown028 { get; init; }                    // 0x028: copied scalar; semantic still open.
    public WeaponType WeaponType { get; init; }              // 0x02C: PS3 runtime discriminator.
    public WeaponClass WeaponClass { get; init; }            // 0x030: PS3 runtime discriminator.
    public IReadOnlyList<int> Unknown034To044 { get; init; } = [];

    // 0x048..0x120 loader-proven pointer region.
    public IReadOnlyList<XPointer<FxEffectDefAsset>> FlashEffectPointers { get; init; } = [];
    public IReadOnlyList<XString> SoundAliasPointers { get; init; } = [];
    public IReadOnlyList<string?> SoundAliasNames { get; init; } = [];
    public XPointer<XString[]> BounceSoundPointer { get; init; }
    public IReadOnlyList<XString> BounceSoundPointers { get; init; } = [];
    public IReadOnlyList<string?> BounceSoundNames { get; init; } = [];
    public IReadOnlyList<XPointer<FxEffectDefAsset>> EffectPointers { get; init; } = [];
    public IReadOnlyList<XPointer<Material.MaterialAsset>> MaterialPointers { get; init; } = [];
    public IReadOnlyList<int> ReticleFields { get; init; } = [];                       // 0x128..0x134
    public IReadOnlyList<int> ViewMovementRotationFields { get; init; } = [];          // 0x138..0x1AC
    public IReadOnlyList<int> PositionalMovementRotationFields { get; init; } = [];    // 0x1B0..0x1D4

    // 0x1D8..0x1E8: XModel shell references.
    public XPointer<XPointer<XModelAsset>[]> WorldGunModelsPointer { get; init; }
    public IReadOnlyList<XPointer<XModelAsset>> WorldGunModelPointers { get; init; } = [];
    public IReadOnlyList<XPointer<XModelAsset>> WorldModelPointers { get; init; } = [];

    public WeaponIconPointers Icons { get; init; } = new();  // 0x1EC..0x1FC
    public WeaponOverlayFields Overlay { get; init; } = new(); // 0x20C..0x314
    public IReadOnlyList<int> OverlayFieldsA { get; init; } = [];                       // 0x200..0x208
    public IReadOnlyList<int> WeaponTimingFields { get; init; } = [];                  // 0x240..0x2DC
    public IReadOnlyList<int> AimMovementTuningFields { get; init; } = [];             // 0x2E0..0x304
    public IReadOnlyList<int> OverlayDimensionFields { get; init; } = [];              // 0x318..0x32C
    public IReadOnlyList<int> BobSpreadIdleSwayAdsViewErrorFields { get; init; } = []; // 0x330..0x3C4

    // 0x3C8: alias-cell PhysCollmap pointer; PS3 WeaponDef loader calls 0x106d88.
    public XPointer<PhysCollmapAsset> PhysCollmapPointer3C8 { get; init; }
    public IReadOnlyList<int> PhysicsFieldsA { get; init; } = [];                      // 0x3CC..0x3D0
    public IReadOnlyList<int> PhysicsFieldsB { get; init; } = [];                      // 0x3D4..0x3E4
    public IReadOnlyList<int> PhysicsFieldsC { get; init; } = [];                      // 0x3E8..0x400
    public IReadOnlyList<int> PhysicsFieldsD { get; init; } = [];                      // 0x404..0x41C

    public WeaponProjectileFields Projectile { get; init; } = new(); // 0x420..0x470
    public WeaponAccuracyFields Accuracy { get; init; } = new();     // 0x50C..0x53C
    public IReadOnlyList<float> TurnSpeedAndRangeFields { get; init; } = [];           // 0x540..0x564
    public WeaponHintFields Hints { get; init; } = new();            // 0x568..0x574

    // 0x58C: XString, historical ScriptName; no clean PS3 runtime consumer yet.
    public XString ScriptNamePointer { get; init; }
    public string? ScriptName { get; init; }
    public IReadOnlyList<int> ScriptFieldsA { get; init; } = [];                       // 0x590..0x594
    public IReadOnlyList<int> ScriptFieldsB { get; init; } = [];                       // 0x598..0x5AC
    public int HitLocationField { get; init; }                                        // 0x5B0

    // 0x5B4: direct float[20], runtime-proven hit-location multiplier array.
    public XPointer<float[]> LocationDamageMultipliersPointer { get; init; }
    public IReadOnlyList<float> LocationDamageMultipliers { get; init; } = [];

    public WeaponRumbleFields Rumble { get; init; } = new();          // 0x5B8..0x5BC
    public XPointer<TracerDefAsset> TracerPointer { get; init; }      // 0x5C0
    public IReadOnlyList<int> TracerFields { get; init; } = [];                        // 0x5C4..0x5D8
    public WeaponTurretFields Turret { get; init; } = new();          // 0x5DC..0x608
    public WeaponMissileConeSoundFields MissileConeSound { get; init; } = new(); // 0x618..0x650
    public WeaponTailFlags TailFlags { get; init; } = new();          // 0x654..0x683
}
