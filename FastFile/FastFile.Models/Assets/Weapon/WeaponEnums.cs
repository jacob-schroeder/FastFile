namespace FastFile.Models.Assets.Weapon;

public enum WeaponType
{
    Bullet = 0,
    Grenade = 1,
    Projectile = 2,
    Binoculars = 3,
    Gas = 4,
    Bomb = 5,
    Mine = 6
}

public enum WeaponClass
{
    Rifle = 0,
    Mg = 1,
    Smg = 2,
    Spread = 3,
    Pistol = 4,
    Grenade = 5,
    RocketLauncher = 6,
    Turret = 7,
    NonPlayer = 8,
    Item = 9
}

public enum PenetrateType
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    Count = 4
}

public enum WeaponInventoryType
{
    Primary = 0,
    Offhand = 1,
    Item = 2,
    AltMode = 3,
    Exclusive = 4,
    Scavenger = 5,
    Count = 6
}

public enum WeaponFireType
{
    FullAuto = 0,
    SingleShot = 1,
    BurstFire2 = 2,
    BurstFire3 = 3,
    BurstFire4 = 4,
    DoubleBarrel = 5,
    Count = 6
}

public enum OffhandClass
{
    None = 0,
    FragGrenade = 1,
    SmokeGrenade = 2,
    FlashGrenade = 3,
    ThrowingKnife = 4,
    Other = 5,
    Count = 6
}

public enum WeaponStance
{
    Stand = 0,
    Duck = 1,
    Prone = 2,
    Count = 3
}

public enum ActiveReticleType
{
    None = 0,
    PipOnAStick = 1,
    BouncingDiamond = 2,
    Count = 3
}

public enum AmmoCounterClipType
{
    None = 0,
    Magazine = 1,
    ShortMagazine = 2,
    Shotgun = 3,
    Rocket = 4,
    BeltFed = 5,
    AltWeapon = 6,
    Count = 7
}

public enum WeaponOverlayReticle
{
    None = 0,
    Crosshair = 1,
    Count = 2
}

public enum WeaponOverlayInterface
{
    None = 0,
    Javelin = 1,
    TurretScope = 2,
    Count = 3
}

public enum WeaponProjectileExplosion
{
    Grenade = 0,
    Rocket = 1,
    Flashbang = 2,
    None = 3,
    Dud = 4,
    Smoke = 5,
    Heavy = 6,
    Count = 7
}

public enum WeaponStickiness
{
    None = 0,
    All = 1,
    AllOrient = 2,
    Ground = 3,
    GroundWithYaw = 4,
    Knife = 5,
    Count = 6
}

public enum GuidedMissileType
{
    None = 0,
    Sidewinder = 1,
    Hellfire = 2,
    Javelin = 3,
    Count = 4
}
