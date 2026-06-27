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
    public XPointer<XString[]> LeftHandAnimationNamesPointer { get; init; }
    public IReadOnlyList<XString> LeftHandAnimationNamePointers { get; init; } = [];

    // 0x014: XString.
    public XString ModeNamePointer { get; init; }
    public string? ModeName { get; init; }

    public WeaponNoteTrackMaps NoteTrackMaps { get; init; } = new(); // 0x018..0x024

    public WeaponType WeaponType { get; init; }              // 0x02C: PS3 runtime discriminator.
    public WeaponClass WeaponClass { get; init; }            // 0x030: PS3 runtime discriminator.

    // 0x048..0x120 loader-proven pointer region.
    public IReadOnlyList<XPointer<FxEffectDefAsset>> FlashEffectPointers { get; init; } = [];
    public IReadOnlyList<XString> SoundAliasPointers { get; init; } = [];
    public XPointer<XString[]> BounceSoundPointer { get; init; }
    public IReadOnlyList<XPointer<FxEffectDefAsset>> EffectPointers { get; init; } = [];
    public IReadOnlyList<XPointer<Material.MaterialAsset>> MaterialPointers { get; init; } = [];

    // 0x1D8..0x1E8: XModel shell references.
    public XPointer<XPointer<XModelAsset>[]> WorldGunModelsPointer { get; init; }
    public IReadOnlyList<XPointer<XModelAsset>> WorldGunModelPointers { get; init; } = [];
    public IReadOnlyList<XPointer<XModelAsset>> WorldModelPointers { get; init; } = [];

    public WeaponIconPointers Icons { get; init; } = new();  // 0x1EC..0x1FC
    public WeaponOverlayFields Overlay { get; init; } = new(); // 0x20C..0x314

    // 0x3C8: PS3 loader calls Load_GfxImagePtr; old PhysCollmap name is rejected.
    public XPointer<GfxImageAsset> GfxImagePointer3C8 { get; init; }

    public WeaponProjectileFields Projectile { get; init; } = new(); // 0x420..0x470
    public WeaponAccuracyFields Accuracy { get; init; } = new();     // 0x50C..0x53C
    public WeaponHintFields Hints { get; init; } = new();            // 0x568..0x574

    // 0x58C: XString, historical ScriptName; no clean PS3 runtime consumer yet.
    public XString ScriptNamePointer { get; init; }
    public string? ScriptName { get; init; }

    // 0x5B4: direct float[20], runtime-proven hit-location multiplier array.
    public XPointer<float[]> LocationDamageMultipliersPointer { get; init; }
    public IReadOnlyList<float> LocationDamageMultipliers { get; init; } = [];

    public WeaponRumbleFields Rumble { get; init; } = new();          // 0x5B8..0x5BC
    public XPointer<TracerDefAsset> TracerPointer { get; init; }      // 0x5C0
    public WeaponTurretFields Turret { get; init; } = new();          // 0x5DC..0x608
    public WeaponMissileConeSoundFields MissileConeSound { get; init; } = new(); // 0x618..0x650
    public WeaponTailFlags TailFlags { get; init; } = new();          // 0x654..0x683
}
