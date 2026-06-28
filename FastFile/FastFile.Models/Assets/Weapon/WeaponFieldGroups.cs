using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Weapon;

public sealed class WeaponNoteTrackMaps
{
    // 0x018 / 0x01C: runtime-proven sound notify remap pair.
    public XPointer<ushort[]> SoundMapKeysPointer { get; init; }
    public IReadOnlyList<ushort> SoundMapKeys { get; init; } = [];
    public XPointer<ushort[]> SoundMapValuesPointer { get; init; }
    public IReadOnlyList<ushort> SoundMapValues { get; init; } = [];

    // 0x020 / 0x024: runtime-proven rumble notify remap pair.
    public XPointer<ushort[]> RumbleMapKeysPointer { get; init; }
    public IReadOnlyList<ushort> RumbleMapKeys { get; init; } = [];
    public XPointer<ushort[]> RumbleMapValuesPointer { get; init; }
    public IReadOnlyList<ushort> RumbleMapValues { get; init; } = [];
}

public sealed class WeaponIconPointers
{
    public XPointer<Material.MaterialAsset> AmmoCounterIconPointer { get; init; } // 0x1EC
    public int AmmoCounterIconRatio { get; init; }                                // 0x1F0
    public XPointer<Material.MaterialAsset> CompassIconPointer { get; init; }     // 0x1F4
    public int CompassIconRatio { get; init; }                                    // 0x1F8
    public XPointer<Material.MaterialAsset> OverlayMaterialPointer { get; init; } // 0x1FC
}

public sealed class WeaponOverlayFields
{
    public XString OverlayReticlePointer { get; init; }                           // 0x20C
    public string? OverlayReticle { get; init; }
    public int OverlayReticleCacheIndex { get; init; }                            // 0x210
    public XString OverlayInterfacePointer { get; init; }                         // 0x214
    public string? OverlayInterface { get; init; }
    public int OverlayInterfaceCacheIndex { get; init; }                          // 0x218
    public IReadOnlyList<int> OverlayFieldsB { get; init; } = [];                 // 0x21C..0x220
    public XString AlternateModeNamePointer { get; init; }                        // 0x224
    public string? AlternateModeName { get; init; }
    public int AlternateModeCacheIndex { get; init; }                             // 0x228
    public IReadOnlyList<int> ModeFields { get; init; } = [];                     // 0x22C..0x23C
    public IReadOnlyList<XPointer<Material.MaterialAsset>> OverlayMaterials { get; init; } = []; // 0x308..0x314
}

public sealed class WeaponProjectileFields
{
    public XPointer<XModelAsset> ModelPointer { get; init; }                      // 0x420
    public int ModelField { get; init; }                                          // 0x424
    public IReadOnlyList<XPointer<FxEffectDefAsset>> EffectPointers { get; init; } = []; // 0x428 / 0x42C
    public IReadOnlyList<XString> SoundAliasPointers { get; init; } = [];         // 0x430 / 0x434
    public IReadOnlyList<string?> SoundAliasNames { get; init; } = [];
    public IReadOnlyList<int> ProjectileFieldsA { get; init; } = [];              // 0x438..0x440

    // 0x444 / 0x448: loader-proven float[31], names are Xbox-correlated/open.
    public XPointer<float[]> ParallelBouncePointer { get; init; }
    public IReadOnlyList<float> ParallelBounce { get; init; } = [];
    public XPointer<float[]> PerpendicularBouncePointer { get; init; }
    public IReadOnlyList<float> PerpendicularBounce { get; init; } = [];

    public IReadOnlyList<XPointer<FxEffectDefAsset>> ImpactEffectPointers { get; init; } = []; // 0x44C / 0x450
    public IReadOnlyList<int> ImpactFieldsA { get; init; } = [];                  // 0x454..0x45C
    public int ImpactFieldB { get; init; }                                        // 0x460
    public IReadOnlyList<int> ImpactFieldsC { get; init; } = [];                  // 0x464..0x468
    public XPointer<FxEffectDefAsset> ViewShellEjectEffectPointer { get; init; }   // 0x46C
    public XString ShellEjectSoundPointer { get; init; }                          // 0x470
    public string? ShellEjectSound { get; init; }
    public IReadOnlyList<int> ShellEjectFields { get; init; } = [];               // 0x474..0x47C
    public IReadOnlyList<int> AdsHipGunKickAiDistanceFields { get; init; } = [];  // 0x480..0x508
}

public sealed class WeaponAccuracyFields
{
    public XString GraphName0Pointer { get; init; }                               // 0x50C
    public string? GraphName0 { get; init; }
    public XString GraphName1Pointer { get; init; }                               // 0x510
    public string? GraphName1 { get; init; }
    public XPointer<Math.Vec2[]> GraphKnotsPointer { get; init; }                 // 0x514
    public IReadOnlyList<Math.Vec2> GraphKnots { get; init; } = [];
    public XPointer<Math.Vec2[]> OriginalGraphKnotsPointer { get; init; }         // 0x518
    public IReadOnlyList<Math.Vec2> OriginalGraphKnots { get; init; } = [];

    // 0x51C / 0x51E are not the loader counts for +0x514/+0x518.
    public ushort LocalGraphKnotCount { get; init; }
    public ushort LocalOriginalGraphKnotCount { get; init; }

    // 0x520: runtime-proven animation notify comparison scalar; exact source name still open.
    public int AnimationNotifyComparison { get; init; }
    public float LeftArc { get; init; }                                           // 0x524
    public float RightArc { get; init; }                                          // 0x528
    public float TopArc { get; init; }                                            // 0x52C
    public float BottomArc { get; init; }                                         // 0x530
    public float Accuracy { get; init; }                                          // 0x534
    public float AiSpread { get; init; }                                          // 0x538
    public float PlayerSpread { get; init; }                                      // 0x53C
}

public sealed class WeaponHintFields
{
    // 0x568: loader-proven XString; historical UseHintString, no clean PS3 runtime consumer yet.
    public XString UseHintStringPointer { get; init; }
    public string? UseHintString { get; init; }

    // 0x56C / 0x574: runtime-proven target/cursor hint behavior.
    public XString DropHintStringPointer { get; init; }
    public string? DropHintString { get; init; }
    public int Unknown570 { get; init; }                                          // 0x570
    public int DropHintStringState { get; init; }                                 // 0x574
    public IReadOnlyList<int> HintFieldsB { get; init; } = [];                    // 0x578..0x588
}

public sealed class WeaponRumbleFields
{
    // 0x5B8 / 0x5BC: loader-proven XStrings; runtime consumer still open.
    public XString FireRumblePointer { get; init; }
    public string? FireRumble { get; init; }
    public XString MeleeImpactRumblePointer { get; init; }
    public string? MeleeImpactRumble { get; init; }
}

public sealed class WeaponTurretFields
{
    public XString OverheatSoundPointer { get; init; }                            // 0x5DC
    public string? OverheatSound { get; init; }
    public XPointer<FxEffectDefAsset> OverheatEffectPointer { get; init; }        // 0x5E0
    public XString BarrelSpinRumblePointer { get; init; }                         // 0x5E4
    public string? BarrelSpinRumble { get; init; }
    public IReadOnlyList<int> TurretFields { get; init; } = [];                   // 0x5E8..0x5F0
    public XString BarrelSpinMaxSoundPointer { get; init; }                       // 0x5F4
    public string? BarrelSpinMaxSound { get; init; }
    public XPointer<XString[]> BarrelSpinUpSoundPointers { get; init; }           // 0x5F8, count 4
    public IReadOnlyList<XString> BarrelSpinUpSounds { get; init; } = [];
    public IReadOnlyList<string?> BarrelSpinUpSoundNames { get; init; } = [];
    public IReadOnlyList<int> Unknown5FCTo604 { get; init; } = [];
    public XPointer<XString[]> BarrelSpinDownSoundPointers { get; init; }         // 0x608, count 4
    public IReadOnlyList<XString> BarrelSpinDownSounds { get; init; } = [];
    public IReadOnlyList<string?> BarrelSpinDownSoundNames { get; init; } = [];
    public IReadOnlyList<int> Unknown60CTo614 { get; init; } = [];
}

public sealed class WeaponMissileConeSoundFields
{
    public XString AliasPointer { get; init; }                                    // 0x618
    public string? Alias { get; init; }
    public XString AliasAtBasePointer { get; init; }                              // 0x61C
    public string? AliasAtBase { get; init; }

    // 0x620..0x650: loader-copied scalar tail; historical names are Xbox-correlated/open.
    public float RadiusAtTop { get; init; }
    public float RadiusAtBase { get; init; }
    public float Height { get; init; }
    public float OriginOffset { get; init; }
    public float VolumeScaleAtCore { get; init; }
    public float VolumeScaleAtEdge { get; init; }
    public float VolumeScaleCoreSize { get; init; }
    public float PitchAtTop { get; init; }
    public float PitchAtBottom { get; init; }
    public float PitchTopSize { get; init; }
    public float PitchBottomSize { get; init; }
    public float CrossfadeTopSize { get; init; }
    public float CrossfadeBottomSize { get; init; }
}
