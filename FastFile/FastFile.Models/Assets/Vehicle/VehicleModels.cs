using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Vehicle;

public enum VehicleAxleType : int
{
    Front = 0,
    Rear = 1,
    All = 2,
}

public enum VehicleType : int
{
    Wheels4 = 0,
    Tank = 1,
    Plane = 2,
    Boat = 3,
    Artillery = 4,
    Helicopter = 5,
    Snowmobile = 6,
}

public sealed partial class VehicleDef
{
    public VehicleType VehicleType { get; set; }
    public int Health { get; set; }
    public int QuadBarrel { get; set; }
    public float TexScrollScale { get; set; }
    public float TopSpeed { get; set; }
    public float Accel { get; set; }
    public float RotRate { get; set; }
    public float RotAccel { get; set; }
    public float MaxBodyPitch { get; set; }
    public float MaxBodyRoll { get; set; }
    public float FakeBodyAccelPitch { get; set; }
    public float FakeBodyAccelRoll { get; set; }
    public float FakeBodyVelPitch { get; set; }
    public float FakeBodyVelRoll { get; set; }
    public float FakeBodySideVelPitch { get; set; }
    public float FakeBodyPitchStrength { get; set; }
    public float FakeBodyRollStrength { get; set; }
    public float FakeBodyPitchDampening { get; set; }
    public float FakeBodyRollDampening { get; set; }
    public float FakeBodyBoatRockingAmplitude { get; set; }
    public float FakeBodyBoatRockingPeriod { get; set; }
    public float FakeBodyBoatRockingRotationPeriod { get; set; }
    public float FakeBodyBoatRockingFadeoutSpeed { get; set; }
    public float BoatBouncingMinForce { get; set; }
    public float BoatBouncingMaxForce { get; set; }
    public float BoatBouncingRate { get; set; }
    public float BoatBouncingFadeinSpeed { get; set; }
    public float BoatBouncingFadeoutSteeringAngle { get; set; }
    public float CollisionDamage { get; set; }
    public float CollisionSpeed { get; set; }
    public float[] KillcamOffset { get; set; } = new float[3];
    public int PlayerProtected { get; set; }
    public int BulletDamage { get; set; }
    public int ArmorPiercingDamage { get; set; }
    public int GrenadeDamage { get; set; }
    public int ProjectileDamage { get; set; }
    public int ProjectileSplashDamage { get; set; }
    public int HeavyExplosiveDamage { get; set; }
    public float BoostDuration { get; set; }
    public float BoostRechargeTime { get; set; }
    public float BoostAcceleration { get; set; }
    public float SuspensionTravel { get; set; }
    public float MaxSteeringAngle { get; set; }
    public float SteeringLerp { get; set; }
    public float MinSteeringScale { get; set; }
    public float MinSteeringSpeed { get; set; }
    public int CamLookEnabled { get; set; }
    public float CamLerp { get; set; }
    public float CamPitchInfluence { get; set; }
    public float CamRollInfluence { get; set; }
    public float CamFovIncrease { get; set; }
    public float CamFovOffset { get; set; }
    public float CamFovSpeed { get; set; }
    public float TurretHorizSpanLeft { get; set; }
    public float TurretHorizSpanRight { get; set; }
    public float TurretVertSpanUp { get; set; }
    public float TurretVertSpanDown { get; set; }
    public float TurretRotRate { get; set; }
    public int TrophyEnabled { get; set; }
    public float TrophyRadius { get; set; }
    public float TrophyInactiveRadius { get; set; }
    public int TrophyAmmoCount { get; set; }
    public float TrophyReloadTime { get; set; }
    public int CompassIconWidth { get; set; }
    public int CompassIconHeight { get; set; }
    public float EngineSndSpeed { get; set; }
    public int EngineStartUpLength { get; set; }
    public int EngineRampUpLength { get; set; }
    public int EngineRampDownLength { get; set; }
    public float SuspensionSoftCompression { get; set; }
    public float SuspensionHardCompression { get; set; }
    public float CollisionBlendSpeed { get; set; }
    public float SpeedSndBlendSpeed { get; set; }
    public float SurfaceSndBlendSpeed { get; set; }
    public float SlideVolume { get; set; }
    public float SlideBlendSpeed { get; set; }
    public float InAirPitch { get; set; }
}

public sealed class VehiclePhysDef
{
    public const int RootSize = 0xb4;

    public int Offset { get; set; }
    public int PhysicsEnabled { get; set; }
    public XPointer<string> PhysPresetName { get; set; } // Direct Pointer
    public XPointer<PhysPreset> PhysPreset { get; set; } // Alias Pointer
    public XPointer<string> AccelGraphName { get; set; } // Direct Pointer
    public VehicleAxleType SteeringAxle { get; set; }
    public VehicleAxleType PowerAxle { get; set; }
    public VehicleAxleType BrakingAxle { get; set; }
    public float TopSpeed { get; set; }
    public float ReverseSpeed { get; set; }
    public float MaxVelocity { get; set; }
    public float MaxPitch { get; set; }
    public float MaxRoll { get; set; }
    public float SuspensionTravelFront { get; set; }
    public float SuspensionTravelRear { get; set; }
    public float SuspensionStrengthFront { get; set; }
    public float SuspensionDampingFront { get; set; }
    public float SuspensionStrengthRear { get; set; }
    public float SuspensionDampingRear { get; set; }
    public float FrictionBraking { get; set; }
    public float FrictionCoasting { get; set; }
    public float FrictionTopSpeed { get; set; }
    public float FrictionSide { get; set; }
    public float FrictionSideRear { get; set; }
    public float VelocityDependentSlip { get; set; }
    public float RollStability { get; set; }
    public float RollResistance { get; set; }
    public float PitchResistance { get; set; }
    public float YawResistance { get; set; }
    public float UprightStrengthPitch { get; set; }
    public float UprightStrengthRoll { get; set; }
    public float TargetAirPitch { get; set; }
    public float AirYawTorque { get; set; }
    public float AirPitchTorque { get; set; }
    public float MinimumMomentumForCollision { get; set; }
    public float CollisionLaunchForceScale { get; set; }
    public float WreckedMassScale { get; set; }
    public float WreckedBodyFriction { get; set; }
    public float MinimumJoltForNotify { get; set; }
    public float SlipThresholdFront { get; set; }
    public float SlipThresholdRear { get; set; }
    public float SlipFricScaleFront { get; set; }
    public float SlipFricScaleRear { get; set; }
    public float SlipFricRateFront { get; set; }
    public float SlipFricRateRear { get; set; }
    public float SlipYawTorque { get; set; }
}
