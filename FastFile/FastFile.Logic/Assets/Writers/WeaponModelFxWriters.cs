using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Assets.XModels;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private const int XModelCollSurfSize = 0x24;

    private static void WriteWeaponVariantDef(XFileWriterContext context, WeaponVariantDef weapon)
    {
        var queue = new XFileInlineWriteQueue();
        WriteWeaponVariantDef(context, queue, weapon);
        ResolveInlineQueue(context, queue);
    }

    private static void WriteXModel(XFileWriterContext context, XModel asset)
    {
        var queue = new XFileInlineWriteQueue();
        WriteXModel(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void WriteXModelSurfs(XFileWriterContext context, XModelSurfs asset)
    {
        var queue = new XFileInlineWriteQueue();
        WriteXModelSurfs(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void WritePhysPreset(XFileWriterContext context, PhysPreset asset)
    {
        var queue = new XFileInlineWriteQueue();
        WritePhysPreset(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void WritePhysCollmap(XFileWriterContext context, PhysCollmap asset)
    {
        var queue = new XFileInlineWriteQueue();
        WritePhysCollmap(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void WriteFxEffectDef(XFileWriterContext context, FxEffectDef asset)
    {
        var queue = new XFileInlineWriteQueue();
        WriteFxEffectDef(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void WriteTracerDef(XFileWriterContext context, TracerDef asset)
    {
        var queue = new XFileInlineWriteQueue();
        WriteTracerDef(context, queue, asset);
        ResolveInlineQueue(context, queue);
    }

    private static void ResolveInlineQueue(XFileWriterContext context, XFileInlineWriteQueue queue)
    {
        if (context.TryDeferInlineWrite(queue.Resolve))
            return;

        queue.Resolve();
    }

    private static void WriteWeaponVariantDef(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        WeaponVariantDef weapon)
    {
        var start = context.Position;

        WriteWeaponStringPointer(context, queue, weapon.InternalNamePtr);
        WriteWeaponPointer(context, queue, weapon.WeaponDefPtr, PointerResolutionKind.Direct, "WeaponVariantDef.WeaponDef", WriteWeaponDef);
        WriteWeaponStringPointer(context, queue, weapon.DisplayNamePtr);
        WriteWeaponUShortArrayPointer(context, queue, weapon.HideTags, "WeaponVariantDef.HideTags");
        WriteWeaponStringPointerArrayPointer(context, queue, weapon.XAnims, "WeaponVariantDef.XAnims");
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
        WriteWeaponStringPointer(context, queue, weapon.szAltWeaponName);
        context.WriteUInt32(weapon.altWeaponIndex);
        context.WriteInt32(weapon.iAltRaiseTime);
        WriteWeaponMaterialPointer(context, queue, weapon.killIcon);
        WriteWeaponMaterialPointer(context, queue, weapon.dpadIcon);
        context.WriteInt32(weapon.unknown8);
        context.WriteInt32(weapon.iFirstRaiseTime);
        context.WriteInt32(weapon.iDropAmmoMax);
        context.WriteFloat(weapon.adsDofStart);
        context.WriteFloat(weapon.adsDofEnd);
        context.WriteUInt16((ushort)weapon.accuracyGraphKnotCount);
        context.WriteUInt16((ushort)weapon.originalAccuracyGraphKnotCount);
        WriteWeaponVec2ArrayPointer(context, queue, weapon.accuracyGraphKnots, "WeaponVariantDef.AccuracyGraphKnots");
        WriteWeaponVec2ArrayPointer(context, queue, weapon.originalAccuracyGraphKnots, "WeaponVariantDef.OriginalAccuracyGraphKnots");
        context.WriteBool(weapon.motionTracker);
        context.WriteBool(weapon.enhanced);
        context.WriteBool(weapon.dpadIconShowsAmmo);
        context.WriteByte(weapon.DpadIconShowsAmmoPadding);
        EnsureFixedSize(context.Position - start, WeaponVariantDefSize, "WeaponVariantDef");
    }

    private static void WriteWeaponDef(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        WeaponDef weapon)
    {
        var start = context.Position;

        WriteWeaponStringPointer(context, queue, weapon.InternalNamePtr);
        WriteWeaponXModelPointerArrayPointer(context, queue, weapon.gunXModel, "WeaponDef.GunXModel");
        WriteWeaponXModelPointer(context, queue, weapon.handXModel);
        WriteWeaponStringPointerArrayPointer(context, queue, weapon.szXAnimsR, "WeaponDef.szXAnimsR");
        WriteWeaponStringPointerArrayPointer(context, queue, weapon.szXAnimsL, "WeaponDef.szXAnimsL");
        WriteWeaponStringPointer(context, queue, weapon.ModeNamePtr);

        for (var i = 0; i < weapon.NoteTrackMaps.Length; i++)
            WriteWeaponUShortArrayPointer(context, queue, weapon.NoteTrackMaps[i], $"WeaponDef.NoteTrackMaps[{i}]");

        WriteInt32Array(context, weapon.PlayerAnimTypeThroughStance);
        foreach (var effect in weapon.FlashEffects)
            WriteWeaponFxPointer(context, queue, effect);

        foreach (var soundAlias in weapon.SoundAliases)
            WriteWeaponSoundAliasPointer(context, queue, soundAlias, "WeaponDef.SoundAliases");
        WriteWeaponSoundAliasPointerArrayPointer(context, queue, weapon.BounceSound, "WeaponDef.BounceSound");

        foreach (var effect in weapon.EffectPointersA)
            WriteWeaponFxPointer(context, queue, effect);
        foreach (var material in weapon.MaterialPointersA)
            WriteWeaponMaterialPointer(context, queue, material);
        WriteInt32Array(context, weapon.ReticleFields);
        WriteInt32Array(context, weapon.ViewMovementRotationFields);
        WriteInt32Array(context, weapon.PositionalMovementRotationFields);

        WriteWeaponXModelPointerArrayPointer(context, queue, weapon.WorldGunXModel, "WeaponDef.WorldGunXModel");
        foreach (var model in weapon.WorldModelPointers)
            WriteWeaponXModelPointer(context, queue, model);
        WriteWeaponMaterialPointer(context, queue, weapon.AmmoCounterIcon);
        context.WriteInt32(weapon.AmmoCounterIconRatio);
        WriteWeaponMaterialPointer(context, queue, weapon.CompassIcon);
        context.WriteInt32(weapon.CompassIconRatio);
        WriteWeaponMaterialPointer(context, queue, weapon.OverlayMaterial);
        WriteInt32Array(context, weapon.OverlayFieldsA);
        WriteWeaponStringPointer(context, queue, weapon.OverlayReticle);
        context.WriteInt32(weapon.OverlayReticleField);
        WriteWeaponStringPointer(context, queue, weapon.OverlayInterface);
        WriteInt32Array(context, weapon.OverlayFieldsB);
        WriteWeaponStringPointer(context, queue, weapon.ModeNameAlt);
        WriteInt32Array(context, weapon.ModeFields);
        WriteInt32Array(context, weapon.WeaponTimingFields);
        WriteInt32Array(context, weapon.AimMovementTuningFields);

        foreach (var material in weapon.OverlayMaterials)
            WriteWeaponMaterialPointer(context, queue, material);
        WriteInt32Array(context, weapon.OverlayDimensionFields);
        WriteInt32Array(context, weapon.BobSpreadIdleSwayAdsViewErrorFields);

        WriteWeaponPhysCollmapPointer(context, queue, weapon.PhysCollmap);
        WriteInt32Array(context, weapon.PhysicsFieldsA);
        WriteInt32Array(context, weapon.PhysicsFieldsB);
        WriteInt32Array(context, weapon.PhysicsFieldsC);
        WriteInt32Array(context, weapon.PhysicsFieldsD);
        WriteWeaponXModelPointer(context, queue, weapon.ProjectileModel);
        context.WriteInt32(weapon.ProjectileModelField);
        foreach (var effect in weapon.ProjectileEffects)
            WriteWeaponFxPointer(context, queue, effect);
        foreach (var soundAlias in weapon.ProjectileSoundAliases)
            WriteWeaponSoundAliasPointer(context, queue, soundAlias, "WeaponDef.ProjectileSoundAliases");
        WriteInt32Array(context, weapon.ProjectileFieldsA);
        WriteWeaponFloatArrayPointer(context, queue, weapon.ParallelBounce, "WeaponDef.ParallelBounce");
        WriteWeaponFloatArrayPointer(context, queue, weapon.PerpendicularBounce, "WeaponDef.PerpendicularBounce");
        foreach (var effect in weapon.ImpactEffects)
            WriteWeaponFxPointer(context, queue, effect);
        WriteInt32Array(context, weapon.ImpactFieldsA);
        context.WriteInt32(weapon.ImpactFieldB);
        WriteInt32Array(context, weapon.ImpactFieldsC);
        WriteWeaponFxPointer(context, queue, weapon.ViewShellEjectEffect);
        WriteWeaponSoundAliasPointer(context, queue, weapon.ShellEjectSound, "WeaponDef.ShellEjectSound");
        WriteInt32Array(context, weapon.ShellEjectFields);
        WriteInt32Array(context, weapon.AdsHipGunKickAiDistanceFields);

        WriteWeaponStringPointer(context, queue, weapon.AccuracyGraphName0);
        WriteWeaponStringPointer(context, queue, weapon.AccuracyGraphName1);
        WriteWeaponVec2ArrayPointer(context, queue, weapon.accuracyGraphKnots, "WeaponDef.AccuracyGraphKnots");
        WriteWeaponVec2ArrayPointer(context, queue, weapon.originalAccuracyGraphKnots, "WeaponDef.OriginalAccuracyGraphKnots");
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
        WriteWeaponFloatArray(context, weapon.MinTurnSpeed);
        WriteWeaponFloatArray(context, weapon.MaxTurnSpeed);
        context.WriteFloat(weapon.PitchConvergenceTime);
        context.WriteFloat(weapon.YawConvergenceTime);
        context.WriteFloat(weapon.SuppressTime);
        context.WriteFloat(weapon.MaxRange);
        context.WriteFloat(weapon.AnimHorizontalRotateInc);
        context.WriteFloat(weapon.PlayerPositionDist);
        WriteWeaponStringPointer(context, queue, weapon.UseHintString);
        WriteWeaponStringPointer(context, queue, weapon.DropHintString);
        WriteInt32Array(context, weapon.HintFieldsA);
        WriteInt32Array(context, weapon.HintFieldsB);
        WriteWeaponStringPointer(context, queue, weapon.ScriptName);
        WriteInt32Array(context, weapon.ScriptFieldsA);
        WriteInt32Array(context, weapon.ScriptFieldsB);
        context.WriteInt32(weapon.HitLocationField);
        WriteWeaponFloatArrayPointer(context, queue, weapon.LocationDamageMultipliers, "WeaponDef.LocationDamageMultipliers");
        WriteWeaponStringPointer(context, queue, weapon.FireRumble);
        WriteWeaponStringPointer(context, queue, weapon.MeleeImpactRumble);
        WriteWeaponTracerPointer(context, queue, weapon.Tracer);

        WriteInt32Array(context, weapon.TracerFields);
        WriteWeaponSoundAliasPointer(context, queue, weapon.TurretOverheatSound, "WeaponDef.TurretOverheatSound");
        WriteWeaponFxPointer(context, queue, weapon.TurretOverheatEffect);
        WriteWeaponStringPointer(context, queue, weapon.TurretBarrelSpinRumble);
        WriteInt32Array(context, weapon.TurretFields);
        WriteWeaponSoundAliasPointer(context, queue, weapon.TurretBarrelSpinMaxSnd, "WeaponDef.TurretBarrelSpinMaxSnd");
        foreach (var soundAlias in weapon.TurretBarrelSpinUpSnd)
            WriteWeaponSoundAliasPointer(context, queue, soundAlias, "WeaponDef.TurretBarrelSpinUpSnd");
        foreach (var soundAlias in weapon.TurretBarrelSpinDownSnd)
            WriteWeaponSoundAliasPointer(context, queue, soundAlias, "WeaponDef.TurretBarrelSpinDownSnd");
        WriteWeaponSoundAliasPointer(context, queue, weapon.MissileConeSoundAlias, "WeaponDef.MissileConeSoundAlias");
        WriteWeaponSoundAliasPointer(context, queue, weapon.MissileConeSoundAliasAtBase, "WeaponDef.MissileConeSoundAliasAtBase");
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
        WriteWeaponBooleanTail(context, weapon);
        EnsureFixedSize(context.Position - start, WeaponDefSize, "WeaponDef");
    }

    private static void WriteWeaponStringPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<string>? pointer)
    {
        context.WritePointerRaw(pointer, PointerResolutionKind.Direct, "Weapon.XString");
        QueueWeaponInlinePointer(context, queue, pointer, (writeContext, _, value) => writeContext.WriteCString(value));
    }

    private static void WriteWeaponStringPointerArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<ZonePointer<string>[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, writeQueue, values) =>
        {
            foreach (var value in values)
                WriteWeaponStringPointer(writeContext, writeQueue, value);
        });
    }

    private static void WriteWeaponSoundAliasPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<string>? pointer,
        string fieldPath)
    {
        context.WritePointerRaw(pointer, PointerResolutionKind.Direct, fieldPath);
        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        queue.Add(() =>
        {
            context.RegisterMaterializedPointerValue(pointer, XFileWriteRules.PointerSize);
            var nestedName = new ZonePointer<string>(-1);
            nestedName.SetResult(pointer.Result);
            WriteWeaponStringPointer(context, queue, nestedName);
        });
    }

    private static void WriteWeaponSoundAliasPointerArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<ZonePointer<string>[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, writeQueue, values) =>
        {
            foreach (var value in values)
                WriteWeaponSoundAliasPointer(writeContext, writeQueue, value, $"{fieldPath}.Element");
        });
    }

    private static void WriteWeaponUShortArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<ushort[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, _, values) =>
        {
            foreach (var value in values)
                writeContext.WriteUInt16(value);
        });
    }

    private static void WriteWeaponFloatArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<float[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, _, values) =>
        {
            foreach (var value in values)
                writeContext.WriteFloat(value);
        });
    }

    private static void WriteWeaponVec2ArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<Vec2[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, _, values) =>
        {
            foreach (var value in values)
            {
                writeContext.WriteFloat(value.a);
                writeContext.WriteFloat(value.b);
            }
        });
    }

    private static void WriteWeaponXModelPointerArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<ZonePointer<XModel>[]>? pointer,
        string fieldPath)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, fieldPath, (writeContext, writeQueue, values) =>
        {
            foreach (var value in values)
                WriteWeaponXModelPointer(writeContext, writeQueue, value);
        });
    }

    private static void WriteWeaponXModelPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModel>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "Weapon.XModel", WriteXModel);
    }

    private static void WriteWeaponMaterialPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<Material>? pointer)
    {
        context.WritePointerRaw(pointer, PointerResolutionKind.Alias, "Weapon.Material");
        QueueWeaponInlineAssetReference(context, queue, pointer, (writeContext, _, material) => WriteMaterial(writeContext, material));
    }

    private static void WriteWeaponFxPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxEffectDef>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "Weapon.Fx", WriteFxEffectDef);
    }

    private static void WriteWeaponPhysCollmapPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<PhysCollmap>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "Weapon.PhysCollmap", WritePhysCollmap);
    }

    private static void WriteWeaponTracerPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<TracerDef>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "Weapon.Tracer", WriteTracerDef);
    }

    private static void WriteWeaponAssetReferencePointer<T>(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<T>? pointer,
        PointerResolutionKind resolutionKind,
        string fieldPath,
        Action<XFileWriterContext, XFileInlineWriteQueue, T> writer)
    {
        context.WritePointerRaw(pointer, resolutionKind, fieldPath);
        QueueWeaponInlineAssetReference(context, queue, pointer, writer);
    }

    private static void WriteWeaponPointer<T>(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<T>? pointer,
        Action<XFileWriterContext, XFileInlineWriteQueue, T> writer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Unknown, typeof(T).Name, writer);
    }

    private static void WriteWeaponPointer<T>(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<T>? pointer,
        PointerResolutionKind resolutionKind,
        string fieldPath,
        Action<XFileWriterContext, XFileInlineWriteQueue, T> writer)
    {
        context.WritePointerRaw(pointer, resolutionKind, fieldPath);
        QueueWeaponInlinePointer(context, queue, pointer, writer);
    }

    private static void QueueWeaponInlinePointer<T>(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<T>? pointer,
        Action<XFileWriterContext, XFileInlineWriteQueue, T> writer)
    {
        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        queue.Add(() =>
        {
            context.RegisterMaterializedPointerValue(pointer);
            writer(context, queue, pointer.Result);
        });
    }

    private static void QueueWeaponInlineAssetReference<T>(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<T>? pointer,
        Action<XFileWriterContext, XFileInlineWriteQueue, T> writer)
    {
        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        queue.Add(() =>
            WriteInlineAssetReferenceBody(
                context,
                pointer,
                (writeContext, value) => writer(writeContext, queue, value)));
    }

    private static void WriteXModelPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModel>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "XModelAssetRef", WriteXModel);
    }

    private static void WriteXModel(XFileWriterContext context, XFileInlineWriteQueue queue, XModel asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        context.WriteByte(asset.NumBones);
        context.WriteByte(asset.NumRootBones);
        context.WriteByte(asset.NumSurfs);
        context.WriteByte(asset.LodRampType);
        context.WriteFloat(asset.Scale);
        WriteInt32Array(context, asset.NoScalePartBits, 6);
        WriteWeaponUShortArrayPointer(context, queue, asset.BoneNames, "XModel.BoneNames");
        WriteXModelParentArrayPointer(context, queue, asset.ParentList);
        WriteXModelQuatArrayPointer(context, queue, asset.Quats);
        WriteXModelVec3ArrayPointer(context, queue, asset.Trans);
        WriteXModelPartClassificationArrayPointer(context, queue, asset.PartClassification);
        WriteXModelDObjAnimMatArrayPointer(context, queue, asset.BaseMat);
        WriteXModelMaterialHandleArrayPointer(context, queue, asset.MaterialHandles);

        for (var i = 0; i < 4; i++)
            WriteXModelLodInfo(context, queue, GetFixedValue(asset.LodInfo, i, nameof(asset.LodInfo)));

        context.WriteByte(asset.MaxLoadedLod);
        context.WriteByte(asset.NumLods);
        context.WriteByte(asset.CollLod);
        context.WriteByte(asset.Flags);
        WriteXModelCollSurfArrayPointer(context, queue, asset.CollSurfs);
        context.WriteInt32(asset.NumCollSurfs);
        context.WriteInt32(asset.Contents);
        WriteXBoneInfoArrayPointer(context, queue, asset.BoneInfo);
        context.WriteFloat(asset.Radius);
        context.WriteBounds(asset.Bounds);
        WriteWeaponUShortArrayPointer(context, queue, asset.InvHighMipRadius, "XModel.InvHighMipRadius");
        context.WriteInt32(asset.MemUsage);
        WritePhysPresetPointer(context, queue, asset.PhysPreset);
        WritePhysCollmapPointer(context, queue, asset.PhysCollmap);
    }

    private static void WriteXModelLodInfo(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        XModelLodInfo lodInfo)
    {
        context.WriteFloat(lodInfo.Dist);
        context.WriteUInt16(lodInfo.NumSurfs);
        context.WriteUInt16(lodInfo.SurfIndex);
        WriteXModelSurfsPointer(context, queue, lodInfo.ModelSurfs);
        WriteInt32Array(context, lodInfo.PartBits, 6);
        context.WritePointerRaw(lodInfo.Surfs, PointerResolutionKind.Direct, "XModelLodInfo.Surfs");
    }

    private static void WriteXModelParentArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModelParent[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.ParentList", (writeContext, _, values) =>
        {
            foreach (var value in values)
                writeContext.WriteByte(value.BoneIndex);
        });
    }

    private static void WriteXModelQuatArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModelQuat[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.Quats", (writeContext, _, values) =>
        {
            foreach (var value in values)
            {
                writeContext.WriteInt16(value.X);
                writeContext.WriteInt16(value.Y);
                writeContext.WriteInt16(value.Z);
                writeContext.WriteInt16(value.W);
            }
        });
    }

    private static void WriteXModelVec3ArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<Vec3[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.Trans", (writeContext, _, values) =>
        {
            foreach (var value in values)
                writeContext.WriteVec3(value);
        });
    }

    private static void WriteXModelPartClassificationArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModelPartClassification[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.PartClassification", (writeContext, _, values) =>
        {
            foreach (var value in values)
                writeContext.WriteByte(value.Value);
        });
    }

    private static void WriteXModelDObjAnimMatArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<DObjAnimMat[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.BaseMat", (writeContext, _, values) =>
        {
            foreach (var value in values)
            {
                writeContext.WriteVec4(value.Quat);
                writeContext.WriteVec3(value.Trans);
                writeContext.WriteFloat(value.TransWeight);
            }
        });
    }

    private static void WriteXModelMaterialHandleArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<ZonePointer<Material>[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.MaterialHandles", (writeContext, writeQueue, values) =>
        {
            foreach (var value in values)
                WriteWeaponMaterialPointer(writeContext, writeQueue, value);
        });
    }

    private static void WriteXModelCollSurfArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModelCollSurf[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.CollSurfs", (writeContext, _, values) =>
        {
            foreach (var value in values)
                WriteXModelCollSurf(writeContext, value);
        });
    }

    private static void WriteXModelCollSurf(XFileWriterContext context, XModelCollSurf value)
    {
        var bytes = value.RawBytes;
        if (bytes is { Length: XModelCollSurfSize })
        {
            context.WriteBytes(bytes);
            return;
        }

        context.WriteZeroes(XModelCollSurfSize);
    }

    private static void WriteXBoneInfoArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XBoneInfo[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "XModel.BoneInfo", (writeContext, _, values) =>
        {
            foreach (var value in values)
            {
                writeContext.WriteBounds(value.Bounds);
                writeContext.WriteFloat(value.RadiusSquared);
            }
        });
    }

    private static void WriteXModelSurfsPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<XModelSurfs>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "XModelSurfsAssetRef", WriteXModelSurfs);
    }

    private static void WriteXModelSurfs(XFileWriterContext context, XFileInlineWriteQueue queue, XModelSurfs asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        context.WritePointerRaw(asset.Surfs, PointerResolutionKind.Direct, "XModelSurfs.Surfs");
        context.WriteUInt16(asset.NumSurfs);
        context.WriteUInt16(asset.PartBitsAlignment);
        WriteInt32Array(context, asset.PartBits, 6);
    }

    private static void WritePhysPresetPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<PhysPreset>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "PhysPresetAssetRef", WritePhysPreset);
    }

    private static void WritePhysPreset(XFileWriterContext context, XFileInlineWriteQueue queue, PhysPreset asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        context.WriteInt32(asset.PresetType);
        context.WriteFloat(asset.Mass);
        context.WriteFloat(asset.Bounce);
        context.WriteFloat(asset.Friction);
        context.WriteFloat(asset.BulletForceScale);
        context.WriteFloat(asset.ExplosiveForceScale);
        WriteWeaponStringPointer(context, queue, asset.SndAliasPrefix);
        context.WriteFloat(asset.PiecesSpreadFraction);
        context.WriteFloat(asset.PiecesUpwardVelocity);
        context.WriteBool(asset.TempDefaultToCylinder);
        context.WriteBool(asset.PerSurfaceSndAlias);
        context.WriteUInt16(asset.BoolAlignmentPadding);
    }

    private static void WritePhysCollmapPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<PhysCollmap>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "PhysCollmapAssetRef", WritePhysCollmap);
    }

    private static void WritePhysCollmap(XFileWriterContext context, XFileInlineWriteQueue queue, PhysCollmap asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        context.WriteUInt32(asset.Count);
        context.WritePointerRaw(asset.Geoms, PointerResolutionKind.Direct, "PhysCollmap.Geoms");
        WritePhysMass(context, asset.Mass);
        context.WriteBounds(asset.Bounds);
    }

    private static void WritePhysMass(XFileWriterContext context, PhysMass mass)
    {
        context.WriteVec3(mass.CenterOfMass);
        context.WriteVec3(mass.MomentsOfInertia);
        context.WriteVec3(mass.ProductsOfInertia);
    }

    private static void WriteTracerPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<TracerDef>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "TracerAssetRef", WriteTracerDef);
    }

    private static void WriteTracerDef(XFileWriterContext context, XFileInlineWriteQueue queue, TracerDef asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        WriteWeaponMaterialPointer(context, queue, asset.Material);
        context.WriteUInt32(asset.DrawInterval);
        context.WriteFloat(asset.Speed);
        context.WriteFloat(asset.BeamLength);
        context.WriteFloat(asset.BeamWidth);
        context.WriteFloat(asset.ScrewRadius);
        context.WriteFloat(asset.ScrewDist);
        foreach (var color in asset.Colors)
            context.WriteVec4(color);
    }

    private static void WriteFxPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxEffectDef>? pointer)
    {
        WriteWeaponAssetReferencePointer(context, queue, pointer, PointerResolutionKind.Alias, "FxEffectAssetRef", WriteFxEffectDef);
    }

    private static void WriteFxEffectDef(XFileWriterContext context, XFileInlineWriteQueue queue, FxEffectDef asset)
    {
        WriteWeaponStringPointer(context, queue, asset.NamePtr);
        context.WriteInt32(asset.Flags);
        context.WriteInt32(asset.TotalSize);
        context.WriteInt32(asset.MsecLoopingLife);
        context.WriteInt32(asset.ElemDefCountLooping);
        context.WriteInt32(asset.ElemDefCountOneShot);
        context.WriteInt32(asset.ElemDefCountEmission);
        WriteFxElemDefArrayPointer(context, queue, asset.ElemDefs);
    }

    private static void WriteFxElemDefArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxElemDef[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "FxEffectDef.ElemDefs", (writeContext, writeQueue, values) =>
        {
            foreach (var value in values)
                WriteFxElemDef(writeContext, writeQueue, value);
        });
    }

    private static void WriteFxElemDef(XFileWriterContext context, XFileInlineWriteQueue queue, FxElemDef elem)
    {
        context.WriteInt32(elem.Flags);
        WriteFxSpawnDef(context, elem.Spawn);
        WriteFxFloatRange(context, elem.SpawnRange);
        WriteFxFloatRange(context, elem.FadeInRange);
        WriteFxFloatRange(context, elem.FadeOutRange);
        context.WriteFloat(elem.SpawnFrustumCullRadius);
        WriteFxIntRange(context, elem.SpawnDelayMsec);
        WriteFxIntRange(context, elem.LifeSpanMsec);
        foreach (var value in elem.SpawnOrigin)
            WriteFxFloatRange(context, value);
        WriteFxFloatRange(context, elem.SpawnOffsetRadius);
        WriteFxFloatRange(context, elem.SpawnOffsetHeight);
        foreach (var value in elem.SpawnAngles)
            WriteFxFloatRange(context, value);
        foreach (var value in elem.AngularVelocity)
            WriteFxFloatRange(context, value);
        WriteFxFloatRange(context, elem.InitialRotation);
        WriteFxFloatRange(context, elem.Gravity);
        WriteFxFloatRange(context, elem.ReflectionFactor);
        WriteFxElemAtlas(context, elem.Atlas);
        context.WriteByte(elem.ElemType);
        context.WriteByte(elem.VisualCount);
        context.WriteByte(elem.VelIntervalCount);
        context.WriteByte(elem.VisStateIntervalCount);
        WriteFxVelSamplesPointer(context, queue, elem.VelSamples);
        WriteFxVisSamplesPointer(context, queue, elem.VisSamples);
        WriteFxVisualsPointer(context, queue, elem);
        context.WriteBounds(elem.CollBounds);
        WriteFxEffectDefRefPointer(context, queue, elem.EffectOnImpact);
        WriteFxEffectDefRefPointer(context, queue, elem.EffectOnDeath);
        WriteFxEffectDefRefPointer(context, queue, elem.EffectEmitted);
        WriteFxFloatRange(context, elem.EmitDist);
        WriteFxFloatRange(context, elem.EmitDistVariance);
        WriteFxExtendedPointer(context, queue, elem);
        context.WriteByte(elem.SortOrder);
        context.WriteByte(elem.LightingFrac);
        context.WriteByte(elem.UseItemClip);
        context.WriteByte(elem.FadeInfo);
    }

    private static void WriteFxVelSamplesPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxElemVelStateSample[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "FxElemDef.VelSamples", (writeContext, _, values) =>
        {
            foreach (var value in values)
                WriteFxVelStateSample(writeContext, value);
        });
    }

    private static void WriteFxVisSamplesPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxElemVisStateSample[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "FxElemDef.VisSamples", (writeContext, _, values) =>
        {
            foreach (var value in values)
                WriteFxVisStateSample(writeContext, value);
        });
    }

    private static void WriteFxVisualsPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        FxElemDef elem)
    {
        if (!UsesFxVisualArrayPointer(elem))
        {
            WriteFxElemVisual(context, queue, elem.ElemType, GetFxSingleVisual(elem));
            return;
        }

        WriteWeaponPointer(context, queue, elem.Visuals, PointerResolutionKind.Direct, "FxElemDef.Visuals", (writeContext, writeQueue, values) =>
        {
            foreach (var visual in values)
                WriteFxElemVisual(writeContext, writeQueue, elem.ElemType, visual);
        });
    }

    private static bool UsesFxVisualArrayPointer(FxElemDef elem)
    {
        return elem.ElemType == 0xB || elem.VisualCount > 1;
    }

    private static FxElemVisual GetFxSingleVisual(FxElemDef elem)
    {
        return elem.Visuals is { Result.Length: > 0 } pointer
            ? pointer.Result[0]
            : throw new InvalidDataException("FxElemDef.Visuals is missing the in-place visual.");
    }

    private static void WriteFxElemVisual(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        byte elemType,
        FxElemVisual visual)
    {
        switch (elemType)
        {
            case 0x7:
                WriteXModelPointer(context, queue, visual.Model);
                break;
            case 0xC:
                WriteFxEffectDefRef(context, queue, visual.EffectDef);
                break;
            case 0xA:
                WriteWeaponStringPointer(context, queue, visual.SoundName);
                break;
            case 0x8:
            case 0x9:
                context.WritePointerRaw(visual.Anonymous, PointerResolutionKind.Direct, "FxElemVisual.Anonymous");
                break;
            case 0xB:
                WriteWeaponMaterialPointer(context, queue, visual.DecalMaterial0);
                WriteWeaponMaterialPointer(context, queue, visual.DecalMaterial1);
                break;
            default:
                WriteWeaponMaterialPointer(context, queue, visual.Material);
                break;
        }
    }

    private static void WriteFxEffectDefRefPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxEffectDefRef>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "FxElemDef.EffectDefRef", (writeContext, writeQueue, value) =>
        {
            WriteFxEffectDefRef(writeContext, writeQueue, value);
        });
    }

    private static void WriteFxEffectDefRef(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        FxEffectDefRef? reference)
    {
        if (reference is null)
        {
            context.WriteNullPointer();
            return;
        }

        if (reference.Name is not null)
        {
            WriteWeaponStringPointer(context, queue, reference.Name);
            return;
        }

        if (reference.Handle is not null)
        {
            context.WritePointerRaw(reference.Handle, PointerResolutionKind.Alias, "FxEffectDefRef.Handle");
            return;
        }

        context.WriteNullPointer();
    }

    private static void WriteFxExtendedPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        FxElemDef elem)
    {
        WriteWeaponPointer(context, queue, elem.Extended, PointerResolutionKind.Direct, "FxElemDef.Extended", (writeContext, writeQueue, value) =>
        {
            WriteFxExtended(writeContext, writeQueue, elem.ElemType, value);
        });
    }

    private static void WriteFxExtended(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        byte elemType,
        FxElemExtendedDef value)
    {
        switch (elemType)
        {
            case 0x3:
                WriteFxTrailDef(context, queue, value.TrailDef);
                break;
            case 0x6:
                WriteFxSparkFountainDef(context, value.SparkFountainDef);
                break;
            default:
                context.WriteByte(value.UnknownDef);
                break;
        }
    }

    private static void WriteFxTrailDef(XFileWriterContext context, XFileInlineWriteQueue queue, FxTrailDef value)
    {
        context.WriteInt32(value.ScrollTimeMsec);
        context.WriteInt32(value.RepeatDist);
        context.WriteFloat(value.InvSplitDist);
        context.WriteFloat(value.InvSplitArcDist);
        context.WriteFloat(value.InvSplitTime);
        context.WriteInt32(value.VertCount);
        WriteFxTrailVertexArrayPointer(context, queue, value.Verts);
        context.WriteInt32(value.IndCount);
        WriteWeaponUShortArrayPointer(context, queue, value.Inds, "FxTrailDef.Inds");
    }

    private static void WriteFxTrailVertexArrayPointer(
        XFileWriterContext context,
        XFileInlineWriteQueue queue,
        ZonePointer<FxTrailVertex[]>? pointer)
    {
        WriteWeaponPointer(context, queue, pointer, PointerResolutionKind.Direct, "FxTrailDef.Verts", (writeContext, _, values) =>
        {
            foreach (var value in values)
            {
                writeContext.WriteFloat(value.Pos0);
                writeContext.WriteFloat(value.Pos1);
                writeContext.WriteFloat(value.Normal0);
                writeContext.WriteFloat(value.Normal1);
                writeContext.WriteFloat(value.TexCoord);
            }
        });
    }

    private static void WriteFxSparkFountainDef(XFileWriterContext context, FxSparkFountainDef value)
    {
        context.WriteFloat(value.Gravity);
        context.WriteFloat(value.BounceFrac);
        context.WriteFloat(value.BounceRand);
        context.WriteFloat(value.SparkSpacing);
        context.WriteFloat(value.SparkLength);
        context.WriteInt32(value.SparkCount);
        context.WriteFloat(value.LoopTime);
        context.WriteFloat(value.VelMin);
        context.WriteFloat(value.VelMax);
        context.WriteFloat(value.VelConeFrac);
        context.WriteFloat(value.RestSpeed);
        context.WriteFloat(value.BoostTime);
        context.WriteFloat(value.BoostFactor);
    }

    private static void WriteFxSpawnDef(XFileWriterContext context, FxSpawnDef value)
    {
        context.WriteInt32(value.LoopingIntervalMsec);
        context.WriteInt32(value.Count);
    }

    private static void WriteFxIntRange(XFileWriterContext context, FxIntRange value)
    {
        context.WriteInt32(value.Base);
        context.WriteInt32(value.Amplitude);
    }

    private static void WriteFxFloatRange(XFileWriterContext context, FxFloatRange value)
    {
        context.WriteFloat(value.Base);
        context.WriteFloat(value.Amplitude);
    }

    private static void WriteFxElemAtlas(XFileWriterContext context, FxElemAtlas value)
    {
        context.WriteByte(value.Behavior);
        context.WriteByte(value.Index);
        context.WriteByte(value.Fps);
        context.WriteByte(value.LoopCount);
        context.WriteByte(value.ColIndexBits);
        context.WriteByte(value.RowIndexBits);
        context.WriteInt16(value.EntryCount);
    }

    private static void WriteFxVelStateSample(XFileWriterContext context, FxElemVelStateSample value)
    {
        WriteFxVelStateInFrame(context, value.Local);
        WriteFxVelStateInFrame(context, value.World);
    }

    private static void WriteFxVelStateInFrame(XFileWriterContext context, FxElemVelStateInFrame value)
    {
        WriteFxVec3Range(context, value.Velocity);
        WriteFxVec3Range(context, value.TotalDelta);
    }

    private static void WriteFxVec3Range(XFileWriterContext context, FxElemVec3Range value)
    {
        context.WriteVec3(value.Base);
        context.WriteVec3(value.Amplitude);
    }

    private static void WriteFxVisStateSample(XFileWriterContext context, FxElemVisStateSample value)
    {
        WriteFxVisualState(context, value.Base);
        WriteFxVisualState(context, value.Amplitude);
    }

    private static void WriteFxVisualState(XFileWriterContext context, FxElemVisualState value)
    {
        context.WriteByte(value.Color.R);
        context.WriteByte(value.Color.G);
        context.WriteByte(value.Color.B);
        context.WriteByte(value.Color.A);
        context.WriteFloat(value.RotationDelta);
        context.WriteFloat(value.RotationTotal);
        context.WriteFloat(value.Size0);
        context.WriteFloat(value.Size1);
        context.WriteFloat(value.Scale);
    }

    private static T GetFixedValue<T>(T[]? values, int index, string fieldName)
    {
        if (values is null || values.Length <= index || values[index] is null)
            throw new InvalidDataException($"{fieldName} is missing element {index:N0}.");

        return values[index];
    }

    private static void WriteWeaponFloatArray(XFileWriterContext context, float[]? values)
    {
        foreach (var value in values ?? [])
            context.WriteFloat(value);
    }

    private static void WriteWeaponBooleanTail(XFileWriterContext context, WeaponDef weapon)
    {
        if (weapon.BooleanTailBytes is { Length: 0x30 } bytes)
        {
            context.WriteBytes(bytes);
            return;
        }

        context.WriteBool(weapon.SharedAmmo);
        context.WriteBool(weapon.LockonSupported);
        context.WriteBool(weapon.RequireLockonToFire);
        context.WriteBool(weapon.BigExplosion);
        WriteWeaponBooleanFlags(context, weapon.BooleanFlags);
    }

    private static void WriteWeaponBooleanFlags(XFileWriterContext context, WeaponBooleanFlags flags)
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
