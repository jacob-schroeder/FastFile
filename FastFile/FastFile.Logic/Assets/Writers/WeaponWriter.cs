using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Assets.Weapons.Enums;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Writers;

internal static class WeaponWriter
{
    private const int WeaponVariantDefSize = 0x74;
    private const int WeaponDefSize = 0x684;

    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteWeaponVariantDef(context, (WeaponVariantDef)asset);
    }

    private static void WriteWeaponVariantDef(ZoneWriterContext context, WeaponVariantDef weapon)
    {
        var start = context.Position;

        GenericWriter.WriteStringPointer(context, weapon.InternalNamePtr);
        context.WritePointer(weapon.WeaponDefPtr, (pointerContext, pointer) =>
        {
            if (pointer.Result is { } value)
                WriteWeaponDef(pointerContext, value);
        });
        GenericWriter.WriteStringPointer(context, weapon.DisplayNamePtr);
        WriteUShortArrayPointer(context, weapon.HideTags);
        GenericWriter.WriteStringPointerArrayPointer(context, weapon.XAnims);
        context.WriteFloat(weapon.fAdsZoomFov);
        context.WriteInt32(weapon.iAdsTransInTime);
        context.WriteInt32(weapon.iAdsTransOutTime);
        context.WriteInt32(weapon.iClipSize);
        context.WriteInt32((int)weapon.impactType);
        context.WriteInt32(weapon.iFireTime);
        context.WriteInt32((int)weapon.dpadIconRatio);
        context.WriteFloat(weapon.fPenetrateMultiplier);
        context.WriteFloat(weapon.fAdsViewKickCenterSpeed);
        context.WriteFloat(weapon.fHipViewKickCenterSpeed);
        GenericWriter.WriteStringPointer(context, weapon.szAltWeaponName);
        context.WriteUInt32(weapon.altWeaponIndex);
        context.WriteInt32(weapon.iAltRaiseTime);
        WriteMaterialPointer(context, weapon.killIcon);
        WriteMaterialPointer(context, weapon.dpadIcon);
        context.WriteInt32(weapon.unknown8);
        context.WriteInt32(weapon.iFirstRaiseTime);
        context.WriteInt32(weapon.iDropAmmoMax);
        context.WriteFloat(weapon.adsDofStart);
        context.WriteFloat(weapon.adsDofEnd);
        context.WriteUInt16((ushort)weapon.accuracyGraphKnotCount);
        context.WriteUInt16((ushort)weapon.originalAccuracyGraphKnotCount);
        WriteVec2ArrayPointer(context, weapon.accuracyGraphKnots);
        WriteVec2ArrayPointer(context, weapon.originalAccuracyGraphKnots);
        context.WriteBool(weapon.motionTracker);
        context.WriteBool(weapon.enhanced);
        context.WriteBool(weapon.dpadIconShowsAmmo);
        context.WriteByte(weapon.DpadIconShowsAmmoPadding);
        EnsureFixedSize(context.Position - start, WeaponVariantDefSize, "WeaponVariantDef");
    }

    private static void WriteWeaponDef(ZoneWriterContext context, WeaponDef weapon)
    {
        var start = context.Position;

        GenericWriter.WriteStringPointer(context, weapon.InternalNamePtr);
        WriteXModelPointerArrayPointer(context, weapon.gunXModel);
        WriteXModelPointer(context, weapon.handXModel);
        GenericWriter.WriteStringPointerArrayPointer(context, weapon.szXAnimsR);
        GenericWriter.WriteStringPointerArrayPointer(context, weapon.szXAnimsL);
        GenericWriter.WriteStringPointer(context, weapon.ModeNamePtr);

        foreach (var noteTrackMap in weapon.NoteTrackMaps)
            WriteUShortArrayPointer(context, noteTrackMap);

        WriteInt32Array(context, weapon.PlayerAnimTypeThroughStance);
        foreach (var effect in weapon.FlashEffects)
            WriteFxPointer(context, effect);

        foreach (var soundAlias in weapon.SoundAliases)
            GenericWriter.WriteStringPointer(context, soundAlias);
        GenericWriter.WriteStringPointerArrayPointer(context, weapon.BounceSound);

        foreach (var effect in weapon.EffectPointersA)
            WriteFxPointer(context, effect);
        foreach (var material in weapon.MaterialPointersA)
            WriteMaterialPointer(context, material);
        WriteInt32Array(context, weapon.ReticleFields);
        WriteInt32Array(context, weapon.ViewMovementRotationFields);
        WriteInt32Array(context, weapon.PositionalMovementRotationFields);

        WriteXModelPointerArrayPointer(context, weapon.WorldGunXModel);
        foreach (var model in weapon.WorldModelPointers)
            WriteXModelPointer(context, model);
        WriteMaterialPointer(context, weapon.AmmoCounterIcon);
        context.WriteInt32(weapon.AmmoCounterIconRatio);
        WriteMaterialPointer(context, weapon.CompassIcon);
        context.WriteInt32(weapon.CompassIconRatio);
        WriteMaterialPointer(context, weapon.OverlayMaterial);
        WriteInt32Array(context, weapon.OverlayFieldsA);
        GenericWriter.WriteStringPointer(context, weapon.OverlayReticle);
        context.WriteInt32(weapon.OverlayReticleField);
        GenericWriter.WriteStringPointer(context, weapon.OverlayInterface);
        WriteInt32Array(context, weapon.OverlayFieldsB);
        GenericWriter.WriteStringPointer(context, weapon.ModeNameAlt);
        WriteInt32Array(context, weapon.ModeFields);
        WriteInt32Array(context, weapon.WeaponTimingFields);
        WriteInt32Array(context, weapon.AimMovementTuningFields);

        foreach (var material in weapon.OverlayMaterials)
            WriteMaterialPointer(context, material);
        WriteInt32Array(context, weapon.OverlayDimensionFields);
        WriteInt32Array(context, weapon.BobSpreadIdleSwayAdsViewErrorFields);

        WritePhysCollmapPointer(context, weapon.PhysCollmap);
        WriteInt32Array(context, weapon.PhysicsFieldsA);
        WriteInt32Array(context, weapon.PhysicsFieldsB);
        WriteInt32Array(context, weapon.PhysicsFieldsC);
        WriteInt32Array(context, weapon.PhysicsFieldsD);
        WriteXModelPointer(context, weapon.ProjectileModel);
        context.WriteInt32(weapon.ProjectileModelField);
        foreach (var effect in weapon.ProjectileEffects)
            WriteFxPointer(context, effect);
        foreach (var soundAlias in weapon.ProjectileSoundAliases)
            GenericWriter.WriteStringPointer(context, soundAlias);
        WriteInt32Array(context, weapon.ProjectileFieldsA);
        WriteFloatArrayPointer(context, weapon.ParallelBounce);
        WriteFloatArrayPointer(context, weapon.PerpendicularBounce);
        foreach (var effect in weapon.ImpactEffects)
            WriteFxPointer(context, effect);
        WriteInt32Array(context, weapon.ImpactFieldsA);
        context.WriteInt32(weapon.ImpactFieldB);
        WriteInt32Array(context, weapon.ImpactFieldsC);
        WriteFxPointer(context, weapon.ViewShellEjectEffect);
        GenericWriter.WriteStringPointer(context, weapon.ShellEjectSound);
        WriteInt32Array(context, weapon.ShellEjectFields);
        WriteInt32Array(context, weapon.AdsHipGunKickAiDistanceFields);

        GenericWriter.WriteStringPointer(context, weapon.AccuracyGraphName0);
        GenericWriter.WriteStringPointer(context, weapon.AccuracyGraphName1);
        WriteVec2ArrayPointer(context, weapon.accuracyGraphKnots);
        WriteVec2ArrayPointer(context, weapon.originalAccuracyGraphKnots);
        context.WriteUInt16(weapon.accuracyGraphKnotCount);
        context.WriteUInt16(weapon.originalAccuracyGraphKnotCount);
        context.WriteInt32(weapon.AccuracyGraphField);
        context.WriteFloat(weapon.LeftArc);
        context.WriteFloat(weapon.RightArc);
        context.WriteFloat(weapon.TopArc);
        context.WriteFloat(weapon.BottomArc);
        context.WriteFloat(weapon.Accuracy);
        context.WriteFloat(weapon.AiSpread);
        context.WriteFloat(weapon.PlayerSpread);
        WriteFloatArray(context, weapon.MinTurnSpeed);
        WriteFloatArray(context, weapon.MaxTurnSpeed);
        context.WriteFloat(weapon.PitchConvergenceTime);
        context.WriteFloat(weapon.YawConvergenceTime);
        context.WriteFloat(weapon.SuppressTime);
        context.WriteFloat(weapon.MaxRange);
        context.WriteFloat(weapon.AnimHorizontalRotateInc);
        context.WriteFloat(weapon.PlayerPositionDist);
        GenericWriter.WriteStringPointer(context, weapon.UseHintString);
        GenericWriter.WriteStringPointer(context, weapon.DropHintString);
        WriteInt32Array(context, weapon.HintFieldsA);
        WriteInt32Array(context, weapon.HintFieldsB);
        GenericWriter.WriteStringPointer(context, weapon.ScriptName);
        WriteInt32Array(context, weapon.ScriptFieldsA);
        WriteInt32Array(context, weapon.ScriptFieldsB);
        context.WriteInt32(weapon.HitLocationField);
        WriteFloatArrayPointer(context, weapon.LocationDamageMultipliers);
        GenericWriter.WriteStringPointer(context, weapon.FireRumble);
        GenericWriter.WriteStringPointer(context, weapon.MeleeImpactRumble);
        WriteTracerPointer(context, weapon.Tracer);

        WriteInt32Array(context, weapon.TracerFields);
        GenericWriter.WriteStringPointer(context, weapon.TurretOverheatSound);
        WriteFxPointer(context, weapon.TurretOverheatEffect);
        GenericWriter.WriteStringPointer(context, weapon.TurretBarrelSpinRumble);
        WriteInt32Array(context, weapon.TurretFields);
        GenericWriter.WriteStringPointer(context, weapon.TurretBarrelSpinMaxSnd);
        foreach (var soundAlias in weapon.TurretBarrelSpinUpSnd)
            GenericWriter.WriteStringPointer(context, soundAlias);
        foreach (var soundAlias in weapon.TurretBarrelSpinDownSnd)
            GenericWriter.WriteStringPointer(context, soundAlias);
        GenericWriter.WriteStringPointer(context, weapon.MissileConeSoundAlias);
        GenericWriter.WriteStringPointer(context, weapon.MissileConeSoundAliasAtBase);
        context.WriteFloat(weapon.MissileConeSoundRadiusAtTop);
        context.WriteFloat(weapon.MissileConeSoundRadiusAtBase);
        context.WriteFloat(weapon.MissileConeSoundHeight);
        context.WriteFloat(weapon.MissileConeSoundOriginOffset);
        context.WriteFloat(weapon.MissileConeSoundVolumescaleAtCore);
        context.WriteFloat(weapon.MissileConeSoundVolumescaleAtEdge);
        context.WriteFloat(weapon.MissileConeSoundVolumescaleCoreSize);
        context.WriteFloat(weapon.MissileConeSoundPitchAtTop);
        context.WriteFloat(weapon.MissileConeSoundPitchAtBottom);
        context.WriteFloat(weapon.MissileConeSoundPitchTopSize);
        context.WriteFloat(weapon.MissileConeSoundPitchBottomSize);
        context.WriteFloat(weapon.MissileConeSoundCrossfadeTopSize);
        context.WriteFloat(weapon.MissileConeSoundCrossfadeBottomSize);
        context.WriteBool(weapon.SharedAmmo);
        context.WriteBool(weapon.LockonSupported);
        context.WriteBool(weapon.RequireLockonToFire);
        context.WriteBool(weapon.BigExplosion);
        WriteWeaponBooleanFlags(context, weapon.BooleanFlags);
        EnsureFixedSize(context.Position - start, WeaponDefSize, "WeaponDef");
    }

    private static void WriteUShortArrayPointer(ZoneWriterContext context, ZonePointer<ushort[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteUInt16(value);
        });
    }

    private static void WriteFloatArrayPointer(ZoneWriterContext context, ZonePointer<float[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteFloat(value);
        });
    }

    private static void WriteVec2ArrayPointer(ZoneWriterContext context, ZonePointer<Vec2[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
            {
                pointerContext.WriteFloat(value.a);
                pointerContext.WriteFloat(value.b);
            }
        });
    }

    private static void WriteXModelPointerArrayPointer(
        ZoneWriterContext context,
        ZonePointer<ZonePointer<XModel>[]>? pointer)
    {
        XModelWriter.WriteXModelPointerArrayPointer(context, pointer);
    }

    private static void WriteXModelPointer(ZoneWriterContext context, ZonePointer<XModel>? pointer)
    {
        XModelWriter.WriteXModelPointer(context, pointer);
    }

    private static void WriteMaterialPointer(ZoneWriterContext context, ZonePointer<MaterialAsset>? pointer)
    {
        MaterialWriter.WriteMaterialPointer(context, pointer);
    }

    private static void WriteFxPointer(ZoneWriterContext context, ZonePointer<FxEffectDef>? pointer)
    {
        FxWriter.WriteFxPointer(context, pointer);
    }

    private static void WritePhysCollmapPointer(ZoneWriterContext context, ZonePointer<PhysCollmap>? pointer)
    {
        PhysicsWriter.WritePhysCollmapPointer(context, pointer);
    }

    private static void WriteTracerPointer(ZoneWriterContext context, ZonePointer<TracerDef>? pointer)
    {
        TracerWriter.WriteTracerPointer(context, pointer);
    }

    private static void WriteReferencePointer<T>(
        ZoneWriterContext context,
        ZonePointer<T>? pointer,
        string typeName)
    {
        context.WritePointer(pointer, (_, p) =>
        {
            throw new InvalidDataException($"Inline {typeName} writing is not implemented.");
        });
    }

    private static void WriteInt32Array(ZoneWriterContext context, int[]? values)
    {
        foreach (var value in values ?? [])
            context.WriteInt32(value);
    }

    private static void WriteFloatArray(ZoneWriterContext context, float[]? values)
    {
        foreach (var value in values ?? [])
            context.WriteFloat(value);
    }

    private static void WriteWeaponBooleanFlags(ZoneWriterContext context, WeaponBooleanFlags flags)
    {
        context.WriteBool(flags.NoAdsWhenMagEmpty);
        context.WriteBool(flags.AvoidDropCleanup);
        context.WriteBool(flags.InheritsPerks);
        context.WriteBool(flags.CrosshairColorChange);
        context.WriteBool(flags.RifleBullet);
        context.WriteBool(flags.ArmorPiercing);
        context.WriteBool(flags.BoltAction);
        context.WriteBool(flags.AimDownSight);
        context.WriteBool(flags.RechamberWhileAds);
        context.WriteBool(flags.BulletExplosiveDamage);
        context.WriteBool(flags.CookOffHold);
        context.WriteBool(flags.ClipOnly);
        context.WriteBool(flags.NoAmmoPickup);
        context.WriteBool(flags.AdsFireOnly);
        context.WriteBool(flags.CancelAutoHolsterWhenEmpty);
        context.WriteBool(flags.DisableSwitchToWhenEmpty);
        context.WriteBool(flags.SuppressAmmoReserveDisplay);
        context.WriteBool(flags.LaserSightDuringNightvision);
        context.WriteBool(flags.MarkableViewmodel);
        context.WriteBool(flags.NoDualWield);
        context.WriteBool(flags.FlipKillIcon);
        context.WriteBool(flags.NoPartialReload);
        context.WriteBool(flags.SegmentedReload);
        context.WriteBool(flags.BlocksProne);
        context.WriteBool(flags.Silenced);
        context.WriteBool(flags.IsRollingGrenade);
        context.WriteBool(flags.ProjExplosionEffectForceNormalUp);
        context.WriteBool(flags.ProjImpactExplode);
        context.WriteBool(flags.StickToPlayers);
        context.WriteBool(flags.HasDetonator);
        context.WriteBool(flags.DisableFiring);
        context.WriteBool(flags.TimedDetonation);
        context.WriteBool(flags.Rotate);
        context.WriteBool(flags.HoldButtonToThrow);
        context.WriteBool(flags.FreezeMovementWhenFiring);
        context.WriteBool(flags.ThermalScope);
        context.WriteBool(flags.AltModeSameWeapon);
        context.WriteBool(flags.TurretBarrelSpinEnabled);
        context.WriteBool(flags.MissileConeSoundEnabled);
        context.WriteBool(flags.MissileConeSoundPitchshiftEnabled);
        context.WriteBool(flags.MissileConeSoundCrossfadeEnabled);
        context.WriteBool(flags.OffhandHoldIsCancelable);
        context.WriteByte(flags.Ps3TailFlag0);
        context.WriteByte(flags.Ps3TailFlag1);
    }

    private static void EnsureFixedSize(
        int written,
        int expectedSize,
        string typeName)
    {
        if (written != expectedSize)
            throw new InvalidDataException($"{typeName} wrote 0x{written:X} bytes; expected 0x{expectedSize:X}.");
    }
}
