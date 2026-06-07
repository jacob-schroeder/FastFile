using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

internal static class VehicleReader
{
    public static VehicleDef Read(ref XFileReadContext context)
    {
        var start = context.Position;
        var asset = new VehicleDef
        {
            Offset = start,
            RawRoot = context.Span.Slice(start, Math.Min(VehicleDef.RootSize, Math.Max(0, context.Span.Length - start))).ToArray(),
            NamePtr = context.ReadDirectPointer<string>("VehicleDef+0x000.Name"),
        };

        var typeRaw = context.ReadInt32();
        asset.VehicleType = (VehicleType)typeRaw;
        XFileReadValidator.ValidateCount(
            ref context,
            "VehicleDef+0x004.Type",
            typeRaw,
            (int)VehicleType.Wheels4,
            (int)VehicleType.Snowmobile,
            "PC VehicleType names are used only after EBOOT 0x00115678 confirms the field offset.");

        asset.UseHintString = context.ReadDirectPointer<string>("VehicleDef+0x008.UseHintString");
        asset.Health = context.ReadInt32();
        asset.QuadBarrel = context.ReadInt32();
        asset.TexScrollScale = context.ReadFloat();
        asset.TopSpeed = context.ReadFloat();
        asset.Accel = context.ReadFloat();
        asset.RotRate = context.ReadFloat();
        asset.RotAccel = context.ReadFloat();
        asset.MaxBodyPitch = context.ReadFloat();
        asset.MaxBodyRoll = context.ReadFloat();
        asset.FakeBodyAccelPitch = context.ReadFloat();
        asset.FakeBodyAccelRoll = context.ReadFloat();
        asset.FakeBodyVelPitch = context.ReadFloat();
        asset.FakeBodyVelRoll = context.ReadFloat();
        asset.FakeBodySideVelPitch = context.ReadFloat();
        asset.FakeBodyPitchStrength = context.ReadFloat();
        asset.FakeBodyRollStrength = context.ReadFloat();
        asset.FakeBodyPitchDampening = context.ReadFloat();
        asset.FakeBodyRollDampening = context.ReadFloat();
        asset.FakeBodyBoatRockingAmplitude = context.ReadFloat();
        asset.FakeBodyBoatRockingPeriod = context.ReadFloat();
        asset.FakeBodyBoatRockingRotationPeriod = context.ReadFloat();
        asset.FakeBodyBoatRockingFadeoutSpeed = context.ReadFloat();
        asset.BoatBouncingMinForce = context.ReadFloat();
        asset.BoatBouncingMaxForce = context.ReadFloat();
        asset.BoatBouncingRate = context.ReadFloat();
        asset.BoatBouncingFadeinSpeed = context.ReadFloat();
        asset.BoatBouncingFadeoutSteeringAngle = context.ReadFloat();
        asset.CollisionDamage = context.ReadFloat();
        asset.CollisionSpeed = context.ReadFloat();
        for (var i = 0; i < asset.KillcamOffset.Length; i++)
            asset.KillcamOffset[i] = context.ReadFloat();
        asset.PlayerProtected = context.ReadInt32();
        asset.BulletDamage = context.ReadInt32();
        asset.ArmorPiercingDamage = context.ReadInt32();
        asset.GrenadeDamage = context.ReadInt32();
        asset.ProjectileDamage = context.ReadInt32();
        asset.ProjectileSplashDamage = context.ReadInt32();
        asset.HeavyExplosiveDamage = context.ReadInt32();

        EnsureRootOffset(ref context, start, 0x0a8);
        asset.VehPhysDef = ReadVehiclePhysDef(ref context);
        EnsureRootOffset(ref context, start, 0x15c);

        asset.BoostDuration = context.ReadFloat();
        asset.BoostRechargeTime = context.ReadFloat();
        asset.BoostAcceleration = context.ReadFloat();
        asset.SuspensionTravel = context.ReadFloat();
        asset.MaxSteeringAngle = context.ReadFloat();
        asset.SteeringLerp = context.ReadFloat();
        asset.MinSteeringScale = context.ReadFloat();
        asset.MinSteeringSpeed = context.ReadFloat();
        asset.CamLookEnabled = context.ReadInt32();
        asset.CamLerp = context.ReadFloat();
        asset.CamPitchInfluence = context.ReadFloat();
        asset.CamRollInfluence = context.ReadFloat();
        asset.CamFovIncrease = context.ReadFloat();
        asset.CamFovOffset = context.ReadFloat();
        asset.CamFovSpeed = context.ReadFloat();

        asset.TurretWeaponName = context.ReadDirectPointer<string>("VehicleDef+0x198.TurretWeaponName");
        asset.TurretWeapon = context.ReadAliasPointer<WeaponVariantDef>("VehicleDef+0x19C.TurretWeapon");
        asset.TurretHorizSpanLeft = context.ReadFloat();
        asset.TurretHorizSpanRight = context.ReadFloat();
        asset.TurretVertSpanUp = context.ReadFloat();
        asset.TurretVertSpanDown = context.ReadFloat();
        asset.TurretRotRate = context.ReadFloat();
        asset.TurretSpinSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1B4.TurretSpinSnd");
        asset.TurretStopSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1B8.TurretStopSnd");
        asset.TrophyEnabled = context.ReadInt32();
        asset.TrophyRadius = context.ReadFloat();
        asset.TrophyInactiveRadius = context.ReadFloat();
        asset.TrophyAmmoCount = context.ReadInt32();
        asset.TrophyReloadTime = context.ReadFloat();
        for (var i = 0; i < asset.TrophyTags.Length; i++)
            asset.TrophyTags[i] = context.ReadUInt16();

        asset.CompassFriendlyIcon = context.ReadAliasPointer<MaterialAsset>("VehicleDef+0x1D8.CompassFriendlyIcon");
        asset.CompassEnemyIcon = context.ReadAliasPointer<MaterialAsset>("VehicleDef+0x1DC.CompassEnemyIcon");
        asset.CompassIconWidth = context.ReadInt32();
        asset.CompassIconHeight = context.ReadInt32();
        asset.IdleLowSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1E8.IdleLowSnd");
        asset.IdleHighSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1EC.IdleHighSnd");
        asset.EngineLowSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1F0.EngineLowSnd");
        asset.EngineHighSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1F4.EngineHighSnd");
        asset.EngineSndSpeed = context.ReadFloat();
        asset.EngineStartUpSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x1FC.EngineStartUpSnd");
        asset.EngineStartUpLength = context.ReadInt32();
        asset.EngineShutdownSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x204.EngineShutdownSnd");
        asset.EngineIdleSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x208.EngineIdleSnd");
        asset.EngineSustainSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x20C.EngineSustainSnd");
        asset.EngineRampUpSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x210.EngineRampUpSnd");
        asset.EngineRampUpLength = context.ReadInt32();
        asset.EngineRampDownSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x218.EngineRampDownSnd");
        asset.EngineRampDownLength = context.ReadInt32();
        asset.SuspensionSoftSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x220.SuspensionSoftSnd");
        asset.SuspensionSoftCompression = context.ReadFloat();
        asset.SuspensionHardSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x228.SuspensionHardSnd");
        asset.SuspensionHardCompression = context.ReadFloat();
        asset.CollisionSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x230.CollisionSnd");
        asset.CollisionBlendSpeed = context.ReadFloat();
        asset.SpeedSnd = ReadSoundAliasPointerField(ref context, "VehicleDef+0x238.SpeedSnd");
        asset.SpeedSndBlendSpeed = context.ReadFloat();
        asset.SurfaceSndPrefix = context.ReadDirectPointer<string>("VehicleDef+0x240.SurfaceSndPrefix");
        for (var i = 0; i < asset.SurfaceSnds.Length; i++)
            asset.SurfaceSnds[i] = ReadSoundAliasPointerField(ref context, $"VehicleDef+0x{0x244 + i * 4:X3}.SurfaceSnds[{i}]");
        asset.SurfaceSndBlendSpeed = context.ReadFloat();
        asset.SlideVolume = context.ReadFloat();
        asset.SlideBlendSpeed = context.ReadFloat();
        asset.InAirPitch = context.ReadFloat();

        var bytesRead = context.Position - start;
        if (bytesRead != VehicleDef.RootSize)
            throw new InvalidDataException($"VehicleDef read {bytesRead:N0} bytes; expected {VehicleDef.RootSize:N0} bytes.");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            ResolveVehicleChildren(ref context, asset);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    private static VehiclePhysDef ReadVehiclePhysDef(ref XFileReadContext context)
    {
        var start = context.Position;
        var physDef = new VehiclePhysDef
        {
            Offset = start,
            PhysicsEnabled = context.ReadInt32(),
            PhysPresetName = context.ReadDirectPointer<string>("VehicleDef+0x0AC.VehPhysDef.PhysPresetName"),
            PhysPreset = PhysicsReader.ReadPhysPresetPointerField(ref context, "VehicleDef+0x0B0.VehPhysDef.PhysPreset"),
            AccelGraphName = context.ReadDirectPointer<string>("VehicleDef+0x0B4.VehPhysDef.AccelGraphName"),
            SteeringAxle = ReadVehicleAxleType(ref context, "VehicleDef+0x0B8.VehPhysDef.SteeringAxle"),
            PowerAxle = ReadVehicleAxleType(ref context, "VehicleDef+0x0BC.VehPhysDef.PowerAxle"),
            BrakingAxle = ReadVehicleAxleType(ref context, "VehicleDef+0x0C0.VehPhysDef.BrakingAxle"),
            TopSpeed = context.ReadFloat(),
            ReverseSpeed = context.ReadFloat(),
            MaxVelocity = context.ReadFloat(),
            MaxPitch = context.ReadFloat(),
            MaxRoll = context.ReadFloat(),
            SuspensionTravelFront = context.ReadFloat(),
            SuspensionTravelRear = context.ReadFloat(),
            SuspensionStrengthFront = context.ReadFloat(),
            SuspensionDampingFront = context.ReadFloat(),
            SuspensionStrengthRear = context.ReadFloat(),
            SuspensionDampingRear = context.ReadFloat(),
            FrictionBraking = context.ReadFloat(),
            FrictionCoasting = context.ReadFloat(),
            FrictionTopSpeed = context.ReadFloat(),
            FrictionSide = context.ReadFloat(),
            FrictionSideRear = context.ReadFloat(),
            VelocityDependentSlip = context.ReadFloat(),
            RollStability = context.ReadFloat(),
            RollResistance = context.ReadFloat(),
            PitchResistance = context.ReadFloat(),
            YawResistance = context.ReadFloat(),
            UprightStrengthPitch = context.ReadFloat(),
            UprightStrengthRoll = context.ReadFloat(),
            TargetAirPitch = context.ReadFloat(),
            AirYawTorque = context.ReadFloat(),
            AirPitchTorque = context.ReadFloat(),
            MinimumMomentumForCollision = context.ReadFloat(),
            CollisionLaunchForceScale = context.ReadFloat(),
            WreckedMassScale = context.ReadFloat(),
            WreckedBodyFriction = context.ReadFloat(),
            MinimumJoltForNotify = context.ReadFloat(),
            SlipThresholdFront = context.ReadFloat(),
            SlipThresholdRear = context.ReadFloat(),
            SlipFricScaleFront = context.ReadFloat(),
            SlipFricScaleRear = context.ReadFloat(),
            SlipFricRateFront = context.ReadFloat(),
            SlipFricRateRear = context.ReadFloat(),
            SlipYawTorque = context.ReadFloat(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != VehiclePhysDef.RootSize)
            throw new InvalidDataException($"VehiclePhysDef read {bytesRead:N0} bytes; expected {VehiclePhysDef.RootSize:N0} bytes.");

        return physDef;
    }

    private static void ResolveVehicleChildren(
        ref XFileReadContext context,
        VehicleDef asset)
    {
        GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr!);
        GenericReader.ResolveStringPointerNow(ref context, asset.UseHintString);
        ResolveVehiclePhysDefChildren(ref context, asset.VehPhysDef);
        GenericReader.ResolveStringPointerNow(ref context, asset.TurretWeaponName);
        ResolveWeaponPointerNow(ref context, asset.TurretWeapon);
        ResolveSoundAliasPointerNow(ref context, asset.TurretSpinSnd);
        ResolveSoundAliasPointerNow(ref context, asset.TurretStopSnd);
        MaterialReader.ResolveMaterialPointerNow(ref context, asset.CompassFriendlyIcon);
        MaterialReader.ResolveMaterialPointerNow(ref context, asset.CompassEnemyIcon);
        ResolveSoundAliasPointerNow(ref context, asset.IdleLowSnd);
        ResolveSoundAliasPointerNow(ref context, asset.IdleHighSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineLowSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineHighSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineStartUpSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineShutdownSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineIdleSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineSustainSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineRampUpSnd);
        ResolveSoundAliasPointerNow(ref context, asset.EngineRampDownSnd);
        ResolveSoundAliasPointerNow(ref context, asset.SuspensionSoftSnd);
        ResolveSoundAliasPointerNow(ref context, asset.SuspensionHardSnd);
        ResolveSoundAliasPointerNow(ref context, asset.CollisionSnd);
        ResolveSoundAliasPointerNow(ref context, asset.SpeedSnd);
        GenericReader.ResolveStringPointerNow(ref context, asset.SurfaceSndPrefix);

        foreach (var sound in asset.SurfaceSnds)
            ResolveSoundAliasPointerNow(ref context, sound);
    }

    private static void ResolveVehiclePhysDefChildren(
        ref XFileReadContext context,
        VehiclePhysDef physDef)
    {
        GenericReader.ResolveStringPointerNow(ref context, physDef.PhysPresetName);
        PhysicsReader.ResolvePhysPresetPointerNow(ref context, physDef.PhysPreset);
        GenericReader.ResolveStringPointerNow(ref context, physDef.AccelGraphName);
    }

    private static DirectPointer<string> ReadSoundAliasPointerField(
        ref XFileReadContext context,
        string fieldPath)
    {
        return context.ReadDirectPointer<string>(fieldPath);
    }

    private static void ResolveSoundAliasPointerNow(
        ref XFileReadContext context,
        ZonePointer<string> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<string> p) =>
        {
            var aliasName = pointerContext.ReadDirectPointer<string>("VehicleDef.SndAlias.Name");
            GenericReader.ResolveStringPointerNow(ref pointerContext, aliasName);
            p.SetResult(aliasName.Result);
        });
    }

    private static void ResolveWeaponPointerNow(
        ref XFileReadContext context,
        ZonePointer<WeaponVariantDef> pointer)
    {
        context.ResolvePointerNowInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<WeaponVariantDef> p) =>
            {
                var resolverScope = pointerContext.CaptureResolverScope();
                p.SetResult(pointerContext.ReadPointerValue(p, WeaponReader.Read));
                pointerContext.ResolveResolverScopeNow(resolverScope);
            });
    }

    private static VehicleAxleType ReadVehicleAxleType(
        ref XFileReadContext context,
        string fieldPath)
    {
        var raw = context.ReadInt32();
        XFileReadValidator.ValidateCount(
            ref context,
            fieldPath,
            raw,
            (int)VehicleAxleType.Front,
            (int)VehicleAxleType.All,
            "PC VehicleAxleType names are used only after EBOOT 0x00107220 confirms VehiclePhysDef offsets.");

        return (VehicleAxleType)raw;
    }

    private static void EnsureRootOffset(
        ref XFileReadContext context,
        int rootStart,
        int rootOffset)
    {
        var actual = context.Position - rootStart;
        if (actual == rootOffset)
            return;

        throw new InvalidDataException(
            $"VehicleDef reader reached root offset 0x{actual:X}; expected 0x{rootOffset:X}.");
    }
}
