using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Utils;

namespace FastFile.Logic.Assets.Readers;

internal static class WeaponReader
{
    private const int WeaponVariantDefSize = 0x74;
    private const int WeaponDefSize = 0x684;
    private const int GunModelCount = 16;
    private const int WeaponAnimCount = 37;
    private const int HideTagCount = 32;
    private const int NoteTrackMapCount = 16;
    private const int SurfaceCount = 31;
    private const int HitLocationCount = 20;
    private const int WeaponSoundAliasCount = 47;

    public static WeaponVariantDef Read(ref ZoneReadContext context)
    {
        var start = context.Position;
        var weapon = new WeaponVariantDef
        {
            Offset = start,
            InternalNamePtr = GenericReader.ReadStringPointer(ref context),
            WeaponDefPtr = ReadWeaponDefPointer(ref context),
            DisplayNamePtr = GenericReader.ReadStringPointer(ref context),
            HideTags = ReadUShortArrayPointer(ref context, HideTagCount),
            XAnims = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount),
            fAdsZoomFov = context.ReadFloat(),
            iAdsTransInTime = context.ReadInt32(),
            iAdsTransOutTime = context.ReadInt32(),
            iClipSize = context.ReadInt32(),
            impactType = (FastFile.Models.Assets.Weapons.Enums.ImpactType)context.ReadInt32(),
            iFireTime = context.ReadInt32(),
            dpadIconRatio = (FastFile.Models.Assets.Weapons.Enums.WeaponIconRatioType)context.ReadInt32(),
            fPenetrateMultiplier = context.ReadFloat(),
            fAdsViewKickCenterSpeed = context.ReadFloat(),
            fHipViewKickCenterSpeed = context.ReadFloat(),
            szAltWeaponName = GenericReader.ReadStringPointer(ref context),
            altWeaponIndex = context.ReadUInt32(),
            iAltRaiseTime = context.ReadInt32(),
            killIcon = MaterialReader.ReadMaterialPointer(ref context),
            dpadIcon = MaterialReader.ReadMaterialPointer(ref context),
            unknown8 = context.ReadInt32(),
            iFirstRaiseTime = context.ReadInt32(),
            iDropAmmoMax = context.ReadInt32(),
            adsDofStart = context.ReadFloat(),
            adsDofEnd = context.ReadFloat(),
            accuracyGraphKnotCount = (short)context.ReadUInt16(),
            originalAccuracyGraphKnotCount = (short)context.ReadUInt16(),
        };

        weapon.accuracyGraphKnots = ReadVec2ArrayPointer(ref context, weapon.accuracyGraphKnotCount);
        weapon.originalAccuracyGraphKnots = ReadVec2ArrayPointer(ref context, weapon.originalAccuracyGraphKnotCount);
        weapon.motionTracker = context.ReadByte() != 0;
        weapon.enhanced = context.ReadByte() != 0;
        weapon.dpadIconShowsAmmo = context.ReadByte() != 0;
        weapon.DpadIconShowsAmmoPadding = context.ReadByte();
        EnsureFixedSize(context.Position - start, WeaponVariantDefSize, "WeaponVariantDef");

        return weapon;
    }

    private static ZonePointer<WeaponDef> ReadWeaponDefPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<WeaponDef>(
            (ref ZoneReadContext pointerContext, ZonePointer<WeaponDef> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadWeaponDef));
            });
    }

    private static WeaponDef ReadWeaponDef(ref ZoneReadContext context)
    {
        var start = context.Position;
        var weaponDef = new WeaponDef
        {
            Offset = start,
            InternalNamePtr = GenericReader.ReadStringPointer(ref context),
            gunXModel = XModelReader.ReadXModelPointerArrayPointer(ref context, GunModelCount),
            handXModel = XModelReader.ReadXModelPointer(ref context),
            szXAnimsR = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount),
            szXAnimsL = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount),
            ModeNamePtr = GenericReader.ReadStringPointer(ref context),
        };

        for (var i = 0; i < weaponDef.NoteTrackMaps.Length; i++)
            weaponDef.NoteTrackMaps[i] = ReadUShortArrayPointer(ref context, NoteTrackMapCount);

        weaponDef.PlayerAnimTypeThroughStance = ReadInt32Array(ref context, weaponDef.PlayerAnimTypeThroughStance.Length);
        for (var i = 0; i < weaponDef.FlashEffects.Length; i++)
            weaponDef.FlashEffects[i] = FxReader.ReadFxPointer(ref context);

        for (var i = 0; i < WeaponSoundAliasCount; i++)
            weaponDef.SoundAliases[i] = ReadSoundAliasPointer(ref context);
        weaponDef.BounceSound = GenericReader.ReadStringPointerArrayPointer(ref context, SurfaceCount);

        for (var i = 0; i < weaponDef.EffectPointersA.Length; i++)
            weaponDef.EffectPointersA[i] = FxReader.ReadFxPointer(ref context);
        for (var i = 0; i < weaponDef.MaterialPointersA.Length; i++)
            weaponDef.MaterialPointersA[i] = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.ReticleFields = ReadInt32Array(ref context, weaponDef.ReticleFields.Length);
        weaponDef.ViewMovementRotationFields = ReadInt32Array(ref context, weaponDef.ViewMovementRotationFields.Length);
        weaponDef.PositionalMovementRotationFields = ReadInt32Array(ref context, weaponDef.PositionalMovementRotationFields.Length);

        weaponDef.WorldGunXModel = XModelReader.ReadXModelPointerArrayPointer(ref context, GunModelCount);
        for (var i = 0; i < weaponDef.WorldModelPointers.Length; i++)
            weaponDef.WorldModelPointers[i] = XModelReader.ReadXModelPointer(ref context);
        weaponDef.AmmoCounterIcon = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.AmmoCounterIconRatio = context.ReadInt32();
        weaponDef.CompassIcon = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.CompassIconRatio = context.ReadInt32();
        weaponDef.OverlayMaterial = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.OverlayFieldsA = ReadInt32Array(ref context, weaponDef.OverlayFieldsA.Length);
        weaponDef.OverlayReticle = GenericReader.ReadStringPointer(ref context);
        weaponDef.OverlayReticleField = context.ReadInt32();
        weaponDef.OverlayInterface = GenericReader.ReadStringPointer(ref context);
        weaponDef.OverlayFieldsB = ReadInt32Array(ref context, weaponDef.OverlayFieldsB.Length);
        weaponDef.ModeNameAlt = GenericReader.ReadStringPointer(ref context);
        weaponDef.ModeFields = ReadInt32Array(ref context, weaponDef.ModeFields.Length);
        weaponDef.WeaponTimingFields = ReadInt32Array(ref context, weaponDef.WeaponTimingFields.Length);
        weaponDef.AimMovementTuningFields = ReadInt32Array(ref context, weaponDef.AimMovementTuningFields.Length);

        for (var i = 0; i < weaponDef.OverlayMaterials.Length; i++)
            weaponDef.OverlayMaterials[i] = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.OverlayDimensionFields = ReadInt32Array(ref context, weaponDef.OverlayDimensionFields.Length);
        weaponDef.BobSpreadIdleSwayAdsViewErrorFields = ReadInt32Array(ref context, weaponDef.BobSpreadIdleSwayAdsViewErrorFields.Length);

        weaponDef.PhysCollmap = PhysicsReader.ReadPhysCollmapPointer(ref context);
        weaponDef.PhysicsFieldsA = ReadInt32Array(ref context, weaponDef.PhysicsFieldsA.Length);
        weaponDef.PhysicsFieldsB = ReadInt32Array(ref context, weaponDef.PhysicsFieldsB.Length);
        weaponDef.PhysicsFieldsC = ReadInt32Array(ref context, weaponDef.PhysicsFieldsC.Length);
        weaponDef.PhysicsFieldsD = ReadInt32Array(ref context, weaponDef.PhysicsFieldsD.Length);
        weaponDef.ProjectileModel = XModelReader.ReadXModelPointer(ref context);
        weaponDef.ProjectileModelField = context.ReadInt32();
        for (var i = 0; i < weaponDef.ProjectileEffects.Length; i++)
            weaponDef.ProjectileEffects[i] = FxReader.ReadFxPointer(ref context);
        for (var i = 0; i < weaponDef.ProjectileSoundAliases.Length; i++)
            weaponDef.ProjectileSoundAliases[i] = ReadSoundAliasPointer(ref context);
        weaponDef.ProjectileFieldsA = ReadInt32Array(ref context, weaponDef.ProjectileFieldsA.Length);
        weaponDef.ParallelBounce = ReadFloatArrayPointer(ref context, SurfaceCount);
        weaponDef.PerpendicularBounce = ReadFloatArrayPointer(ref context, SurfaceCount);
        for (var i = 0; i < weaponDef.ImpactEffects.Length; i++)
            weaponDef.ImpactEffects[i] = FxReader.ReadFxPointer(ref context);
        weaponDef.ImpactFieldsA = ReadInt32Array(ref context, weaponDef.ImpactFieldsA.Length);
        weaponDef.ImpactFieldB = context.ReadInt32();
        weaponDef.ImpactFieldsC = ReadInt32Array(ref context, weaponDef.ImpactFieldsC.Length);
        weaponDef.ViewShellEjectEffect = FxReader.ReadFxPointer(ref context);
        weaponDef.ShellEjectSound = ReadSoundAliasPointer(ref context);
        weaponDef.ShellEjectFields = ReadInt32Array(ref context, weaponDef.ShellEjectFields.Length);
        weaponDef.AdsHipGunKickAiDistanceFields = ReadInt32Array(ref context, weaponDef.AdsHipGunKickAiDistanceFields.Length);

        weaponDef.AccuracyGraphName0 = GenericReader.ReadStringPointer(ref context);
        weaponDef.AccuracyGraphName1 = GenericReader.ReadStringPointer(ref context);
        weaponDef.accuracyGraphKnots = context.ReadPointer<Vec2[]>();
        weaponDef.originalAccuracyGraphKnots = context.ReadPointer<Vec2[]>();
        weaponDef.accuracyGraphKnotCount = context.ReadUInt16();
        weaponDef.originalAccuracyGraphKnotCount = context.ReadUInt16();
        ResolveVec2ArrayPointer(ref context, weaponDef.accuracyGraphKnots, weaponDef.accuracyGraphKnotCount);
        ResolveVec2ArrayPointer(ref context, weaponDef.originalAccuracyGraphKnots, weaponDef.originalAccuracyGraphKnotCount);

        weaponDef.AccuracyGraphField = context.ReadInt32();
        weaponDef.LeftArc = context.ReadFloat();
        weaponDef.RightArc = context.ReadFloat();
        weaponDef.TopArc = context.ReadFloat();
        weaponDef.BottomArc = context.ReadFloat();
        weaponDef.Accuracy = context.ReadFloat();
        weaponDef.AiSpread = context.ReadFloat();
        weaponDef.PlayerSpread = context.ReadFloat();
        weaponDef.MinTurnSpeed = ReadFloatArray(ref context, weaponDef.MinTurnSpeed.Length);
        weaponDef.MaxTurnSpeed = ReadFloatArray(ref context, weaponDef.MaxTurnSpeed.Length);
        weaponDef.PitchConvergenceTime = context.ReadFloat();
        weaponDef.YawConvergenceTime = context.ReadFloat();
        weaponDef.SuppressTime = context.ReadFloat();
        weaponDef.MaxRange = context.ReadFloat();
        weaponDef.AnimHorizontalRotateInc = context.ReadFloat();
        weaponDef.PlayerPositionDist = context.ReadFloat();
        weaponDef.UseHintString = GenericReader.ReadStringPointer(ref context);
        weaponDef.DropHintString = GenericReader.ReadStringPointer(ref context);
        weaponDef.HintFieldsA = ReadInt32Array(ref context, weaponDef.HintFieldsA.Length);
        weaponDef.HintFieldsB = ReadInt32Array(ref context, weaponDef.HintFieldsB.Length);
        weaponDef.ScriptName = GenericReader.ReadStringPointer(ref context);
        weaponDef.ScriptFieldsA = ReadInt32Array(ref context, weaponDef.ScriptFieldsA.Length);
        weaponDef.ScriptFieldsB = ReadInt32Array(ref context, weaponDef.ScriptFieldsB.Length);
        weaponDef.HitLocationField = context.ReadInt32();
        weaponDef.LocationDamageMultipliers = ReadFloatArrayPointer(ref context, HitLocationCount);
        weaponDef.FireRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.MeleeImpactRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.Tracer = TracerReader.ReadTracerPointer(ref context);

        weaponDef.TracerFields = ReadInt32Array(ref context, weaponDef.TracerFields.Length);
        weaponDef.TurretOverheatSound = ReadSoundAliasPointer(ref context);
        weaponDef.TurretOverheatEffect = FxReader.ReadFxPointer(ref context);
        weaponDef.TurretBarrelSpinRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.TurretFields = ReadInt32Array(ref context, weaponDef.TurretFields.Length);
        weaponDef.TurretBarrelSpinMaxSnd = ReadSoundAliasPointer(ref context);
        for (var i = 0; i < 4; i++)
            weaponDef.TurretBarrelSpinUpSnd[i] = ReadSoundAliasPointer(ref context);
        for (var i = 0; i < 4; i++)
            weaponDef.TurretBarrelSpinDownSnd[i] = ReadSoundAliasPointer(ref context);
        weaponDef.MissileConeSoundAlias = ReadSoundAliasPointer(ref context);
        weaponDef.MissileConeSoundAliasAtBase = ReadSoundAliasPointer(ref context);
        weaponDef.MissileConeSoundRadiusAtTop = context.ReadFloat();
        weaponDef.MissileConeSoundRadiusAtBase = context.ReadFloat();
        weaponDef.MissileConeSoundHeight = context.ReadFloat();
        weaponDef.MissileConeSoundOriginOffset = context.ReadFloat();
        weaponDef.MissileConeSoundVolumescaleAtCore = context.ReadFloat();
        weaponDef.MissileConeSoundVolumescaleAtEdge = context.ReadFloat();
        weaponDef.MissileConeSoundVolumescaleCoreSize = context.ReadFloat();
        weaponDef.MissileConeSoundPitchAtTop = context.ReadFloat();
        weaponDef.MissileConeSoundPitchAtBottom = context.ReadFloat();
        weaponDef.MissileConeSoundPitchTopSize = context.ReadFloat();
        weaponDef.MissileConeSoundPitchBottomSize = context.ReadFloat();
        weaponDef.MissileConeSoundCrossfadeTopSize = context.ReadFloat();
        weaponDef.MissileConeSoundCrossfadeBottomSize = context.ReadFloat();
        weaponDef.SharedAmmo = context.ReadBool();
        weaponDef.LockonSupported = context.ReadBool();
        weaponDef.RequireLockonToFire = context.ReadBool();
        weaponDef.BigExplosion = context.ReadBool();
        weaponDef.BooleanFlags = ReadWeaponBooleanFlags(ref context);
        EnsureFixedSize(context.Position - start, WeaponDefSize, "WeaponDef");
        return weaponDef;
    }

    private static WeaponBooleanFlags ReadWeaponBooleanFlags(ref ZoneReadContext context)
    {
        return new WeaponBooleanFlags
        {
            NoAdsWhenMagEmpty = context.ReadBool(),
            AvoidDropCleanup = context.ReadBool(),
            InheritsPerks = context.ReadBool(),
            CrosshairColorChange = context.ReadBool(),
            RifleBullet = context.ReadBool(),
            ArmorPiercing = context.ReadBool(),
            BoltAction = context.ReadBool(),
            AimDownSight = context.ReadBool(),
            RechamberWhileAds = context.ReadBool(),
            BulletExplosiveDamage = context.ReadBool(),
            CookOffHold = context.ReadBool(),
            ClipOnly = context.ReadBool(),
            NoAmmoPickup = context.ReadBool(),
            AdsFireOnly = context.ReadBool(),
            CancelAutoHolsterWhenEmpty = context.ReadBool(),
            DisableSwitchToWhenEmpty = context.ReadBool(),
            SuppressAmmoReserveDisplay = context.ReadBool(),
            LaserSightDuringNightvision = context.ReadBool(),
            MarkableViewmodel = context.ReadBool(),
            NoDualWield = context.ReadBool(),
            FlipKillIcon = context.ReadBool(),
            NoPartialReload = context.ReadBool(),
            SegmentedReload = context.ReadBool(),
            BlocksProne = context.ReadBool(),
            Silenced = context.ReadBool(),
            IsRollingGrenade = context.ReadBool(),
            ProjExplosionEffectForceNormalUp = context.ReadBool(),
            ProjImpactExplode = context.ReadBool(),
            StickToPlayers = context.ReadBool(),
            HasDetonator = context.ReadBool(),
            DisableFiring = context.ReadBool(),
            TimedDetonation = context.ReadBool(),
            Rotate = context.ReadBool(),
            HoldButtonToThrow = context.ReadBool(),
            FreezeMovementWhenFiring = context.ReadBool(),
            ThermalScope = context.ReadBool(),
            AltModeSameWeapon = context.ReadBool(),
            TurretBarrelSpinEnabled = context.ReadBool(),
            MissileConeSoundEnabled = context.ReadBool(),
            MissileConeSoundPitchshiftEnabled = context.ReadBool(),
            MissileConeSoundCrossfadeEnabled = context.ReadBool(),
            OffhandHoldIsCancelable = context.ReadBool(),
            Ps3TailFlag0 = context.ReadByte(),
            Ps3TailFlag1 = context.ReadByte(),
        };
    }

    private static ZonePointer<string> ReadSoundAliasPointer(ref ZoneReadContext context)
    {
        return GenericReader.ReadStringPointer(ref context);
    }

    private static ZonePointer<ushort[]> ReadUShortArrayPointer(
        ref ZoneReadContext context,
        int count)
    {
        var pointer = context.ReadPointer<ushort[]>();
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<ushort[]> p) =>
        {
            var values = new ushort[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadUInt16();

            p.SetResult(values);
        });

        return pointer;
    }

    private static ZonePointer<float[]> ReadFloatArrayPointer(
        ref ZoneReadContext context,
        int count)
    {
        var pointer = context.ReadPointer<float[]>();
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<float[]> p) =>
        {
            var values = new float[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadFloat();

            p.SetResult(values);
        });

        return pointer;
    }

    private static ZonePointer<Vec2[]> ReadVec2ArrayPointer(
        ref ZoneReadContext context,
        int count)
    {
        var pointer = context.ReadPointer<Vec2[]>();
        ResolveVec2ArrayPointer(ref context, pointer, count);
        return pointer;
    }

    private static void ResolveVec2ArrayPointer(
        ref ZoneReadContext context,
        ZonePointer<Vec2[]> pointer,
        int count)
    {
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<Vec2[]> p) =>
        {
            var values = new Vec2[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new Vec2
                {
                    a = pointerContext.ReadFloat(),
                    b = pointerContext.ReadFloat(),
                };
            }

            p.SetResult(values);
        });
    }

    private static int[] ReadInt32Array(ref ZoneReadContext context, int count)
    {
        var values = new int[Math.Max(0, count)];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadInt32();

        return values;
    }

    private static float[] ReadFloatArray(ref ZoneReadContext context, int count)
    {
        var values = new float[Math.Max(0, count)];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();

        return values;
    }

    private static void EnsureFixedSize(
        int read,
        int expectedSize,
        string typeName)
    {
        if (read != expectedSize)
            throw new InvalidDataException($"{typeName} read 0x{read:X} bytes; expected 0x{expectedSize:X}.");
    }
}
