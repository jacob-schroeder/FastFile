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
    private const int WeaponBooleanTailSize = 0x30;

    public static WeaponVariantDef Read(ref XFileReadContext context)
    {
        var start = context.Position;
        var weapon = new WeaponVariantDef
        {
            Offset = start,
            InternalNamePtr = GenericReader.ReadStringPointer(ref context),
            WeaponDefPtr = ReadWeaponDefPointer(ref context),
            DisplayNamePtr = GenericReader.ReadStringPointer(ref context),
            HideTags = ReadUShortArrayPointer(ref context, HideTagCount, "WeaponVariantDef.HideTags"),
            XAnims = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount, "WeaponVariantDef.XAnims"),
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

        weapon.accuracyGraphKnots = ReadVec2ArrayPointer(ref context, weapon.accuracyGraphKnotCount, "WeaponVariantDef.AccuracyGraphKnots");
        weapon.originalAccuracyGraphKnots = ReadVec2ArrayPointer(ref context, weapon.originalAccuracyGraphKnotCount, "WeaponVariantDef.OriginalAccuracyGraphKnots");
        weapon.motionTracker = context.ReadByte() != 0;
        weapon.enhanced = context.ReadByte() != 0;
        weapon.dpadIconShowsAmmo = context.ReadByte() != 0;
        weapon.DpadIconShowsAmmoPadding = context.ReadByte();
        EnsureFixedSize(context.Position - start, WeaponVariantDefSize, "WeaponVariantDef");

        return weapon;
    }

    private static ZonePointer<WeaponDef> ReadWeaponDefPointer(ref XFileReadContext context)
    {
        return context.ReadPointer<WeaponDef>(
            (ref XFileReadContext pointerContext, ZonePointer<WeaponDef> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadWeaponDef));
            },
            PointerResolutionKind.Direct,
            "WeaponVariantDef.WeaponDef");
    }

    private static WeaponDef ReadWeaponDef(ref XFileReadContext context)
    {
        var start = context.Position;
        var weaponDef = new WeaponDef
        {
            Offset = start,
            InternalNamePtr = GenericReader.ReadStringPointer(ref context),
            gunXModel = XModelReader.ReadXModelPointerArrayPointer(ref context, GunModelCount, "WeaponDef.GunXModel"),
            handXModel = XModelReader.ReadXModelPointer(ref context),
            szXAnimsR = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount, "WeaponDef.szXAnimsR"),
            szXAnimsL = GenericReader.ReadStringPointerArrayPointer(ref context, WeaponAnimCount, "WeaponDef.szXAnimsL"),
            ModeNamePtr = GenericReader.ReadStringPointer(ref context),
        };

        for (var i = 0; i < weaponDef.NoteTrackMaps.Length; i++)
            weaponDef.NoteTrackMaps[i] = ReadUShortArrayPointer(ref context, NoteTrackMapCount, $"WeaponDef.NoteTrackMaps[{i}]");

        weaponDef.PlayerAnimTypeThroughStance = ReadInt32Array(ref context, weaponDef.PlayerAnimTypeThroughStance.Length);
        for (var i = 0; i < weaponDef.FlashEffects.Length; i++)
            weaponDef.FlashEffects[i] = FxReader.ReadFxPointer(ref context);

        for (var i = 0; i < WeaponSoundAliasCount; i++)
            weaponDef.SoundAliases[i] = ReadSoundAliasPointer(ref context, $"WeaponDef.SoundAliases[{i}]");
        weaponDef.BounceSound = ReadSoundAliasPointerArrayPointer(ref context, SurfaceCount, "WeaponDef.BounceSound");

        for (var i = 0; i < weaponDef.EffectPointersA.Length; i++)
            weaponDef.EffectPointersA[i] = FxReader.ReadFxPointer(ref context);
        for (var i = 0; i < weaponDef.MaterialPointersA.Length; i++)
            weaponDef.MaterialPointersA[i] = MaterialReader.ReadMaterialPointer(ref context);
        weaponDef.ReticleFields = ReadInt32Array(ref context, weaponDef.ReticleFields.Length);
        weaponDef.ViewMovementRotationFields = ReadInt32Array(ref context, weaponDef.ViewMovementRotationFields.Length);
        weaponDef.PositionalMovementRotationFields = ReadInt32Array(ref context, weaponDef.PositionalMovementRotationFields.Length);

        weaponDef.WorldGunXModel = XModelReader.ReadXModelPointerArrayPointer(ref context, GunModelCount, "WeaponDef.WorldGunXModel");
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
            weaponDef.ProjectileSoundAliases[i] = ReadSoundAliasPointer(ref context, $"WeaponDef.ProjectileSoundAliases[{i}]");
        weaponDef.ProjectileFieldsA = ReadInt32Array(ref context, weaponDef.ProjectileFieldsA.Length);
        weaponDef.ParallelBounce = ReadFloatArrayPointer(ref context, SurfaceCount, "WeaponDef.ParallelBounce");
        weaponDef.PerpendicularBounce = ReadFloatArrayPointer(ref context, SurfaceCount, "WeaponDef.PerpendicularBounce");
        for (var i = 0; i < weaponDef.ImpactEffects.Length; i++)
            weaponDef.ImpactEffects[i] = FxReader.ReadFxPointer(ref context);
        weaponDef.ImpactFieldsA = ReadInt32Array(ref context, weaponDef.ImpactFieldsA.Length);
        weaponDef.ImpactFieldB = context.ReadInt32();
        weaponDef.ImpactFieldsC = ReadInt32Array(ref context, weaponDef.ImpactFieldsC.Length);
        weaponDef.ViewShellEjectEffect = FxReader.ReadFxPointer(ref context);
        weaponDef.ShellEjectSound = ReadSoundAliasPointer(ref context, "WeaponDef.ShellEjectSound");
        weaponDef.ShellEjectFields = ReadInt32Array(ref context, weaponDef.ShellEjectFields.Length);
        weaponDef.AdsHipGunKickAiDistanceFields = ReadInt32Array(ref context, weaponDef.AdsHipGunKickAiDistanceFields.Length);

        weaponDef.AccuracyGraphName0 = GenericReader.ReadStringPointer(ref context);
        weaponDef.AccuracyGraphName1 = GenericReader.ReadStringPointer(ref context);
        weaponDef.accuracyGraphKnots = context.ReadDirectPointer<Vec2[]>("WeaponDef.AccuracyGraphKnots");
        weaponDef.originalAccuracyGraphKnots = context.ReadDirectPointer<Vec2[]>("WeaponDef.OriginalAccuracyGraphKnots");
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
        weaponDef.LocationDamageMultipliers = ReadFloatArrayPointer(ref context, HitLocationCount, "WeaponDef.LocationDamageMultipliers");
        weaponDef.FireRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.MeleeImpactRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.Tracer = TracerReader.ReadTracerPointer(ref context);

        weaponDef.TracerFields = ReadInt32Array(ref context, weaponDef.TracerFields.Length);
        weaponDef.TurretOverheatSound = ReadSoundAliasPointer(ref context, "WeaponDef.TurretOverheatSound");
        weaponDef.TurretOverheatEffect = FxReader.ReadFxPointer(ref context);
        weaponDef.TurretBarrelSpinRumble = GenericReader.ReadStringPointer(ref context);
        weaponDef.TurretFields = ReadInt32Array(ref context, weaponDef.TurretFields.Length);
        weaponDef.TurretBarrelSpinMaxSnd = ReadSoundAliasPointer(ref context, "WeaponDef.TurretBarrelSpinMaxSnd");
        for (var i = 0; i < 4; i++)
            weaponDef.TurretBarrelSpinUpSnd[i] = ReadSoundAliasPointer(ref context, $"WeaponDef.TurretBarrelSpinUpSnd[{i}]");
        for (var i = 0; i < 4; i++)
            weaponDef.TurretBarrelSpinDownSnd[i] = ReadSoundAliasPointer(ref context, $"WeaponDef.TurretBarrelSpinDownSnd[{i}]");
        weaponDef.MissileConeSoundAlias = ReadSoundAliasPointer(ref context, "WeaponDef.MissileConeSoundAlias");
        weaponDef.MissileConeSoundAliasAtBase = ReadSoundAliasPointer(ref context, "WeaponDef.MissileConeSoundAliasAtBase");
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
        ReadWeaponBooleanTail(ref context, weaponDef);
        EnsureFixedSize(context.Position - start, WeaponDefSize, "WeaponDef");
        return weaponDef;
    }

    private static void ReadWeaponBooleanTail(
        ref XFileReadContext context,
        WeaponDef weaponDef)
    {
        var bytes = context.ReadBytes(WeaponBooleanTailSize);
        weaponDef.BooleanTailBytes = bytes;

        var offset = 0;
        weaponDef.SharedAmmo = ReadWeaponBool(bytes, ref offset);
        weaponDef.LockonSupported = ReadWeaponBool(bytes, ref offset);
        weaponDef.RequireLockonToFire = ReadWeaponBool(bytes, ref offset);
        weaponDef.BigExplosion = ReadWeaponBool(bytes, ref offset);
        weaponDef.BooleanFlags = ReadWeaponBooleanFlags(bytes.AsSpan(offset));
    }

    private static WeaponBooleanFlags ReadWeaponBooleanFlags(ReadOnlySpan<byte> bytes)
    {
        var offset = 0;
        return new WeaponBooleanFlags
        {
            NoAdsWhenMagEmpty = ReadWeaponBool(bytes, ref offset),
            AvoidDropCleanup = ReadWeaponBool(bytes, ref offset),
            InheritsPerks = ReadWeaponBool(bytes, ref offset),
            CrosshairColorChange = ReadWeaponBool(bytes, ref offset),
            RifleBullet = ReadWeaponBool(bytes, ref offset),
            ArmorPiercing = ReadWeaponBool(bytes, ref offset),
            BoltAction = ReadWeaponBool(bytes, ref offset),
            AimDownSight = ReadWeaponBool(bytes, ref offset),
            RechamberWhileAds = ReadWeaponBool(bytes, ref offset),
            BulletExplosiveDamage = ReadWeaponBool(bytes, ref offset),
            CookOffHold = ReadWeaponBool(bytes, ref offset),
            ClipOnly = ReadWeaponBool(bytes, ref offset),
            NoAmmoPickup = ReadWeaponBool(bytes, ref offset),
            AdsFireOnly = ReadWeaponBool(bytes, ref offset),
            CancelAutoHolsterWhenEmpty = ReadWeaponBool(bytes, ref offset),
            DisableSwitchToWhenEmpty = ReadWeaponBool(bytes, ref offset),
            SuppressAmmoReserveDisplay = ReadWeaponBool(bytes, ref offset),
            LaserSightDuringNightvision = ReadWeaponBool(bytes, ref offset),
            MarkableViewmodel = ReadWeaponBool(bytes, ref offset),
            NoDualWield = ReadWeaponBool(bytes, ref offset),
            FlipKillIcon = ReadWeaponBool(bytes, ref offset),
            NoPartialReload = ReadWeaponBool(bytes, ref offset),
            SegmentedReload = ReadWeaponBool(bytes, ref offset),
            BlocksProne = ReadWeaponBool(bytes, ref offset),
            Silenced = ReadWeaponBool(bytes, ref offset),
            IsRollingGrenade = ReadWeaponBool(bytes, ref offset),
            ProjExplosionEffectForceNormalUp = ReadWeaponBool(bytes, ref offset),
            ProjImpactExplode = ReadWeaponBool(bytes, ref offset),
            StickToPlayers = ReadWeaponBool(bytes, ref offset),
            HasDetonator = ReadWeaponBool(bytes, ref offset),
            DisableFiring = ReadWeaponBool(bytes, ref offset),
            TimedDetonation = ReadWeaponBool(bytes, ref offset),
            Rotate = ReadWeaponBool(bytes, ref offset),
            HoldButtonToThrow = ReadWeaponBool(bytes, ref offset),
            FreezeMovementWhenFiring = ReadWeaponBool(bytes, ref offset),
            ThermalScope = ReadWeaponBool(bytes, ref offset),
            AltModeSameWeapon = ReadWeaponBool(bytes, ref offset),
            TurretBarrelSpinEnabled = ReadWeaponBool(bytes, ref offset),
            MissileConeSoundEnabled = ReadWeaponBool(bytes, ref offset),
            MissileConeSoundPitchshiftEnabled = ReadWeaponBool(bytes, ref offset),
            MissileConeSoundCrossfadeEnabled = ReadWeaponBool(bytes, ref offset),
            OffhandHoldIsCancelable = ReadWeaponBool(bytes, ref offset),
            Ps3TailFlag0 = bytes[offset++],
            Ps3TailFlag1 = bytes[offset],
        };
    }

    private static bool ReadWeaponBool(
        ReadOnlySpan<byte> bytes,
        ref int offset)
    {
        return bytes[offset++] != 0;
    }

    private static ZonePointer<string> ReadSoundAliasPointer(
        ref XFileReadContext context,
        string fieldPath)
    {
        var pointer = context.ReadDirectPointer<string>(fieldPath);
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<string> p) =>
        {
            var aliasName = GenericReader.ReadStringPointer(ref pointerContext, resolve: false);
            GenericReader.ResolveStringPointerNow(ref pointerContext, aliasName);
            p.SetResult(aliasName.Result);
        });

        return pointer;
    }

    private static ZonePointer<ZonePointer<string>[]> ReadSoundAliasPointerArrayPointer(
        ref XFileReadContext context,
        int count,
        string fieldPath)
    {
        var pointer = context.ReadDirectPointer<ZonePointer<string>[]>(fieldPath);
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<string>[]> p) =>
        {
            var values = new ZonePointer<string>[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = ReadSoundAliasPointer(ref pointerContext, $"{fieldPath}[{i}]");

            p.SetResult(values);
        });

        return pointer;
    }

    private static ZonePointer<ushort[]> ReadUShortArrayPointer(
        ref XFileReadContext context,
        int count,
        string fieldPath)
    {
        var pointer = context.ReadDirectPointer<ushort[]>(fieldPath);
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<ushort[]> p) =>
        {
            var values = new ushort[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadUInt16();

            p.SetResult(values);
        });

        return pointer;
    }

    private static ZonePointer<float[]> ReadFloatArrayPointer(
        ref XFileReadContext context,
        int count,
        string fieldPath)
    {
        var pointer = context.ReadDirectPointer<float[]>(fieldPath);
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<float[]> p) =>
        {
            var values = new float[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadFloat();

            p.SetResult(values);
        });

        return pointer;
    }

    private static ZonePointer<Vec2[]> ReadVec2ArrayPointer(
        ref XFileReadContext context,
        int count,
        string fieldPath)
    {
        var pointer = context.ReadDirectPointer<Vec2[]>(fieldPath);
        ResolveVec2ArrayPointer(ref context, pointer, count);
        return pointer;
    }

    private static void ResolveVec2ArrayPointer(
        ref XFileReadContext context,
        ZonePointer<Vec2[]> pointer,
        int count)
    {
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<Vec2[]> p) =>
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

    private static int[] ReadInt32Array(ref XFileReadContext context, int count)
    {
        var values = new int[Math.Max(0, count)];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadInt32();

        return values;
    }

    private static float[] ReadFloatArray(ref XFileReadContext context, int count)
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
