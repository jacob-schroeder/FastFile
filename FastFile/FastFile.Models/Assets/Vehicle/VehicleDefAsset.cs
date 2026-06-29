using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Weapon;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Vehicle;

public sealed class VehicleDefAsset : BaseAsset
{
    public const int SerializedSize = 0x2D0;
    public const int ScriptStringOffset = 0x1D0;
    public const int ScriptStringCount = 4;
    public const int SurfaceSoundOffset = 0x244;
    public const int SurfaceSoundCount = 31;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public VehicleType Type { get; init; }
    public XPointer<string> UseHintStringPointer { get; init; }
    public string? UseHintString { get; init; }
    public int Health { get; init; }
    public int QuadBarrel { get; init; }
    public float TexScrollScale { get; init; }
    public float TopSpeed { get; init; }
    public float Accel { get; init; }
    public float RotRate { get; init; }
    public float RotAccel { get; init; }
    public float MaxBodyPitch { get; init; }
    public float MaxBodyRoll { get; init; }
    public VehicleFakeBodyTuning FakeBody { get; init; } = new();
    public float CollisionDamage { get; init; }
    public float CollisionSpeed { get; init; }
    public VehicleVec3 KillcamOffset { get; init; } = new();
    public int PlayerProtected { get; init; }
    public int BulletDamage { get; init; }
    public int ArmorPiercingDamage { get; init; }
    public int GrenadeDamage { get; init; }
    public int ProjectileDamage { get; init; }
    public int ProjectileSplashDamage { get; init; }
    public int HeavyExplosiveDamage { get; init; }
    public VehiclePhysDef Phys { get; init; } = new();
    public float BoostDuration { get; init; }
    public float BoostRechargeTime { get; init; }
    public float BoostAcceleration { get; init; }
    public float SuspensionTravel { get; init; }
    public float MaxSteeringAngle { get; init; }
    public float SteeringLerp { get; init; }
    public float MinSteeringScale { get; init; }
    public float MinSteeringSpeed { get; init; }
    public int CamLookEnabled { get; init; }
    public float CamLerp { get; init; }
    public float CamPitchInfluence { get; init; }
    public float CamRollInfluence { get; init; }
    public float CamFovIncrease { get; init; }
    public float CamFovOffset { get; init; }
    public float CamFovSpeed { get; init; }
    public XPointer<string> TurretWeaponNamePointer { get; init; }
    public string? TurretWeaponName { get; init; }
    public XPointer<WeaponVariantDef> TurretWeaponPointer { get; init; }
    public WeaponVariantDef? TurretWeapon { get; init; }
    public float TurretHorizSpanLeft { get; init; }
    public float TurretHorizSpanRight { get; init; }
    public float TurretVertSpanUp { get; init; }
    public float TurretVertSpanDown { get; init; }
    public float TurretRotRate { get; init; }
    public VehicleSoundAliasField TurretSpinSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField TurretStopSound { get; init; } = VehicleSoundAliasField.Empty;
    public int TrophyEnabled { get; init; }
    public float TrophyRadius { get; init; }
    public float TrophyInactiveRadius { get; init; }
    public int TrophyAmmoCount { get; init; }
    public float TrophyReloadTime { get; init; }
    public XBlockAddress? ScriptStringsAddress { get; init; }
    public IReadOnlyList<ushort> TrophyTags { get; init; } = [];
    public XPointer<MaterialAsset> CompassFriendlyIconPointer { get; init; }
    public MaterialAsset? CompassFriendlyIcon { get; init; }
    public XPointer<MaterialAsset> CompassEnemyIconPointer { get; init; }
    public MaterialAsset? CompassEnemyIcon { get; init; }
    public float CompassIconWidth { get; init; }
    public float CompassIconHeight { get; init; }
    public VehicleEngineSoundFields EngineSounds { get; init; } = new();
    public VehicleSuspensionSoundFields SuspensionSounds { get; init; } = new();
    public VehicleSoundAliasField CollisionSound { get; init; } = VehicleSoundAliasField.Empty;
    public float CollisionBlendSpeed { get; init; }
    public VehicleSoundAliasField SpeedSound { get; init; } = VehicleSoundAliasField.Empty;
    public float SpeedSoundBlendSpeed { get; init; }
    public XPointer<string> SurfaceSoundPrefixPointer { get; init; }
    public string? SurfaceSoundPrefix { get; init; }
    public IReadOnlyList<XPointer<string>> SurfaceSoundAliasPointers { get; init; } = [];
    public IReadOnlyList<string?> SurfaceSoundAliases { get; init; } = [];
    public float SurfaceSoundBlendSpeed { get; init; }
    public float SlideVolume { get; init; }
    public float SlideBlendSpeed { get; init; }
    public float InAirPitch { get; init; }
}

public sealed class VehiclePhysDef
{
    public const int OffsetInVehicleDef = 0x0A8;
    public const int SerializedSize = 0xB4;

    public int PhysicsEnabled { get; init; }
    public XPointer<string> PhysPresetNamePointer { get; init; }
    public string? PhysPresetName { get; init; }
    public XPointer<PhysPresetAsset> PhysPresetPointer { get; init; }
    public PhysPresetAsset? PhysPreset { get; init; }
    public XPointer<string> AccelGraphNamePointer { get; init; }
    public string? AccelGraphName { get; init; }
    public VehicleAxleType SteeringAxle { get; init; }
    public VehicleAxleType PowerAxle { get; init; }
    public VehicleAxleType BrakingAxle { get; init; }
    public float TopSpeed { get; init; }
    public float ReverseSpeed { get; init; }
    public float MaxVelocity { get; init; }
    public float MaxPitch { get; init; }
    public float MaxRoll { get; init; }
    public float SuspensionTravelFront { get; init; }
    public float SuspensionTravelRear { get; init; }
    public float SuspensionStrengthFront { get; init; }
    public float SuspensionDampingFront { get; init; }
    public float SuspensionStrengthRear { get; init; }
    public float SuspensionDampingRear { get; init; }
    public float FrictionBraking { get; init; }
    public float FrictionCoasting { get; init; }
    public float FrictionTopSpeed { get; init; }
    public float FrictionSide { get; init; }
    public float FrictionSideRear { get; init; }
    public float VelocityDependentSlip { get; init; }
    public float RollStability { get; init; }
    public float RollResistance { get; init; }
    public float PitchResistance { get; init; }
    public float YawResistance { get; init; }
    public float UprightStrengthPitch { get; init; }
    public float UprightStrengthRoll { get; init; }
    public float TargetAirPitch { get; init; }
    public float AirYawTorque { get; init; }
    public float AirPitchTorque { get; init; }
    public float MinimumMomentumForCollision { get; init; }
    public float CollisionLaunchForceScale { get; init; }
    public float WreckedMassScale { get; init; }
    public float WreckedBodyFriction { get; init; }
    public float MinimumJoltForNotify { get; init; }
    public float SlipThresholdFront { get; init; }
    public float SlipThresholdRear { get; init; }
    public float SlipFricScaleFront { get; init; }
    public float SlipFricScaleRear { get; init; }
    public float SlipFricRateFront { get; init; }
    public float SlipFricRateRear { get; init; }
    public float SlipYawTorque { get; init; }
}

public sealed class PhysPresetAsset : BaseAsset
{
    public const int SerializedSize = 0x2C;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int Type { get; init; }
    public float Mass { get; init; }
    public float Bounce { get; init; }
    public float Friction { get; init; }
    public float BulletForceScale { get; init; }
    public float ExplosiveForceScale { get; init; }
    public XPointer<string> SndAliasPrefixPointer { get; init; }
    public string? SndAliasPrefix { get; init; }
    public float PiecesSpreadFraction { get; init; }
    public float PiecesUpwardVelocity { get; init; }
    public byte TempDefaultToCylinder { get; init; }
    public byte PerSurfaceSndAlias { get; init; }
    public ushort Pad2A { get; init; }
}

public sealed record VehicleSoundAliasField(
    int Offset,
    XPointer<string> Pointer,
    string? Value)
{
    public static VehicleSoundAliasField Empty { get; } = new(0, default, null);
}

public sealed class VehicleFakeBodyTuning
{
    public float AccelPitch { get; init; }
    public float AccelRoll { get; init; }
    public float VelPitch { get; init; }
    public float VelRoll { get; init; }
    public float SideVelPitch { get; init; }
    public float PitchStrength { get; init; }
    public float RollStrength { get; init; }
    public float PitchDampening { get; init; }
    public float RollDampening { get; init; }
    public float BoatRockingAmplitude { get; init; }
    public float BoatRockingPeriod { get; init; }
    public float BoatRockingRotationPeriod { get; init; }
    public float BoatRockingFadeoutSpeed { get; init; }
    public float BoatBouncingMinForce { get; init; }
    public float BoatBouncingMaxForce { get; init; }
    public float BoatBouncingRate { get; init; }
    public float BoatBouncingFadeinSpeed { get; init; }
    public float BoatBouncingFadeoutSteeringAngle { get; init; }
}

public sealed class VehicleEngineSoundFields
{
    public VehicleSoundAliasField IdleLowSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField IdleHighSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField EngineLowSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField EngineHighSound { get; init; } = VehicleSoundAliasField.Empty;
    public float EngineSoundSpeed { get; init; }
    public VehicleSoundAliasField EngineStartUpSound { get; init; } = VehicleSoundAliasField.Empty;
    public float EngineStartUpLength { get; init; }
    public VehicleSoundAliasField EngineShutdownSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField EngineIdleSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField EngineSustainSound { get; init; } = VehicleSoundAliasField.Empty;
    public VehicleSoundAliasField EngineRampUpSound { get; init; } = VehicleSoundAliasField.Empty;
    public float EngineRampUpLength { get; init; }
    public VehicleSoundAliasField EngineRampDownSound { get; init; } = VehicleSoundAliasField.Empty;
    public float EngineRampDownLength { get; init; }
}

public sealed class VehicleSuspensionSoundFields
{
    public VehicleSoundAliasField SuspensionSoftSound { get; init; } = VehicleSoundAliasField.Empty;
    public float SuspensionSoftCompression { get; init; }
    public VehicleSoundAliasField SuspensionHardSound { get; init; } = VehicleSoundAliasField.Empty;
    public float SuspensionHardCompression { get; init; }
}

public sealed record VehicleVec3(float X, float Y, float Z)
{
    public VehicleVec3()
        : this(0, 0, 0)
    {
    }
}

public enum VehicleAxleType
{
    Front = 0,
    Rear = 1,
    All = 2,
    Count = 3
}

public enum VehicleType
{
    Wheels4 = 0,
    Tank = 1,
    Plane = 2,
    Boat = 3,
    Artillery = 4,
    Helicopter = 5,
    Snowmobile = 6,
    Count = 7
}
