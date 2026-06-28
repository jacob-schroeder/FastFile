using System.Text;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Weapon;
using FastFile.Models.Codecs;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.Weapon;

public sealed class WeaponEmitter : IXAssetEmitter<WeaponAsset>
{
    public IXAssetCodecContract Contract => WeaponCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, WeaponAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        WeaponVariantDef variant = asset.Variant;
        _ = Required(variant.InternalName, "WeaponVariantDef.internalName");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress root = context.Blocks.AllocateCurrent(WeaponVariantDef.SerializedSize);
            EmitWeaponVariantRoot(context, variant);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitWeaponVariantChildren(context, root, variant);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"Weapon emitted source=0x{sourceOffset:X} name='{variant.InternalName}' blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitWeaponVariantRoot(XEmitContext context, WeaponVariantDef variant)
    {
        ValidateCountIfPresent(variant.HideTags, WeaponVariantDef.HideTagCount, "WeaponVariantDef.hideTags");
        ValidateCountIfPresent(variant.AnimationNames, WeaponVariantDef.WeaponAnimCount, "WeaponVariantDef.animationNames");
        ValidateCount(variant.AccuracyGraphKnotCount, variant.AccuracyGraphKnots.Count, "WeaponVariantDef.accuracyGraphKnots");
        ValidateCount(variant.OriginalAccuracyGraphKnotCount, variant.OriginalAccuracyGraphKnots.Count, "WeaponVariantDef.originalAccuracyGraphKnots");

        context.Source.WriteInt32(PointerRaw(variant.InternalName, variant.InternalNamePointer.Raw, "WeaponVariantDef.internalName"));
        context.Source.WriteInt32(PointerRaw(variant.Definition, variant.DefinitionPointer.Raw, "WeaponVariantDef.weaponDef"));
        context.Source.WriteInt32(PointerRaw(variant.DisplayName, variant.DisplayNamePointer.Raw, "WeaponVariantDef.displayName"));
        context.Source.WriteInt32(PointerRaw(variant.HideTags, variant.HideTagsPointer.Raw, "WeaponVariantDef.hideTags"));
        context.Source.WriteInt32(PointerRaw(variant.AnimationNames, variant.AnimationNamesPointer.Raw, "WeaponVariantDef.animationNames"));
        context.Source.WriteSingle(variant.AdsZoomFov);
        context.Source.WriteInt32(variant.AdsTransitionInTime);
        context.Source.WriteInt32(variant.AdsTransitionOutTime);
        context.Source.WriteInt32(variant.ClipSize);
        context.Source.WriteInt32(variant.ImpactType);
        context.Source.WriteInt32(variant.FireTime);
        context.Source.WriteInt32(variant.DpadIconRatio);
        context.Source.WriteSingle(variant.PenetrateMultiplier);
        context.Source.WriteSingle(variant.AdsViewKickCenterSpeed);
        context.Source.WriteSingle(variant.HipViewKickCenterSpeed);
        context.Source.WriteInt32(PointerRaw(variant.AlternateWeaponName, variant.AlternateWeaponNamePointer.Raw, "WeaponVariantDef.alternateWeaponName"));
        context.Source.WriteUInt32(variant.AlternateWeaponIndex);
        context.Source.WriteInt32(variant.AlternateRaiseTime);
        WriteExternalNullPointer(context, variant.KillIconPointer.Raw, "WeaponVariantDef.killIcon");
        WriteExternalNullPointer(context, variant.DpadIconPointer.Raw, "WeaponVariantDef.dpadIcon");
        context.Source.WriteInt32(variant.DropAmmoMin);
        context.Source.WriteInt32(variant.FirstRaiseTime);
        context.Source.WriteInt32(variant.DropAmmoMax);
        context.Source.WriteSingle(variant.AdsDofStart);
        context.Source.WriteSingle(variant.AdsDofEnd);
        context.Source.WriteUInt16(variant.AccuracyGraphKnotCount);
        context.Source.WriteUInt16(variant.OriginalAccuracyGraphKnotCount);
        context.Source.WriteInt32(PointerRaw(variant.AccuracyGraphKnots, variant.AccuracyGraphKnotsPointer.Raw, "WeaponVariantDef.accuracyGraphKnots"));
        context.Source.WriteInt32(PointerRaw(variant.OriginalAccuracyGraphKnots, variant.OriginalAccuracyGraphKnotsPointer.Raw, "WeaponVariantDef.originalAccuracyGraphKnots"));
        context.Source.WriteByte(variant.MotionTracker);
        context.Source.WriteByte(variant.Enhanced);
        context.Source.WriteByte(variant.DpadIconShowsAmmo);
        context.Source.WriteByte(variant.Padding73);
    }

    private static void EmitWeaponVariantChildren(XEmitContext context, XBlockAddress root, WeaponVariantDef variant)
    {
        EmitInlineXString(context, root.Add(0x00), Required(variant.InternalName, "WeaponVariantDef.internalName"));
        if (variant.Definition is { } definition)
            EmitWeaponDefPointer(context, root.Add(0x04), definition, variant);
        EmitOptionalXString(context, root.Add(0x08), variant.DisplayName);
        EmitUInt16Array(context, root.Add(0x0C), variant.HideTags);
        EmitXStringPointerArray(context, root.Add(0x10), variant.AnimationNames, "WeaponVariantDef.animationNames");
        EmitOptionalXString(context, root.Add(0x3C), variant.AlternateWeaponName);
        EmitVec2Array(context, root.Add(0x68), variant.AccuracyGraphKnots);
        EmitVec2Array(context, root.Add(0x6C), variant.OriginalAccuracyGraphKnots);
    }

    private static void EmitWeaponDefPointer(
        XEmitContext context,
        XBlockAddress cell,
        WeaponDef definition,
        WeaponVariantDef owner)
    {
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(WeaponDef.SerializedSize);
        EmitWeaponDefRoot(context, definition, owner);
        EmitWeaponDefChildren(context, root, definition, owner);
    }

    private static void EmitWeaponDefRoot(XEmitContext context, WeaponDef def, WeaponVariantDef owner)
    {
        ValidateCountIfPresent(def.GunModelPointers, WeaponDef.GunModelCount, "WeaponDef.gunModels");
        ValidateCountIfPresent(def.RightHandAnimationNames, WeaponDef.WeaponAnimCount, "WeaponDef.rightHandAnimationNames");
        ValidateCountIfPresent(def.LeftHandAnimationNames, WeaponDef.WeaponAnimCount, "WeaponDef.leftHandAnimationNames");
        ValidateCountIfPresent(def.SoundAliasNames, WeaponDef.WeaponSoundAliasCount, "WeaponDef.soundAliases");
        ValidateCountIfPresent(def.BounceSoundNames, WeaponDef.SurfaceCount, "WeaponDef.bounceSound");
        ValidateCountIfPresent(def.WorldGunModelPointers, WeaponDef.GunModelCount, "WeaponDef.worldGunModels");
        ValidateCountIfPresent(def.Projectile.SoundAliasNames, 2, "WeaponDef.projectileSoundAliases");
        ValidateCountIfPresent(def.Projectile.ParallelBounce, WeaponDef.SurfaceCount, "WeaponDef.parallelBounce");
        ValidateCountIfPresent(def.Projectile.PerpendicularBounce, WeaponDef.SurfaceCount, "WeaponDef.perpendicularBounce");
        ValidateCount(owner.AccuracyGraphKnotCount, def.Accuracy.GraphKnots.Count, "WeaponDef.accuracyGraphKnots");
        ValidateCount(owner.OriginalAccuracyGraphKnotCount, def.Accuracy.OriginalGraphKnots.Count, "WeaponDef.originalAccuracyGraphKnots");
        ValidateCountIfPresent(def.LocationDamageMultipliers, WeaponDef.HitLocationCount, "WeaponDef.locationDamageMultipliers");
        ValidateCountIfPresent(def.Turret.BarrelSpinUpSoundNames, WeaponDef.TurretBarrelSpinSoundCount, "WeaponDef.turretBarrelSpinUp");
        ValidateCountIfPresent(def.Turret.BarrelSpinDownSoundNames, WeaponDef.TurretBarrelSpinSoundCount, "WeaponDef.turretBarrelSpinDown");

        context.Source.WriteInt32(PointerRaw(def.InternalName, def.InternalNamePointer.Raw, "WeaponDef.internalName"));
        context.Source.WriteInt32(PointerRaw(def.GunModelPointers, def.GunModelsPointer.Raw, "WeaponDef.gunModels"));
        WriteExternalNullPointer(context, def.HandModelPointer.Raw, "WeaponDef.handModel");
        context.Source.WriteInt32(PointerRaw(def.RightHandAnimationNames, def.RightHandAnimationNamesPointer.Raw, "WeaponDef.rightHandAnimationNames"));
        context.Source.WriteInt32(PointerRaw(def.LeftHandAnimationNames, def.LeftHandAnimationNamesPointer.Raw, "WeaponDef.leftHandAnimationNames"));
        context.Source.WriteInt32(PointerRaw(def.ModeName, def.ModeNamePointer.Raw, "WeaponDef.modeName"));
        context.Source.WriteInt32(PointerRaw(def.NoteTrackMaps.SoundMapKeys, def.NoteTrackMaps.SoundMapKeysPointer.Raw, "WeaponDef.soundMapKeys"));
        context.Source.WriteInt32(PointerRaw(def.NoteTrackMaps.SoundMapValues, def.NoteTrackMaps.SoundMapValuesPointer.Raw, "WeaponDef.soundMapValues"));
        context.Source.WriteInt32(PointerRaw(def.NoteTrackMaps.RumbleMapKeys, def.NoteTrackMaps.RumbleMapKeysPointer.Raw, "WeaponDef.rumbleMapKeys"));
        context.Source.WriteInt32(PointerRaw(def.NoteTrackMaps.RumbleMapValues, def.NoteTrackMaps.RumbleMapValuesPointer.Raw, "WeaponDef.rumbleMapValues"));
        context.Source.WriteInt32(def.Unknown028);
        context.Source.WriteInt32((int)def.WeaponType);
        context.Source.WriteInt32((int)def.WeaponClass);
        WriteFixedInt32Array(context, def.Unknown034To044, 5, "WeaponDef.unknown034To044");
        WriteExternalNullPointers(context, def.FlashEffectPointers, 2, "WeaponDef.flashEffects");
        WriteFixedSoundAliasCells(context, def.SoundAliasNames, WeaponDef.WeaponSoundAliasCount, "WeaponDef.soundAliases");
        context.Source.WriteInt32(PointerRaw(def.BounceSoundNames, def.BounceSoundPointer.Raw, "WeaponDef.bounceSound"));
        WriteExternalNullPointers(context, def.EffectPointers, 4, "WeaponDef.effects");
        WriteExternalNullPointers(context, def.MaterialPointers, 2, "WeaponDef.materials");
        WriteFixedInt32Array(context, def.ReticleFields, 4, "WeaponDef.reticleFields");
        WriteFixedInt32Array(context, def.ViewMovementRotationFields, 30, "WeaponDef.viewMovementRotationFields");
        WriteFixedInt32Array(context, def.PositionalMovementRotationFields, 10, "WeaponDef.positionalMovementRotationFields");
        context.Source.WriteInt32(PointerRaw(def.WorldGunModelPointers, def.WorldGunModelsPointer.Raw, "WeaponDef.worldGunModels"));
        WriteExternalNullPointers(context, def.WorldModelPointers, 4, "WeaponDef.worldModels");
        WriteExternalNullPointer(context, def.Icons.AmmoCounterIconPointer.Raw, "WeaponDef.ammoCounterIcon");
        context.Source.WriteInt32(def.Icons.AmmoCounterIconRatio);
        WriteExternalNullPointer(context, def.Icons.CompassIconPointer.Raw, "WeaponDef.compassIcon");
        context.Source.WriteInt32(def.Icons.CompassIconRatio);
        WriteExternalNullPointer(context, def.Icons.OverlayMaterialPointer.Raw, "WeaponDef.overlayMaterial");
        WriteFixedInt32Array(context, def.OverlayFieldsA, 3, "WeaponDef.overlayFieldsA");
        context.Source.WriteInt32(PointerRaw(def.Overlay.OverlayReticle, def.Overlay.OverlayReticlePointer.Raw, "WeaponDef.overlayReticle"));
        context.Source.WriteInt32(def.Overlay.OverlayReticleCacheIndex);
        context.Source.WriteInt32(PointerRaw(def.Overlay.OverlayInterface, def.Overlay.OverlayInterfacePointer.Raw, "WeaponDef.overlayInterface"));
        context.Source.WriteInt32(def.Overlay.OverlayInterfaceCacheIndex);
        WriteFixedInt32Array(context, def.Overlay.OverlayFieldsB, 2, "WeaponDef.overlayFieldsB");
        context.Source.WriteInt32(PointerRaw(def.Overlay.AlternateModeName, def.Overlay.AlternateModeNamePointer.Raw, "WeaponDef.alternateModeName"));
        context.Source.WriteInt32(def.Overlay.AlternateModeCacheIndex);
        WriteFixedInt32Array(context, def.Overlay.ModeFields, 5, "WeaponDef.modeFields");
        WriteFixedInt32Array(context, def.WeaponTimingFields, 40, "WeaponDef.weaponTimingFields");
        WriteFixedInt32Array(context, def.AimMovementTuningFields, 10, "WeaponDef.aimMovementTuningFields");
        WriteExternalNullPointers(context, def.Overlay.OverlayMaterials, 4, "WeaponDef.overlayMaterials");
        WriteFixedInt32Array(context, def.OverlayDimensionFields, 6, "WeaponDef.overlayDimensionFields");
        WriteFixedInt32Array(context, def.BobSpreadIdleSwayAdsViewErrorFields, 38, "WeaponDef.bobSpreadIdleSwayAdsViewErrorFields");
        WriteExternalNullPointer(context, def.PhysCollmapPointer3C8.Raw, "WeaponDef.physCollmap3C8");
        WriteFixedInt32Array(context, def.PhysicsFieldsA, 2, "WeaponDef.physicsFieldsA");
        WriteFixedInt32Array(context, def.PhysicsFieldsB, 5, "WeaponDef.physicsFieldsB");
        WriteFixedInt32Array(context, def.PhysicsFieldsC, 7, "WeaponDef.physicsFieldsC");
        WriteFixedInt32Array(context, def.PhysicsFieldsD, 7, "WeaponDef.physicsFieldsD");
        WriteExternalNullPointer(context, def.Projectile.ModelPointer.Raw, "WeaponDef.projectileModel");
        context.Source.WriteInt32(def.Projectile.ModelField);
        WriteExternalNullPointers(context, def.Projectile.EffectPointers, 2, "WeaponDef.projectileEffects");
        WriteFixedSoundAliasCells(context, def.Projectile.SoundAliasNames, 2, "WeaponDef.projectileSoundAliases");
        WriteFixedInt32Array(context, def.Projectile.ProjectileFieldsA, 3, "WeaponDef.projectileFieldsA");
        context.Source.WriteInt32(PointerRaw(def.Projectile.ParallelBounce, def.Projectile.ParallelBouncePointer.Raw, "WeaponDef.parallelBounce"));
        context.Source.WriteInt32(PointerRaw(def.Projectile.PerpendicularBounce, def.Projectile.PerpendicularBouncePointer.Raw, "WeaponDef.perpendicularBounce"));
        WriteExternalNullPointers(context, def.Projectile.ImpactEffectPointers, 2, "WeaponDef.impactEffects");
        WriteFixedInt32Array(context, def.Projectile.ImpactFieldsA, 3, "WeaponDef.impactFieldsA");
        context.Source.WriteInt32(def.Projectile.ImpactFieldB);
        WriteFixedInt32Array(context, def.Projectile.ImpactFieldsC, 2, "WeaponDef.impactFieldsC");
        WriteExternalNullPointer(context, def.Projectile.ViewShellEjectEffectPointer.Raw, "WeaponDef.viewShellEjectEffect");
        context.Source.WriteInt32(PointerRaw(def.Projectile.ShellEjectSound, def.Projectile.ShellEjectSoundPointer.Raw, "WeaponDef.shellEjectSound"));
        WriteFixedInt32Array(context, def.Projectile.ShellEjectFields, 3, "WeaponDef.shellEjectFields");
        WriteFixedInt32Array(context, def.Projectile.AdsHipGunKickAiDistanceFields, 35, "WeaponDef.adsHipGunKickAiDistanceFields");
        context.Source.WriteInt32(PointerRaw(def.Accuracy.GraphName0, def.Accuracy.GraphName0Pointer.Raw, "WeaponDef.accuracyGraphName0"));
        context.Source.WriteInt32(PointerRaw(def.Accuracy.GraphName1, def.Accuracy.GraphName1Pointer.Raw, "WeaponDef.accuracyGraphName1"));
        context.Source.WriteInt32(PointerRaw(def.Accuracy.GraphKnots, def.Accuracy.GraphKnotsPointer.Raw, "WeaponDef.accuracyGraphKnots"));
        context.Source.WriteInt32(PointerRaw(def.Accuracy.OriginalGraphKnots, def.Accuracy.OriginalGraphKnotsPointer.Raw, "WeaponDef.originalAccuracyGraphKnots"));
        context.Source.WriteUInt16(def.Accuracy.LocalGraphKnotCount);
        context.Source.WriteUInt16(def.Accuracy.LocalOriginalGraphKnotCount);
        context.Source.WriteInt32(def.Accuracy.AnimationNotifyComparison);
        context.Source.WriteSingle(def.Accuracy.LeftArc);
        context.Source.WriteSingle(def.Accuracy.RightArc);
        context.Source.WriteSingle(def.Accuracy.TopArc);
        context.Source.WriteSingle(def.Accuracy.BottomArc);
        context.Source.WriteSingle(def.Accuracy.Accuracy);
        context.Source.WriteSingle(def.Accuracy.AiSpread);
        context.Source.WriteSingle(def.Accuracy.PlayerSpread);
        WriteFixedSingleArray(context, def.TurnSpeedAndRangeFields, 10, "WeaponDef.turnSpeedAndRangeFields");
        context.Source.WriteInt32(PointerRaw(def.Hints.UseHintString, def.Hints.UseHintStringPointer.Raw, "WeaponDef.useHintString"));
        context.Source.WriteInt32(PointerRaw(def.Hints.DropHintString, def.Hints.DropHintStringPointer.Raw, "WeaponDef.dropHintString"));
        context.Source.WriteInt32(def.Hints.Unknown570);
        context.Source.WriteInt32(def.Hints.DropHintStringState);
        WriteFixedInt32Array(context, def.Hints.HintFieldsB, 5, "WeaponDef.hintFieldsB");
        context.Source.WriteInt32(PointerRaw(def.ScriptName, def.ScriptNamePointer.Raw, "WeaponDef.scriptName"));
        WriteFixedInt32Array(context, def.ScriptFieldsA, 2, "WeaponDef.scriptFieldsA");
        WriteFixedInt32Array(context, def.ScriptFieldsB, 6, "WeaponDef.scriptFieldsB");
        context.Source.WriteInt32(def.HitLocationField);
        context.Source.WriteInt32(PointerRaw(def.LocationDamageMultipliers, def.LocationDamageMultipliersPointer.Raw, "WeaponDef.locationDamageMultipliers"));
        context.Source.WriteInt32(PointerRaw(def.Rumble.FireRumble, def.Rumble.FireRumblePointer.Raw, "WeaponDef.fireRumble"));
        context.Source.WriteInt32(PointerRaw(def.Rumble.MeleeImpactRumble, def.Rumble.MeleeImpactRumblePointer.Raw, "WeaponDef.meleeImpactRumble"));
        WriteExternalNullPointer(context, def.TracerPointer.Raw, "WeaponDef.tracer");
        WriteFixedInt32Array(context, def.TracerFields, 6, "WeaponDef.tracerFields");
        context.Source.WriteInt32(PointerRaw(def.Turret.OverheatSound, def.Turret.OverheatSoundPointer.Raw, "WeaponDef.turretOverheatSound"));
        WriteExternalNullPointer(context, def.Turret.OverheatEffectPointer.Raw, "WeaponDef.turretOverheatEffect");
        context.Source.WriteInt32(PointerRaw(def.Turret.BarrelSpinRumble, def.Turret.BarrelSpinRumblePointer.Raw, "WeaponDef.turretBarrelSpinRumble"));
        WriteFixedInt32Array(context, def.Turret.TurretFields, 3, "WeaponDef.turretFields");
        context.Source.WriteInt32(PointerRaw(def.Turret.BarrelSpinMaxSound, def.Turret.BarrelSpinMaxSoundPointer.Raw, "WeaponDef.turretBarrelSpinMaxSound"));
        context.Source.WriteInt32(PointerRaw(def.Turret.BarrelSpinUpSoundNames, def.Turret.BarrelSpinUpSoundPointers.Raw, "WeaponDef.turretBarrelSpinUp"));
        WriteFixedInt32Array(context, def.Turret.Unknown5FCTo604, 3, "WeaponDef.turretUnknown5FCTo604");
        context.Source.WriteInt32(PointerRaw(def.Turret.BarrelSpinDownSoundNames, def.Turret.BarrelSpinDownSoundPointers.Raw, "WeaponDef.turretBarrelSpinDown"));
        WriteFixedInt32Array(context, def.Turret.Unknown60CTo614, 3, "WeaponDef.turretUnknown60CTo614");
        context.Source.WriteInt32(PointerRaw(def.MissileConeSound.Alias, def.MissileConeSound.AliasPointer.Raw, "WeaponDef.missileConeSoundAlias"));
        context.Source.WriteInt32(PointerRaw(def.MissileConeSound.AliasAtBase, def.MissileConeSound.AliasAtBasePointer.Raw, "WeaponDef.missileConeSoundAliasAtBase"));
        WriteMissileConeFloats(context, def.MissileConeSound);
        WriteTailFlags(context, def.TailFlags);
    }

    private static void EmitWeaponDefChildren(XEmitContext context, XBlockAddress root, WeaponDef def, WeaponVariantDef owner)
    {
        EmitOptionalXString(context, root.Add(0x000), def.InternalName);
        EmitAliasPointerTable(context, root.Add(0x004), def.GunModelPointers, WeaponDef.GunModelCount, "WeaponDef.gunModels");
        EmitXStringPointerArray(context, root.Add(0x00C), def.RightHandAnimationNames, "WeaponDef.rightHandAnimationNames");
        EmitXStringPointerArray(context, root.Add(0x010), def.LeftHandAnimationNames, "WeaponDef.leftHandAnimationNames");
        EmitOptionalXString(context, root.Add(0x014), def.ModeName);
        EmitUInt16Array(context, root.Add(0x018), def.NoteTrackMaps.SoundMapKeys);
        EmitUInt16Array(context, root.Add(0x01C), def.NoteTrackMaps.SoundMapValues);
        EmitUInt16Array(context, root.Add(0x020), def.NoteTrackMaps.RumbleMapKeys);
        EmitUInt16Array(context, root.Add(0x024), def.NoteTrackMaps.RumbleMapValues);
        EmitSoundAliasCells(context, root.Add(0x050), def.SoundAliasNames);
        EmitSoundAliasCellArray(context, root.Add(0x10C), def.BounceSoundNames, "WeaponDef.bounceSound");
        EmitAliasPointerTable(context, root.Add(0x1D8), def.WorldGunModelPointers, WeaponDef.GunModelCount, "WeaponDef.worldGunModels");
        EmitOptionalXString(context, root.Add(0x20C), def.Overlay.OverlayReticle);
        EmitOptionalXString(context, root.Add(0x214), def.Overlay.OverlayInterface);
        EmitOptionalXString(context, root.Add(0x224), def.Overlay.AlternateModeName);
        EmitSoundAliasCells(context, root.Add(0x430), def.Projectile.SoundAliasNames);
        EmitFloatArray(context, root.Add(0x444), def.Projectile.ParallelBounce);
        EmitFloatArray(context, root.Add(0x448), def.Projectile.PerpendicularBounce);
        EmitSoundAliasCell(context, root.Add(0x470), def.Projectile.ShellEjectSound);
        EmitOptionalXString(context, root.Add(0x50C), def.Accuracy.GraphName0);
        EmitVec2Array(context, root.Add(0x514), def.Accuracy.GraphKnots);
        EmitOptionalXString(context, root.Add(0x510), def.Accuracy.GraphName1);
        EmitVec2Array(context, root.Add(0x518), def.Accuracy.OriginalGraphKnots);
        EmitOptionalXString(context, root.Add(0x568), def.Hints.UseHintString);
        EmitOptionalXString(context, root.Add(0x56C), def.Hints.DropHintString);
        EmitOptionalXString(context, root.Add(0x58C), def.ScriptName);
        EmitFloatArray(context, root.Add(0x5B4), def.LocationDamageMultipliers);
        EmitOptionalXString(context, root.Add(0x5B8), def.Rumble.FireRumble);
        EmitOptionalXString(context, root.Add(0x5BC), def.Rumble.MeleeImpactRumble);
        EmitSoundAliasCell(context, root.Add(0x5DC), def.Turret.OverheatSound);
        EmitOptionalXString(context, root.Add(0x5E4), def.Turret.BarrelSpinRumble);
        EmitSoundAliasCell(context, root.Add(0x5F4), def.Turret.BarrelSpinMaxSound);
        EmitSoundAliasCellArray(context, root.Add(0x5F8), def.Turret.BarrelSpinUpSoundNames, "WeaponDef.turretBarrelSpinUp");
        EmitSoundAliasCellArray(context, root.Add(0x608), def.Turret.BarrelSpinDownSoundNames, "WeaponDef.turretBarrelSpinDown");
        EmitSoundAliasCell(context, root.Add(0x618), def.MissileConeSound.Alias);
        EmitSoundAliasCell(context, root.Add(0x61C), def.MissileConeSound.AliasAtBase);
    }

    private static void EmitXStringPointerArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<string?> values, string owner)
    {
        if (values.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, values.Count, sizeof(int));
        foreach (string? value in values)
            context.Source.WriteInt32(value is null ? 0 : -1);

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] is { } value)
                EmitInlineXString(context, table.Add(checked(i * sizeof(int))), value);
        }
    }

    private static void EmitSoundAliasCellArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<string?> values, string owner)
    {
        if (values.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, values.Count, sizeof(int));
        foreach (string? value in values)
            context.Source.WriteInt32(value is null ? 0 : -1);

        EmitSoundAliasCells(context, table, values);
    }

    private static void EmitSoundAliasCells(XEmitContext context, XBlockAddress firstCell, IReadOnlyList<string?> values)
    {
        for (int i = 0; i < values.Count; i++)
            EmitSoundAliasCell(context, firstCell.Add(checked(i * sizeof(int))), values[i]);
    }

    private static void EmitSoundAliasCell(XEmitContext context, XBlockAddress cell, string? value)
    {
        if (value is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress nestedCell = context.Blocks.AllocateCurrent(sizeof(int));
        context.Source.WriteInt32(-1);
        EmitInlineXString(context, nestedCell, value);
    }

    private static void EmitAliasPointerTable<T>(
        XEmitContext context,
        XBlockAddress cell,
        IReadOnlyList<XPointer<T>> values,
        int expectedCount,
        string owner)
    {
        if (values.Count == 0)
            return;

        ValidateCountIfPresent(values, expectedCount, owner);
        PatchAndAllocateTable(context, cell, values.Count, sizeof(int));
        WriteExternalNullPointers(context, values, expectedCount, owner);
    }

    private static void EmitOptionalXString(XEmitContext context, XBlockAddress cell, string? value)
    {
        if (value is not null)
            EmitInlineXString(context, cell, value);
    }

    private static void EmitInlineXString(XEmitContext context, XBlockAddress cell, string value)
    {
        context.Blocks.PatchInlinePointerCell(cell);
        context.Blocks.AllocateCurrent(checked(Encoding.Latin1.GetByteCount(value) + 1));
        context.Source.WriteCString(value);
    }

    private static void EmitUInt16Array(XEmitContext context, XBlockAddress cell, IReadOnlyList<ushort> values)
    {
        if (values.Count == 0)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 2);
        context.Blocks.AllocateCurrent(checked(values.Count * sizeof(ushort)));
        foreach (ushort value in values)
            context.Source.WriteUInt16(value);
    }

    private static void EmitFloatArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<float> values)
    {
        if (values.Count == 0)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        context.Blocks.AllocateCurrent(checked(values.Count * sizeof(float)));
        foreach (float value in values)
            context.Source.WriteSingle(value);
    }

    private static void EmitVec2Array(XEmitContext context, XBlockAddress cell, IReadOnlyList<Vec2> values)
    {
        if (values.Count == 0)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        context.Blocks.AllocateCurrent(checked(values.Count * 2 * sizeof(float)));
        foreach (Vec2 value in values)
        {
            context.Source.WriteSingle(value.a);
            context.Source.WriteSingle(value.b);
        }
    }

    private static XBlockAddress PatchAndAllocateTable(XEmitContext context, XBlockAddress cell, int count, int stride)
    {
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        return context.Blocks.AllocateCurrent(checked(count * stride));
    }

    private static void WriteFixedSoundAliasCells(XEmitContext context, IReadOnlyList<string?> values, int count, string owner)
    {
        if (values.Count == 0)
        {
            for (int i = 0; i < count; i++)
                context.Source.WriteInt32(0);
            return;
        }

        ValidateCountIfPresent(values, count, owner);
        foreach (string? value in values)
            context.Source.WriteInt32(value is null ? 0 : -1);
    }

    private static void WriteExternalNullPointer(XEmitContext context, int raw, string owner)
    {
        RejectExternalPointer(raw, owner);
        context.Source.WriteInt32(0);
    }

    private static void WriteExternalNullPointers<T>(XEmitContext context, IReadOnlyList<XPointer<T>> values, int count, string owner)
    {
        if (values.Count == 0)
        {
            for (int i = 0; i < count; i++)
                context.Source.WriteInt32(0);
            return;
        }

        ValidateCountIfPresent(values, count, owner);
        for (int i = 0; i < values.Count; i++)
            WriteExternalNullPointer(context, values[i].Raw, $"{owner}[{i}]");
    }

    private static void WriteFixedInt32Array(XEmitContext context, IReadOnlyList<int> values, int count, string owner)
    {
        ValidateCountIfPresent(values, count, owner);
        for (int i = 0; i < count; i++)
            context.Source.WriteInt32(i < values.Count ? values[i] : 0);
    }

    private static void WriteFixedSingleArray(XEmitContext context, IReadOnlyList<float> values, int count, string owner)
    {
        ValidateCountIfPresent(values, count, owner);
        for (int i = 0; i < count; i++)
            context.Source.WriteSingle(i < values.Count ? values[i] : 0);
    }

    private static void WriteMissileConeFloats(XEmitContext context, WeaponMissileConeSoundFields value)
    {
        context.Source.WriteSingle(value.RadiusAtTop);
        context.Source.WriteSingle(value.RadiusAtBase);
        context.Source.WriteSingle(value.Height);
        context.Source.WriteSingle(value.OriginOffset);
        context.Source.WriteSingle(value.VolumeScaleAtCore);
        context.Source.WriteSingle(value.VolumeScaleAtEdge);
        context.Source.WriteSingle(value.VolumeScaleCoreSize);
        context.Source.WriteSingle(value.PitchAtTop);
        context.Source.WriteSingle(value.PitchAtBottom);
        context.Source.WriteSingle(value.PitchTopSize);
        context.Source.WriteSingle(value.PitchBottomSize);
        context.Source.WriteSingle(value.CrossfadeTopSize);
        context.Source.WriteSingle(value.CrossfadeBottomSize);
    }

    private static void WriteTailFlags(XEmitContext context, WeaponTailFlags flags)
    {
        context.Source.WriteByte(flags.SharedAmmo);
        context.Source.WriteByte(flags.LockonSupported);
        context.Source.WriteByte(flags.RequireLockonToFire);
        context.Source.WriteByte(flags.BigExplosion);
        context.Source.WriteByte(flags.NoAdsWhenMagEmpty);
        context.Source.WriteByte(flags.AvoidDropCleanup);
        context.Source.WriteByte(flags.InheritsPerks);
        context.Source.WriteByte(flags.CrosshairColorChange);
        context.Source.WriteByte(flags.RifleBullet);
        context.Source.WriteByte(flags.ArmorPiercing);
        context.Source.WriteByte(flags.BoltAction);
        context.Source.WriteByte(flags.AimDownSight);
        context.Source.WriteByte(flags.RechamberWhileAds);
        context.Source.WriteByte(flags.BulletExplosiveDamage);
        context.Source.WriteByte(flags.CookOffHold);
        context.Source.WriteByte(flags.ClipOnly);
        context.Source.WriteByte(flags.NoAmmoPickup);
        context.Source.WriteByte(flags.AdsFireOnly);
        context.Source.WriteByte(flags.CancelAutoHolsterWhenEmpty);
        context.Source.WriteByte(flags.DisableSwitchToWhenEmpty);
        context.Source.WriteByte(flags.SuppressAmmoReserveDisplay);
        context.Source.WriteByte(flags.LaserSightDuringNightvision);
        context.Source.WriteByte(flags.MarkableViewmodel);
        context.Source.WriteByte(flags.NoDualWield);
        context.Source.WriteByte(flags.FlipKillIcon);
        context.Source.WriteByte(flags.NoPartialReload);
        context.Source.WriteByte(flags.SegmentedReload);
        context.Source.WriteByte(flags.BlocksProne);
        context.Source.WriteByte(flags.Silenced);
        context.Source.WriteByte(flags.IsRollingGrenade);
        context.Source.WriteByte(flags.ProjectileExplosionEffectForceNormalUp);
        context.Source.WriteByte(flags.ProjectileImpactExplode);
        context.Source.WriteByte(flags.StickToPlayers);
        context.Source.WriteByte(flags.HasDetonator);
        context.Source.WriteByte(flags.DisableFiring);
        context.Source.WriteByte(flags.TimedDetonation);
        context.Source.WriteByte(flags.Rotate);
        context.Source.WriteByte(flags.HoldButtonToThrow);
        context.Source.WriteByte(flags.FreezeMovementWhenFiring);
        context.Source.WriteByte(flags.ThermalScope);
        context.Source.WriteByte(flags.AltModeSameWeapon);
        context.Source.WriteByte(flags.TurretBarrelSpinEnabled);
        context.Source.WriteByte(flags.MissileConeSoundEnabled);
        context.Source.WriteByte(flags.MissileConeSoundPitchShiftEnabled);
        context.Source.WriteByte(flags.MissileConeSoundCrossfadeEnabled);
        context.Source.WriteByte(flags.OffhandHoldIsCancelable);
        context.Source.WriteUInt16(flags.ReservedPadding);
    }

    private static int PointerRaw(object? value, int raw, string owner)
    {
        if (value is not null)
            return -1;

        RejectExternalPointer(raw, owner);
        return 0;
    }

    private static int PointerRaw<T>(IReadOnlyList<T> values, int raw, string owner)
    {
        if (values.Count > 0)
            return -1;

        RejectExternalPointer(raw, owner);
        return 0;
    }

    private static void RejectExternalPointer(int raw, string owner)
    {
        if (raw != 0)
            throw new NotSupportedException($"{owner} has non-null raw pointer 0x{raw:X8}; WeaponEmitter needs linker-owned external alias/direct references for this field.");
    }

    private static string Required(string? value, string owner)
    {
        return value ?? throw new InvalidDataException($"{owner} is required for inline PS3 weapon emission.");
    }

    private static void ValidateCount(int declared, int actual, string owner)
    {
        if (declared < 0)
            throw new InvalidDataException($"{owner} has negative declared count {declared}.");

        if (declared != actual)
            throw new InvalidDataException($"{owner} declared count {declared} does not match {actual} value(s).");
    }

    private static void ValidateCountIfPresent<T>(IReadOnlyList<T> values, int expected, string owner)
    {
        if (values.Count != 0 && values.Count != expected)
            throw new InvalidDataException($"{owner} expected {expected} value(s), got {values.Count}.");
    }
}
