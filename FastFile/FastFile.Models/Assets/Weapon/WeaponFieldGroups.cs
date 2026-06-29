using FastFile.Models.Math;
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
    public XPointer<Material.MaterialAsset> HudIconPointer { get; init; }         // 0x1EC
    public int HudIconRatio { get; init; }                                        // 0x1F0
    public XPointer<Material.MaterialAsset> PickupIconPointer { get; init; }      // 0x1F4
    public int PickupIconRatio { get; init; }                                     // 0x1F8
    public XPointer<Material.MaterialAsset> AmmoCounterIconPointer { get; init; } // 0x1FC
    public int AmmoCounterIconRatio { get; init; }                                // 0x200
    public AmmoCounterClipType AmmoCounterClip { get; init; }                     // 0x204
    public int StartAmmo { get; init; }                                           // 0x208
}

public sealed class WeaponReticleFields
{
    public int CenterSize { get; init; }                                           // 0x128
    public int SideSize { get; init; }                                             // 0x12C
    public int MinOffset { get; init; }                                            // 0x130
    public ActiveReticleType ActiveType { get; init; }                             // 0x134
}

public sealed class WeaponViewMovementFields
{
    public Vec3 StandMove { get; init; }                                           // 0x138
    public Vec3 StandRotation { get; init; }                                       // 0x144
    public Vec3 StrafeMove { get; init; }                                          // 0x150
    public Vec3 StrafeRotation { get; init; }                                      // 0x15C
    public Vec3 DuckedOffset { get; init; }                                        // 0x168
    public Vec3 DuckedMove { get; init; }                                          // 0x174
    public Vec3 DuckedRotation { get; init; }                                      // 0x180
    public Vec3 ProneOffset { get; init; }                                         // 0x18C
    public Vec3 ProneMove { get; init; }                                           // 0x198
    public Vec3 ProneRotation { get; init; }                                       // 0x1A4
}

public sealed class WeaponPositionalMovementFields
{
    public float PositionMoveRate { get; init; }                                   // 0x1B0
    public float PositionProneMoveRate { get; init; }                              // 0x1B4
    public float StandMoveMinSpeed { get; init; }                                  // 0x1B8
    public float DuckedMoveMinSpeed { get; init; }                                 // 0x1BC
    public float ProneMoveMinSpeed { get; init; }                                  // 0x1C0
    public float PositionRotationRate { get; init; }                               // 0x1C4
    public float PositionProneRotationRate { get; init; }                          // 0x1C8
    public float StandRotationMinSpeed { get; init; }                              // 0x1CC
    public float DuckedRotationMinSpeed { get; init; }                             // 0x1D0
    public float ProneRotationMinSpeed { get; init; }                              // 0x1D4
}

public sealed class WeaponOverlayFields
{
    public IReadOnlyList<XPointer<Material.MaterialAsset>> OverlayMaterials { get; init; } = []; // 0x308..0x314
    public WeaponOverlayReticle Reticle { get; init; }                            // 0x318
    public WeaponOverlayInterface Interface { get; init; }                        // 0x31C
    public int Width { get; init; }                                               // 0x320
    public int Height { get; init; }                                              // 0x324
    public int WidthSplitscreen { get; init; }                                    // 0x328
    public int HeightSplitscreen { get; init; }                                   // 0x32C
}

public sealed class WeaponAmmoFields
{
    public XString AmmoNamePointer { get; init; }                                 // 0x20C
    public string? AmmoName { get; init; }
    public int AmmoIndex { get; init; }                                           // 0x210
    public XString ClipNamePointer { get; init; }                                 // 0x214
    public string? ClipName { get; init; }
    public int ClipIndex { get; init; }                                           // 0x218
    public int MaxAmmo { get; init; }                                             // 0x21C
    public int ShotCount { get; init; }                                           // 0x220
    public XString SharedAmmoCapNamePointer { get; init; }                        // 0x224
    public string? SharedAmmoCapName { get; init; }
    public int SharedAmmoCapIndex { get; init; }                                  // 0x228
    public int SharedAmmoCap { get; init; }                                       // 0x22C
    public int Damage { get; init; }                                              // 0x230
    public int PlayerDamage { get; init; }                                        // 0x234
    public int MeleeDamage { get; init; }                                         // 0x238
    public int DamageType { get; init; }                                          // 0x23C
}

public sealed class WeaponTimingFields
{
    public int FireDelay { get; init; }                                           // 0x240
    public int MeleeDelay { get; init; }                                          // 0x244
    public int MeleeChargeDelay { get; init; }                                    // 0x248
    public int DetonateDelay { get; init; }                                       // 0x24C
    public int RechamberTime { get; init; }                                       // 0x250
    public int RechamberTimeOneHanded { get; init; }                              // 0x254
    public int RechamberBoltTime { get; init; }                                   // 0x258
    public int HoldFireTime { get; init; }                                        // 0x25C
    public int DetonateTime { get; init; }                                        // 0x260
    public int MeleeTime { get; init; }                                           // 0x264
    public int MeleeChargeTime { get; init; }                                     // 0x268
    public int ReloadTime { get; init; }                                          // 0x26C
    public int ReloadShowRocketTime { get; init; }                                // 0x270
    public int ReloadEmptyTime { get; init; }                                     // 0x274
    public int ReloadAddTime { get; init; }                                       // 0x278
    public int ReloadStartTime { get; init; }                                     // 0x27C
    public int ReloadStartAddTime { get; init; }                                  // 0x280
    public int ReloadEndTime { get; init; }                                       // 0x284
    public int DropTime { get; init; }                                            // 0x288
    public int RaiseTime { get; init; }                                           // 0x28C
    public int AltDropTime { get; init; }                                         // 0x290
    public int QuickDropTime { get; init; }                                       // 0x294
    public int QuickRaiseTime { get; init; }                                      // 0x298
    public int BreachRaiseTime { get; init; }                                     // 0x29C
    public int EmptyRaiseTime { get; init; }                                      // 0x2A0
    public int EmptyDropTime { get; init; }                                       // 0x2A4
    public int SprintInTime { get; init; }                                        // 0x2A8
    public int SprintLoopTime { get; init; }                                      // 0x2AC
    public int SprintOutTime { get; init; }                                       // 0x2B0
    public int StunnedTimeBegin { get; init; }                                    // 0x2B4
    public int StunnedTimeLoop { get; init; }                                     // 0x2B8
    public int StunnedTimeEnd { get; init; }                                      // 0x2BC
    public int NightVisionWearTime { get; init; }                                 // 0x2C0
    public int NightVisionWearTimeFadeOutEnd { get; init; }                       // 0x2C4
    public int NightVisionWearTimePowerUp { get; init; }                          // 0x2C8
    public int NightVisionRemoveTime { get; init; }                               // 0x2CC
    public int NightVisionRemoveTimePowerDown { get; init; }                      // 0x2D0
    public int NightVisionRemoveTimeFadeInStart { get; init; }                    // 0x2D4
    public int FuseTime { get; init; }                                            // 0x2D8
    public int AiFuseTime { get; init; }                                          // 0x2DC
}

public sealed class WeaponAimMovementTuningFields
{
    public float AutoAimRange { get; init; }                                      // 0x2E0
    public float AimAssistRange { get; init; }                                    // 0x2E4
    public float AimAssistRangeAds { get; init; }                                 // 0x2E8
    public float AimPadding { get; init; }                                        // 0x2EC
    public float EnemyCrosshairRange { get; init; }                               // 0x2F0
    public float MoveSpeedScale { get; init; }                                    // 0x2F4
    public float AdsMoveSpeedScale { get; init; }                                 // 0x2F8
    public float SprintDurationScale { get; init; }                               // 0x2FC
    public float AdsZoomInFraction { get; init; }                                 // 0x300
    public float AdsZoomOutFraction { get; init; }                                // 0x304
}

public sealed class WeaponAdsViewAndSpreadFields
{
    public float AdsBobFactor { get; init; }                                      // 0x330
    public float AdsViewBobMultiplier { get; init; }                              // 0x334
    public float HipSpreadStandMin { get; init; }                                 // 0x338
    public float HipSpreadDuckedMin { get; init; }                                // 0x33C
    public float HipSpreadProneMin { get; init; }                                 // 0x340
    public float HipSpreadStandMax { get; init; }                                 // 0x344
    public float HipSpreadDuckedMax { get; init; }                                // 0x348
    public float HipSpreadProneMax { get; init; }                                 // 0x34C
    public float HipSpreadDecayRate { get; init; }                                // 0x350
    public float HipSpreadFireAdd { get; init; }                                  // 0x354
    public float HipSpreadTurnAdd { get; init; }                                  // 0x358
    public float HipSpreadMoveAdd { get; init; }                                  // 0x35C
    public float HipSpreadDuckedDecay { get; init; }                              // 0x360
    public float HipSpreadProneDecay { get; init; }                               // 0x364
    public float HipReticleSidePosition { get; init; }                            // 0x368
    public float AdsIdleAmount { get; init; }                                     // 0x36C
    public float HipIdleAmount { get; init; }                                     // 0x370
    public float AdsIdleSpeed { get; init; }                                      // 0x374
    public float HipIdleSpeed { get; init; }                                      // 0x378
    public float IdleCrouchFactor { get; init; }                                  // 0x37C
    public float IdleProneFactor { get; init; }                                   // 0x380
    public float GunMaxPitch { get; init; }                                       // 0x384
    public float GunMaxYaw { get; init; }                                         // 0x388
    public float SwayMaxAngle { get; init; }                                      // 0x38C
    public float SwayLerpSpeed { get; init; }                                     // 0x390
    public float SwayPitchScale { get; init; }                                    // 0x394
    public float SwayYawScale { get; init; }                                      // 0x398
    public float SwayHorizontalScale { get; init; }                               // 0x39C
    public float SwayVerticalScale { get; init; }                                 // 0x3A0
    public float SwayShellShockScale { get; init; }                               // 0x3A4
    public float AdsSwayMaxAngle { get; init; }                                   // 0x3A8
    public float AdsSwayLerpSpeed { get; init; }                                  // 0x3AC
    public float AdsSwayPitchScale { get; init; }                                 // 0x3B0
    public float AdsSwayYawScale { get; init; }                                   // 0x3B4
    public float AdsSwayHorizontalScale { get; init; }                            // 0x3B8
    public float AdsSwayVerticalScale { get; init; }                              // 0x3BC
    public float AdsViewErrorMin { get; init; }                                   // 0x3C0
    public float AdsViewErrorMax { get; init; }                                   // 0x3C4
}

public sealed class WeaponPhysicsFields
{
    public float DualWieldViewModelOffset { get; init; }                          // 0x3CC
    public int KillIconRatio { get; init; }                                       // 0x3D0
    public int ReloadAmmoAdd { get; init; }                                       // 0x3D4
    public int ReloadStartAdd { get; init; }                                      // 0x3D8
    public int AmmoDropStockMin { get; init; }                                    // 0x3DC
    public float AmmoDropClipPercentMin { get; init; }                            // 0x3E0
    public float AmmoDropClipPercentMax { get; init; }                            // 0x3E4
    public int ExplosionRadius { get; init; }                                     // 0x3E8
    public int ExplosionRadiusMin { get; init; }                                  // 0x3EC
    public int ExplosionInnerDamage { get; init; }                                // 0x3F0
    public int ExplosionOuterDamage { get; init; }                                // 0x3F4
    public float DamageConeAngle { get; init; }                                   // 0x3F8
    public float BulletExplosionDamageMultiplier { get; init; }                   // 0x3FC
    public float BulletExplosionRadiusMultiplier { get; init; }                   // 0x400
    public int ProjectileSpeed { get; init; }                                     // 0x404
    public int ProjectileSpeedUp { get; init; }                                   // 0x408
    public int ProjectileSpeedForward { get; init; }                              // 0x40C
    public int ProjectileActivateDistance { get; init; }                          // 0x410
    public int ProjectileLifetime { get; init; }                                  // 0x414
    public int TimeToAccelerate { get; init; }                                    // 0x418
    public float ProjectileCurvature { get; init; }                               // 0x41C
}

public sealed class WeaponProjectileFields
{
    public XPointer<XModelAsset> ModelPointer { get; init; }                      // 0x420
    public WeaponProjectileExplosion Explosion { get; init; }                     // 0x424
    public XPointer<FxEffectDefAsset> ExplosionEffectPointer { get; init; }       // 0x428
    public XPointer<FxEffectDefAsset> DudEffectPointer { get; init; }             // 0x42C
    public XString ExplosionSoundPointer { get; init; }                           // 0x430
    public string? ExplosionSound { get; init; }
    public XString DudSoundPointer { get; init; }                                 // 0x434
    public string? DudSound { get; init; }
    public WeaponStickiness Stickiness { get; init; }                             // 0x438
    public int LowAmmoWarningThreshold { get; init; }                             // 0x43C
    public float RicochetChance { get; init; }                                    // 0x440

    // 0x444 / 0x448: loader-proven float[31], names are Xbox-correlated/open.
    public XPointer<float[]> ParallelBouncePointer { get; init; }
    public IReadOnlyList<float> ParallelBounce { get; init; } = [];
    public XPointer<float[]> PerpendicularBouncePointer { get; init; }
    public IReadOnlyList<float> PerpendicularBounce { get; init; } = [];

    public XPointer<FxEffectDefAsset> TrailEffectPointer { get; init; }           // 0x44C
    public XPointer<FxEffectDefAsset> BeaconEffectPointer { get; init; }          // 0x450
    public Vec3 ProjectileColor { get; init; }                                    // 0x454..0x45C
    public GuidedMissileType GuidedMissileType { get; init; }                     // 0x460
    public float MaxSteeringAcceleration { get; init; }                           // 0x464
    public int IgnitionDelay { get; init; }                                       // 0x468
    public XPointer<FxEffectDefAsset> IgnitionEffectPointer { get; init; }        // 0x46C
    public XString IgnitionSoundPointer { get; init; }                            // 0x470
    public string? IgnitionSound { get; init; }
    public float AdsAimPitch { get; init; }                                       // 0x474
    public float AdsCrosshairInFraction { get; init; }                            // 0x478
    public float AdsCrosshairOutFraction { get; init; }                           // 0x47C
    public WeaponGunKickAndDistanceFields GunKickAndDistance { get; init; } = new(); // 0x480..0x508
}

public sealed class WeaponGunKickAndDistanceFields
{
    public int AdsGunKickReducedKickBullets { get; init; }                        // 0x480
    public float AdsGunKickReducedKickPercent { get; init; }                      // 0x484
    public float AdsGunKickPitchMin { get; init; }                                // 0x488
    public float AdsGunKickPitchMax { get; init; }                                // 0x48C
    public float AdsGunKickYawMin { get; init; }                                  // 0x490
    public float AdsGunKickYawMax { get; init; }                                  // 0x494
    public float AdsGunKickAcceleration { get; init; }                            // 0x498
    public float AdsGunKickSpeedMax { get; init; }                                // 0x49C
    public float AdsGunKickSpeedDecay { get; init; }                              // 0x4A0
    public float AdsGunKickStaticDecay { get; init; }                             // 0x4A4
    public float AdsViewKickPitchMin { get; init; }                               // 0x4A8
    public float AdsViewKickPitchMax { get; init; }                               // 0x4AC
    public float AdsViewKickYawMin { get; init; }                                 // 0x4B0
    public float AdsViewKickYawMax { get; init; }                                 // 0x4B4
    public float AdsViewScatterMin { get; init; }                                 // 0x4B8
    public float AdsViewScatterMax { get; init; }                                 // 0x4BC
    public float AdsSpread { get; init; }                                         // 0x4C0
    public int HipGunKickReducedKickBullets { get; init; }                        // 0x4C4
    public float HipGunKickReducedKickPercent { get; init; }                      // 0x4C8
    public float HipGunKickPitchMin { get; init; }                                // 0x4CC
    public float HipGunKickPitchMax { get; init; }                                // 0x4D0
    public float HipGunKickYawMin { get; init; }                                  // 0x4D4
    public float HipGunKickYawMax { get; init; }                                  // 0x4D8
    public float HipGunKickAcceleration { get; init; }                            // 0x4DC
    public float HipGunKickSpeedMax { get; init; }                                // 0x4E0
    public float HipGunKickSpeedDecay { get; init; }                              // 0x4E4
    public float HipGunKickStaticDecay { get; init; }                             // 0x4E8
    public float HipViewKickPitchMin { get; init; }                               // 0x4EC
    public float HipViewKickPitchMax { get; init; }                               // 0x4F0
    public float HipViewKickYawMin { get; init; }                                 // 0x4F4
    public float HipViewKickYawMax { get; init; }                                 // 0x4F8
    public float HipViewScatterMin { get; init; }                                 // 0x4FC
    public float HipViewScatterMax { get; init; }                                 // 0x500
    public float FightDistance { get; init; }                                     // 0x504
    public float MaxDistance { get; init; }                                       // 0x508
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

public sealed class WeaponTurnSpeedAndRangeFields
{
    public float MinTurnSpeed { get; init; }                                      // 0x540
    public float MaxTurnSpeed { get; init; }                                      // 0x544
    public float PitchConvergenceTime { get; init; }                              // 0x548
    public float YawConvergenceTime { get; init; }                                // 0x54C
    public float SuppressTime { get; init; }                                      // 0x550
    public float MaxRange { get; init; }                                          // 0x554
    public float AnimationHorizontalRotateIncrement { get; init; }                // 0x558
    public float PlayerPositionDistance { get; init; }                            // 0x55C
    public float ScanSpeed { get; init; }                                         // 0x560
    public float ScanAcceleration { get; init; }                                  // 0x564
}

public sealed class WeaponHintFields
{
    // 0x568: loader-proven XString; historical UseHintString, no clean PS3 runtime consumer yet.
    public XString UseHintStringPointer { get; init; }
    public string? UseHintString { get; init; }

    // 0x56C..0x574: runtime-proven drop hint behavior; Xbox-correlated index pair.
    public XString DropHintStringPointer { get; init; }
    public string? DropHintString { get; init; }
    public int UseHintStringIndex { get; init; }                                  // 0x570
    public int DropHintStringIndex { get; init; }                                 // 0x574
    public float HorizontalViewJitter { get; init; }                              // 0x578
    public float VerticalViewJitter { get; init; }                                // 0x57C
    public float ScanSpeed { get; init; }                                         // 0x580
    public float ScanAcceleration { get; init; }                                  // 0x584
    public int ScanPauseTime { get; init; }                                       // 0x588
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
    public float BarrelSpinSpeed { get; init; }                                   // 0x5E8
    public float BarrelSpinUpTime { get; init; }                                  // 0x5EC
    public float BarrelSpinDownTime { get; init; }                                // 0x5F0
    public XString BarrelSpinMaxSoundPointer { get; init; }                       // 0x5F4
    public string? BarrelSpinMaxSound { get; init; }
    public IReadOnlyList<XString> BarrelSpinUpSoundPointers { get; init; } = [];  // 0x5F8..0x604, count 4
    public IReadOnlyList<string?> BarrelSpinUpSoundNames { get; init; } = [];
    public IReadOnlyList<XString> BarrelSpinDownSoundPointers { get; init; } = [];// 0x608..0x614, count 4
    public IReadOnlyList<string?> BarrelSpinDownSoundNames { get; init; } = [];
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
