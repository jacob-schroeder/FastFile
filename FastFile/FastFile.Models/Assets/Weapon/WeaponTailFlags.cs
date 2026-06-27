namespace FastFile.Models.Assets.Weapon;

public sealed class WeaponTailFlags
{
    public byte SharedAmmo { get; init; }                         // 0x654: setup/grouping flag behavior proven.
    public byte LockonSupported { get; init; }                    // 0x655: state transition use proven; name correlated.
    public byte RequireLockonToFire { get; init; }                // 0x656: state transition use proven; name correlated.
    public byte BigExplosion { get; init; }                       // 0x657: loader-copied; name correlated.
    public byte NoAdsWhenMagEmpty { get; init; }                  // 0x658
    public byte AvoidDropCleanup { get; init; }                   // 0x659
    public byte InheritsPerks { get; init; }                      // 0x65A
    public byte CrosshairColorChange { get; init; }               // 0x65B
    public byte RifleBullet { get; init; }                        // 0x65C: bullet trace/damage behavior proven.
    public byte ArmorPiercing { get; init; }                      // 0x65D: damage behavior proven.
    public byte BoltAction { get; init; }                         // 0x65E
    public byte AimDownSight { get; init; }                       // 0x65F: ADS-capability predicate proven.
    public byte RechamberWhileAds { get; init; }                  // 0x660
    public byte BulletExplosiveDamage { get; init; }              // 0x661
    public byte CookOffHold { get; init; }                        // 0x662
    public byte ClipOnly { get; init; }                           // 0x663: ammo/clip predicate proven.
    public byte NoAmmoPickup { get; init; }                       // 0x664: inverted runtime predicate proven.
    public byte AdsFireOnly { get; init; }                        // 0x665: ADS/fire-state behavior proven.
    public byte CancelAutoHolsterWhenEmpty { get; init; }         // 0x666
    public byte DisableSwitchToWhenEmpty { get; init; }           // 0x667
    public byte SuppressAmmoReserveDisplay { get; init; }         // 0x668
    public byte LaserSightDuringNightvision { get; init; }        // 0x669
    public byte MarkableViewmodel { get; init; }                  // 0x66A
    public byte NoDualWield { get; init; }                        // 0x66B: dual-wield restriction proven.
    public byte FlipKillIcon { get; init; }                       // 0x66C
    public byte NoPartialReload { get; init; }                    // 0x66D
    public byte SegmentedReload { get; init; }                    // 0x66E: reload-state behavior proven.
    public byte BlocksProne { get; init; }                        // 0x66F: stance/prone predicate proven.
    public byte Silenced { get; init; }                           // 0x670
    public byte IsRollingGrenade { get; init; }                   // 0x671
    public byte ProjectileExplosionEffectForceNormalUp { get; init; } // 0x672
    public byte ProjectileImpactExplode { get; init; }            // 0x673
    public byte StickToPlayers { get; init; }                     // 0x674
    public byte HasDetonator { get; init; }                       // 0x675: detonator/offhand behavior proven.
    public byte DisableFiring { get; init; }                      // 0x676
    public byte TimedDetonation { get; init; }                    // 0x677
    public byte Rotate { get; init; }                             // 0x678
    public byte HoldButtonToThrow { get; init; }                  // 0x679
    public byte FreezeMovementWhenFiring { get; init; }           // 0x67A
    public byte ThermalScope { get; init; }                       // 0x67B: client-view predicate proven.
    public byte AltModeSameWeapon { get; init; }                  // 0x67C: alternate-mode behavior proven.
    public byte TurretBarrelSpinEnabled { get; init; }            // 0x67D: turret barrel spin gate proven.
    public byte MissileConeSoundEnabled { get; init; }            // 0x67E: no clean PS3 MP consumer yet.
    public byte MissileConeSoundPitchShiftEnabled { get; init; }  // 0x67F: no clean PS3 MP consumer yet.
    public byte MissileConeSoundCrossfadeEnabled { get; init; }   // 0x680: no clean PS3 MP consumer yet.
    public byte OffhandHoldIsCancelable { get; init; }            // 0x681
    public ushort ReservedPadding { get; init; }                  // 0x682..0x683: final tail padding; zero in checked MP fastfiles, no PS3 MP/SP consumer found.
}
