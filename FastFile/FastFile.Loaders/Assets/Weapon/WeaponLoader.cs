using FastFile.Loaders.Assets.Material;
using FastFile.Models.Assets.Weapon;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using MaterialAsset = FastFile.Models.Assets.Material.MaterialAsset;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Weapon;

public sealed class WeaponLoader
{
    private const int MaterialSize = 0xa8;
    private const int GfxImageSize = 0x50;
    private const int XModelSize = 0x120;
    private const int XModelLodInfoSize = 0x28;
    private const int XModelSurfsSize = 0x24;
    private const int XSurfaceSize = 0x54;
    private const int XSurfaceVertexInfoSize = 0x0c;
    private const int XRigidVertListSize = 0x0c;
    private const int XSurfaceCollisionTreeSize = 0x28;
    private const int XSurfaceCollisionNodeSize = 0x10;
    private const int XSurfaceCollisionLeafSize = 0x02;
    private const int PhysPresetSize = 0x2c;
    private const int PhysCollmapSize = 0x48;
    private const int FxEffectDefSize = 0x20;
    private const int FxElemDefSize = 0xfc;
    private const int FxElemVelStateSampleSize = 0x60;
    private const int FxElemVisStateSampleSize = 0x30;
    private const int FxElemDefVisualSize = 0x04;
    private const int FxElemMarkVisualSize = 0x08;
    private const int FxTrailDefSize = 0x24;
    private const int FxTrailVertexSize = 0x14;
    private const int FxSparkFountainDefSize = 0x34;
    private const int TracerDefSize = 0x70;
    private static readonly MaterialLoader MaterialLoader = new();

    public WeaponAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level Weapon pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            WeaponVariantRoot root = ReadWeaponVariantRoot(cursor, context);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                WeaponVariantDef variant = ReadWeaponVariantChildren(cursor, root, context);
                return new WeaponAsset
                {
                    Offset = root.Offset,
                    Variant = variant
                };
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static WeaponVariantRoot ReadWeaponVariantRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, WeaponVariantDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var root = new WeaponVariantRoot(
            Offset: offset,
            InternalNamePointer: ReadXStringPointer(rootCursor, context),
            DefinitionPointer: ReadPointer<WeaponDef>(rootCursor, context, XPointerResolutionMode.Direct),
            DisplayNamePointer: ReadXStringPointer(rootCursor, context),
            HideTagsPointer: ReadPointer<ushort[]>(rootCursor, context, XPointerResolutionMode.Direct),
            AnimationNamesPointer: ReadPointer<XString[]>(rootCursor, context, XPointerResolutionMode.Direct),
            AdsZoomFov: ReadSingle(rootCursor),
            AdsTransitionInTime: rootCursor.ReadInt32(),
            AdsTransitionOutTime: rootCursor.ReadInt32(),
            ClipSize: rootCursor.ReadInt32(),
            ImpactType: rootCursor.ReadInt32(),
            FireTime: rootCursor.ReadInt32(),
            DpadIconRatio: rootCursor.ReadInt32(),
            PenetrateMultiplier: ReadSingle(rootCursor),
            AdsViewKickCenterSpeed: ReadSingle(rootCursor),
            HipViewKickCenterSpeed: ReadSingle(rootCursor),
            AlternateWeaponNamePointer: ReadXStringPointer(rootCursor, context),
            AlternateWeaponIndex: rootCursor.ReadUInt32(),
            AlternateRaiseTime: rootCursor.ReadInt32(),
            KillIconPointer: ReadPointer<MaterialAsset>(rootCursor, context, XPointerResolutionMode.AliasCell),
            DpadIconPointer: ReadPointer<MaterialAsset>(rootCursor, context, XPointerResolutionMode.AliasCell),
            DropAmmoMin: rootCursor.ReadInt32(),
            FirstRaiseTime: rootCursor.ReadInt32(),
            DropAmmoMax: rootCursor.ReadInt32(),
            AdsDofStart: ReadSingle(rootCursor),
            AdsDofEnd: ReadSingle(rootCursor),
            AccuracyGraphKnotCount: rootCursor.ReadUInt16(),
            OriginalAccuracyGraphKnotCount: rootCursor.ReadUInt16(),
            AccuracyGraphKnotsPointer: ReadPointer<Vec2[]>(rootCursor, context, XPointerResolutionMode.Direct),
            OriginalAccuracyGraphKnotsPointer: ReadPointer<Vec2[]>(rootCursor, context, XPointerResolutionMode.Direct),
            MotionTracker: rootCursor.ReadByte(),
            Enhanced: rootCursor.ReadByte(),
            DpadIconShowsAmmo: rootCursor.ReadByte(),
            Padding73: rootCursor.ReadByte());

        if (rootCursor.Offset != WeaponVariantDef.SerializedSize)
            throw new InvalidDataException($"WeaponVariantDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{WeaponVariantDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  WeaponVariantDef root source=0x{offset:X} name=0x{root.InternalNamePointer.Raw:X8} def=0x{root.DefinitionPointer.Raw:X8} " +
            $"display=0x{root.DisplayNamePointer.Raw:X8} hideTags=0x{root.HideTagsPointer.Raw:X8} anims=0x{root.AnimationNamesPointer.Raw:X8} " +
            $"accuracyCounts={root.AccuracyGraphKnotCount}/{root.OriginalAccuracyGraphKnotCount} blocks={context.Blocks.DescribePositions()}");

        return root;
    }

    private static WeaponVariantDef ReadWeaponVariantChildren(
        FastFileCursor cursor,
        WeaponVariantRoot root,
        FastFileLoadContext context)
    {
        string? internalName = ReadXString(cursor, root.InternalNamePointer, context);
        WeaponDef? definition = ReadWeaponDefPointer(cursor, root.DefinitionPointer.Untyped, root, context);
        string? displayName = ReadXString(cursor, root.DisplayNamePointer, context);
        IReadOnlyList<ushort> hideTags = ReadUInt16Array(cursor, root.HideTagsPointer.Untyped, WeaponVariantDef.HideTagCount, context);
        IReadOnlyList<XString> animationPointers = ReadXStringPointerArray(cursor, root.AnimationNamesPointer.Untyped, WeaponVariantDef.WeaponAnimCount, context);
        IReadOnlyList<string?> animationNames = ReadXStrings(cursor, animationPointers, context);
        string? alternateWeaponName = ReadXString(cursor, root.AlternateWeaponNamePointer, context);

        ReadMaterialPointer(cursor, root.KillIconPointer.Untyped, context);
        ReadMaterialPointer(cursor, root.DpadIconPointer.Untyped, context);

        IReadOnlyList<Vec2> accuracyGraphKnots = ReadVec2Array(cursor, root.AccuracyGraphKnotsPointer.Untyped, root.AccuracyGraphKnotCount, context);
        IReadOnlyList<Vec2> originalAccuracyGraphKnots = ReadVec2Array(cursor, root.OriginalAccuracyGraphKnotsPointer.Untyped, root.OriginalAccuracyGraphKnotCount, context);

        return new WeaponVariantDef
        {
            Offset = root.Offset,
            InternalNamePointer = root.InternalNamePointer,
            InternalName = internalName,
            DefinitionPointer = root.DefinitionPointer,
            Definition = definition,
            DisplayNamePointer = root.DisplayNamePointer,
            DisplayName = displayName,
            HideTagsPointer = root.HideTagsPointer,
            HideTags = hideTags,
            AnimationNamesPointer = root.AnimationNamesPointer,
            AnimationNamePointers = animationPointers,
            AnimationNames = animationNames,
            AdsZoomFov = root.AdsZoomFov,
            AdsTransitionInTime = root.AdsTransitionInTime,
            AdsTransitionOutTime = root.AdsTransitionOutTime,
            ClipSize = root.ClipSize,
            ImpactType = root.ImpactType,
            FireTime = root.FireTime,
            DpadIconRatio = root.DpadIconRatio,
            PenetrateMultiplier = root.PenetrateMultiplier,
            AdsViewKickCenterSpeed = root.AdsViewKickCenterSpeed,
            HipViewKickCenterSpeed = root.HipViewKickCenterSpeed,
            AlternateWeaponNamePointer = root.AlternateWeaponNamePointer,
            AlternateWeaponName = alternateWeaponName,
            AlternateWeaponIndex = root.AlternateWeaponIndex,
            AlternateRaiseTime = root.AlternateRaiseTime,
            KillIconPointer = root.KillIconPointer,
            DpadIconPointer = root.DpadIconPointer,
            DropAmmoMin = root.DropAmmoMin,
            FirstRaiseTime = root.FirstRaiseTime,
            DropAmmoMax = root.DropAmmoMax,
            AdsDofStart = root.AdsDofStart,
            AdsDofEnd = root.AdsDofEnd,
            AccuracyGraphKnotCount = root.AccuracyGraphKnotCount,
            OriginalAccuracyGraphKnotCount = root.OriginalAccuracyGraphKnotCount,
            AccuracyGraphKnotsPointer = root.AccuracyGraphKnotsPointer,
            AccuracyGraphKnots = accuracyGraphKnots,
            OriginalAccuracyGraphKnotsPointer = root.OriginalAccuracyGraphKnotsPointer,
            OriginalAccuracyGraphKnots = originalAccuracyGraphKnots,
            MotionTracker = root.MotionTracker,
            Enhanced = root.Enhanced,
            DpadIconShowsAmmo = root.DpadIconShowsAmmo,
            Padding73 = root.Padding73
        };
    }

    private static WeaponDef? ReadWeaponDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        WeaponVariantRoot owner,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            return null;

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        WeaponDefRoot root = ReadWeaponDefRoot(cursor, context);
        return ReadWeaponDefChildren(cursor, root, owner, context);
    }

    private static WeaponDefRoot ReadWeaponDefRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, WeaponDef.SerializedSize, out XBlockAddress rootAddress);
        var c = new FastFileCursor(rootBytes, rootAddress);

        var root = new WeaponDefRoot
        {
            Offset = offset,
            InternalNamePointer = ReadXStringPointer(c, context),
            GunModelsPointer = ReadPointer<XPointer<XModelAsset>[]>(c, context, XPointerResolutionMode.Direct),
            HandModelPointer = ReadPointer<XModelAsset>(c, context, XPointerResolutionMode.AliasCell),
            RightHandAnimationNamesPointer = ReadPointer<XString[]>(c, context, XPointerResolutionMode.Direct),
            LeftHandAnimationNamesPointer = ReadPointer<XString[]>(c, context, XPointerResolutionMode.Direct),
            ModeNamePointer = ReadXStringPointer(c, context),
            NoteTrackMaps = new WeaponNoteTrackMapPointers(
                ReadPointer<ushort[]>(c, context, XPointerResolutionMode.Direct),
                ReadPointer<ushort[]>(c, context, XPointerResolutionMode.Direct),
                ReadPointer<ushort[]>(c, context, XPointerResolutionMode.Direct),
                ReadPointer<ushort[]>(c, context, XPointerResolutionMode.Direct))
        };

        Seek(c, 0x02c);
        root.WeaponType = (WeaponType)c.ReadInt32();
        root.WeaponClass = (WeaponClass)c.ReadInt32();

        Seek(c, 0x048);
        root.FlashEffectPointers = ReadAliasPointerArray<FxEffectDefAsset>(c, 2, context);

        Seek(c, 0x050);
        root.SoundAliasPointers = ReadSoundAliasCellPointers(c, WeaponDef.WeaponSoundAliasCount, context);
        root.BounceSoundPointer = ReadPointer<XString[]>(c, context, XPointerResolutionMode.Direct);
        root.EffectPointers = ReadAliasPointerArray<FxEffectDefAsset>(c, 4, context);
        root.MaterialPointers = ReadAliasPointerArray<MaterialAsset>(c, 2, context);

        Seek(c, 0x1d8);
        root.WorldGunModelsPointer = ReadPointer<XPointer<XModelAsset>[]>(c, context, XPointerResolutionMode.Direct);
        root.WorldModelPointers = ReadAliasPointerArray<XModelAsset>(c, 4, context);
        root.Icons = new WeaponIconPointers
        {
            AmmoCounterIconPointer = ReadPointer<MaterialAsset>(c, context, XPointerResolutionMode.AliasCell),
            AmmoCounterIconRatio = c.ReadInt32(),
            CompassIconPointer = ReadPointer<MaterialAsset>(c, context, XPointerResolutionMode.AliasCell),
            CompassIconRatio = c.ReadInt32(),
            OverlayMaterialPointer = ReadPointer<MaterialAsset>(c, context, XPointerResolutionMode.AliasCell)
        };

        Seek(c, 0x20c);
        root.OverlayReticlePointer = ReadXStringPointer(c, context);
        root.OverlayReticleCacheIndex = c.ReadInt32();
        root.OverlayInterfacePointer = ReadXStringPointer(c, context);
        root.OverlayInterfaceCacheIndex = c.ReadInt32();
        Seek(c, 0x224);
        root.AlternateModeNamePointer = ReadXStringPointer(c, context);
        root.AlternateModeCacheIndex = c.ReadInt32();

        Seek(c, 0x308);
        root.OverlayMaterials = ReadAliasPointerArray<MaterialAsset>(c, 4, context);

        Seek(c, 0x3c8);
        root.GfxImagePointer3C8 = ReadPointer<GfxImageAsset>(c, context, XPointerResolutionMode.AliasCell);

        Seek(c, 0x420);
        root.ProjectileModelPointer = ReadPointer<XModelAsset>(c, context, XPointerResolutionMode.AliasCell);
        root.ProjectileEffectPointers = ReadAliasPointerArray<FxEffectDefAsset>(c, 2, context);
        root.ProjectileSoundAliasPointers = ReadSoundAliasCellPointers(c, 2, context);
        Seek(c, 0x444);
        root.ParallelBouncePointer = ReadPointer<float[]>(c, context, XPointerResolutionMode.Direct);
        root.PerpendicularBouncePointer = ReadPointer<float[]>(c, context, XPointerResolutionMode.Direct);
        root.ImpactEffectPointers = ReadAliasPointerArray<FxEffectDefAsset>(c, 2, context);
        Seek(c, 0x46c);
        root.ViewShellEjectEffectPointer = ReadPointer<FxEffectDefAsset>(c, context, XPointerResolutionMode.AliasCell);
        root.ShellEjectSoundPointer = ReadXStringPointer(c, context);

        Seek(c, 0x50c);
        root.AccuracyGraphName0Pointer = ReadXStringPointer(c, context);
        root.AccuracyGraphName1Pointer = ReadXStringPointer(c, context);
        root.AccuracyGraphKnotsPointer = ReadPointer<Vec2[]>(c, context, XPointerResolutionMode.Direct);
        root.OriginalAccuracyGraphKnotsPointer = ReadPointer<Vec2[]>(c, context, XPointerResolutionMode.Direct);
        root.LocalGraphKnotCount = c.ReadUInt16();
        root.LocalOriginalGraphKnotCount = c.ReadUInt16();
        root.AnimationNotifyComparison = c.ReadInt32();
        root.LeftArc = ReadSingle(c);
        root.RightArc = ReadSingle(c);
        root.TopArc = ReadSingle(c);
        root.BottomArc = ReadSingle(c);
        root.Accuracy = ReadSingle(c);
        root.AiSpread = ReadSingle(c);
        root.PlayerSpread = ReadSingle(c);

        Seek(c, 0x568);
        root.UseHintStringPointer = ReadXStringPointer(c, context);
        root.DropHintStringPointer = ReadXStringPointer(c, context);
        Seek(c, 0x574);
        root.DropHintStringState = c.ReadInt32();

        Seek(c, 0x58c);
        root.ScriptNamePointer = ReadXStringPointer(c, context);

        Seek(c, 0x5b4);
        root.LocationDamageMultipliersPointer = ReadPointer<float[]>(c, context, XPointerResolutionMode.Direct);
        root.FireRumblePointer = ReadXStringPointer(c, context);
        root.MeleeImpactRumblePointer = ReadXStringPointer(c, context);
        root.TracerPointer = ReadPointer<TracerDefAsset>(c, context, XPointerResolutionMode.AliasCell);

        Seek(c, 0x5dc);
        root.TurretOverheatSoundPointer = ReadXStringPointer(c, context);
        root.TurretOverheatEffectPointer = ReadPointer<FxEffectDefAsset>(c, context, XPointerResolutionMode.AliasCell);
        root.TurretBarrelSpinRumblePointer = ReadXStringPointer(c, context);
        Seek(c, 0x5f4);
        root.TurretBarrelSpinMaxSoundPointer = ReadXStringPointer(c, context);
        root.TurretBarrelSpinUpSoundPointers = ReadPointer<XString[]>(c, context, XPointerResolutionMode.Direct);
        Seek(c, 0x608);
        root.TurretBarrelSpinDownSoundPointers = ReadPointer<XString[]>(c, context, XPointerResolutionMode.Direct);

        Seek(c, 0x618);
        root.MissileConeSoundAliasPointer = ReadXStringPointer(c, context);
        root.MissileConeSoundAliasAtBasePointer = ReadXStringPointer(c, context);
        root.MissileConeFloats = ReadSingleArray(c, 13);

        Seek(c, 0x654);
        root.TailFlags = ReadTailFlags(c);

        if (c.Offset != WeaponDef.SerializedSize)
            throw new InvalidDataException($"WeaponDef consumed 0x{c.Offset:X} bytes instead of 0x{WeaponDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"    WeaponDef root source=0x{offset:X} name=0x{root.InternalNamePointer.Raw:X8} gunModels=0x{root.GunModelsPointer.Raw:X8} " +
            $"xanims=0x{root.RightHandAnimationNamesPointer.Raw:X8}/0x{root.LeftHandAnimationNamesPointer.Raw:X8} " +
            $"sounds={root.SoundAliasPointers.Count} mats=0x{root.MaterialPointers[0].Raw:X8}/0x{root.MaterialPointers[1].Raw:X8} " +
            $"effects=0x{root.EffectPointers[0].Raw:X8}/0x{root.EffectPointers[1].Raw:X8}/0x{root.EffectPointers[2].Raw:X8}/0x{root.EffectPointers[3].Raw:X8} " +
            $"worldGunModels=0x{root.WorldGunModelsPointer.Raw:X8} gfxImage3c8=0x{root.GfxImagePointer3C8.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        return root;
    }

    private static WeaponDef ReadWeaponDefChildren(
        FastFileCursor cursor,
        WeaponDefRoot root,
        WeaponVariantRoot owner,
        FastFileLoadContext context)
    {
        string? internalName = ReadXString(cursor, root.InternalNamePointer, context);
        IReadOnlyList<XPointer<XModelAsset>> gunModelPointers = ReadXModelPointerArray(cursor, root.GunModelsPointer.Untyped, WeaponDef.GunModelCount, context);
        ReadXModelPointer(cursor, root.HandModelPointer.Untyped, context);
        IReadOnlyList<XString> rightHandAnimationNames = ReadXStringPointerArray(cursor, root.RightHandAnimationNamesPointer.Untyped, WeaponDef.WeaponAnimCount, context);
        ReadXStrings(cursor, rightHandAnimationNames, context);
        IReadOnlyList<XString> leftHandAnimationNames = ReadXStringPointerArray(cursor, root.LeftHandAnimationNamesPointer.Untyped, WeaponDef.WeaponAnimCount, context);
        ReadXStrings(cursor, leftHandAnimationNames, context);
        string? modeName = ReadXString(cursor, root.ModeNamePointer, context);

        WeaponNoteTrackMaps noteTrackMaps = new()
        {
            SoundMapKeysPointer = root.NoteTrackMaps.SoundMapKeysPointer,
            SoundMapKeys = ReadUInt16Array(cursor, root.NoteTrackMaps.SoundMapKeysPointer.Untyped, WeaponDef.NoteTrackMapCount, context),
            SoundMapValuesPointer = root.NoteTrackMaps.SoundMapValuesPointer,
            SoundMapValues = ReadUInt16Array(cursor, root.NoteTrackMaps.SoundMapValuesPointer.Untyped, WeaponDef.NoteTrackMapCount, context),
            RumbleMapKeysPointer = root.NoteTrackMaps.RumbleMapKeysPointer,
            RumbleMapKeys = ReadUInt16Array(cursor, root.NoteTrackMaps.RumbleMapKeysPointer.Untyped, WeaponDef.NoteTrackMapCount, context),
            RumbleMapValuesPointer = root.NoteTrackMaps.RumbleMapValuesPointer,
            RumbleMapValues = ReadUInt16Array(cursor, root.NoteTrackMaps.RumbleMapValuesPointer.Untyped, WeaponDef.NoteTrackMapCount, context)
        };

        ReadFxPointers(cursor, root.FlashEffectPointers, context);
        ReadSoundAliasCells(cursor, root.SoundAliasPointers, context);
        ReadSoundAliasCellArray(cursor, root.BounceSoundPointer.Untyped, WeaponDef.SurfaceCount, context);
        ReadFxPointers(cursor, root.EffectPointers, context);
        ReadMaterialPointers(cursor, root.MaterialPointers, context);

        IReadOnlyList<XPointer<XModelAsset>> worldGunModelPointers = ReadXModelPointerArray(cursor, root.WorldGunModelsPointer.Untyped, WeaponDef.GunModelCount, context);
        ReadXModelPointers(cursor, root.WorldModelPointers, context);
        ReadMaterialPointer(cursor, root.Icons.AmmoCounterIconPointer.Untyped, context);
        ReadMaterialPointer(cursor, root.Icons.CompassIconPointer.Untyped, context);
        ReadMaterialPointer(cursor, root.Icons.OverlayMaterialPointer.Untyped, context);

        string? overlayReticle = ReadXString(cursor, root.OverlayReticlePointer, context);
        string? overlayInterface = ReadXString(cursor, root.OverlayInterfacePointer, context);
        string? alternateModeName = ReadXString(cursor, root.AlternateModeNamePointer, context);
        ReadMaterialPointers(cursor, root.OverlayMaterials, context);
        ReadGfxImagePointer(cursor, root.GfxImagePointer3C8.Untyped, context);

        ReadXModelPointer(cursor, root.ProjectileModelPointer.Untyped, context);
        ReadFxPointers(cursor, root.ProjectileEffectPointers, context);
        ReadSoundAliasCells(cursor, root.ProjectileSoundAliasPointers, context);
        IReadOnlyList<float> parallelBounce = ReadFloatArray(cursor, root.ParallelBouncePointer.Untyped, WeaponDef.SurfaceCount, context);
        IReadOnlyList<float> perpendicularBounce = ReadFloatArray(cursor, root.PerpendicularBouncePointer.Untyped, WeaponDef.SurfaceCount, context);
        ReadFxPointers(cursor, root.ImpactEffectPointers, context);
        ReadFxPointer(cursor, root.ViewShellEjectEffectPointer.Untyped, context);
        string? shellEjectSound = ReadSoundAliasCell(cursor, root.ShellEjectSoundPointer, context);

        string? graphName0 = ReadXString(cursor, root.AccuracyGraphName0Pointer, context);
        IReadOnlyList<Vec2> graphKnots = ReadVec2Array(cursor, root.AccuracyGraphKnotsPointer.Untyped, owner.AccuracyGraphKnotCount, context);
        string? graphName1 = ReadXString(cursor, root.AccuracyGraphName1Pointer, context);
        IReadOnlyList<Vec2> originalGraphKnots = ReadVec2Array(cursor, root.OriginalAccuracyGraphKnotsPointer.Untyped, owner.OriginalAccuracyGraphKnotCount, context);

        string? useHintString = ReadXString(cursor, root.UseHintStringPointer, context);
        string? dropHintString = ReadXString(cursor, root.DropHintStringPointer, context);
        string? scriptName = ReadXString(cursor, root.ScriptNamePointer, context);
        IReadOnlyList<float> locationDamageMultipliers = ReadFloatArray(cursor, root.LocationDamageMultipliersPointer.Untyped, WeaponDef.HitLocationCount, context);
        string? fireRumble = ReadXString(cursor, root.FireRumblePointer, context);
        string? meleeImpactRumble = ReadXString(cursor, root.MeleeImpactRumblePointer, context);
        ReadTracerPointer(cursor, root.TracerPointer.Untyped, context);

        string? turretOverheatSound = ReadSoundAliasCell(cursor, root.TurretOverheatSoundPointer, context);
        ReadFxPointer(cursor, root.TurretOverheatEffectPointer.Untyped, context);
        string? turretBarrelSpinRumble = ReadXString(cursor, root.TurretBarrelSpinRumblePointer, context);
        string? turretBarrelSpinMaxSound = ReadSoundAliasCell(cursor, root.TurretBarrelSpinMaxSoundPointer, context);
        IReadOnlyList<XString> barrelSpinUpSounds = ReadSoundAliasCellArray(cursor, root.TurretBarrelSpinUpSoundPointers.Untyped, WeaponDef.TurretBarrelSpinSoundCount, context);
        IReadOnlyList<XString> barrelSpinDownSounds = ReadSoundAliasCellArray(cursor, root.TurretBarrelSpinDownSoundPointers.Untyped, WeaponDef.TurretBarrelSpinSoundCount, context);
        string? missileConeSoundAlias = ReadSoundAliasCell(cursor, root.MissileConeSoundAliasPointer, context);
        string? missileConeSoundAliasAtBase = ReadSoundAliasCell(cursor, root.MissileConeSoundAliasAtBasePointer, context);

        return new WeaponDef
        {
            Offset = root.Offset,
            InternalNamePointer = root.InternalNamePointer,
            InternalName = internalName,
            GunModelsPointer = root.GunModelsPointer,
            GunModelPointers = gunModelPointers,
            HandModelPointer = root.HandModelPointer,
            RightHandAnimationNamesPointer = root.RightHandAnimationNamesPointer,
            RightHandAnimationNamePointers = rightHandAnimationNames,
            LeftHandAnimationNamesPointer = root.LeftHandAnimationNamesPointer,
            LeftHandAnimationNamePointers = leftHandAnimationNames,
            ModeNamePointer = root.ModeNamePointer,
            ModeName = modeName,
            NoteTrackMaps = noteTrackMaps,
            WeaponType = root.WeaponType,
            WeaponClass = root.WeaponClass,
            FlashEffectPointers = root.FlashEffectPointers,
            SoundAliasPointers = root.SoundAliasPointers,
            BounceSoundPointer = root.BounceSoundPointer,
            EffectPointers = root.EffectPointers,
            MaterialPointers = root.MaterialPointers,
            WorldGunModelsPointer = root.WorldGunModelsPointer,
            WorldGunModelPointers = worldGunModelPointers,
            WorldModelPointers = root.WorldModelPointers,
            Icons = root.Icons,
            Overlay = new WeaponOverlayFields
            {
                OverlayReticlePointer = root.OverlayReticlePointer,
                OverlayReticle = overlayReticle,
                OverlayReticleCacheIndex = root.OverlayReticleCacheIndex,
                OverlayInterfacePointer = root.OverlayInterfacePointer,
                OverlayInterface = overlayInterface,
                OverlayInterfaceCacheIndex = root.OverlayInterfaceCacheIndex,
                AlternateModeNamePointer = root.AlternateModeNamePointer,
                AlternateModeName = alternateModeName,
                AlternateModeCacheIndex = root.AlternateModeCacheIndex,
                OverlayMaterials = root.OverlayMaterials
            },
            GfxImagePointer3C8 = root.GfxImagePointer3C8,
            Projectile = new WeaponProjectileFields
            {
                ModelPointer = root.ProjectileModelPointer,
                EffectPointers = root.ProjectileEffectPointers,
                SoundAliasPointers = root.ProjectileSoundAliasPointers,
                ParallelBouncePointer = root.ParallelBouncePointer,
                ParallelBounce = parallelBounce,
                PerpendicularBouncePointer = root.PerpendicularBouncePointer,
                PerpendicularBounce = perpendicularBounce,
                ImpactEffectPointers = root.ImpactEffectPointers,
                ViewShellEjectEffectPointer = root.ViewShellEjectEffectPointer,
                ShellEjectSoundPointer = root.ShellEjectSoundPointer,
                ShellEjectSound = shellEjectSound
            },
            Accuracy = new WeaponAccuracyFields
            {
                GraphName0Pointer = root.AccuracyGraphName0Pointer,
                GraphName0 = graphName0,
                GraphName1Pointer = root.AccuracyGraphName1Pointer,
                GraphName1 = graphName1,
                GraphKnotsPointer = root.AccuracyGraphKnotsPointer,
                GraphKnots = graphKnots,
                OriginalGraphKnotsPointer = root.OriginalAccuracyGraphKnotsPointer,
                OriginalGraphKnots = originalGraphKnots,
                LocalGraphKnotCount = root.LocalGraphKnotCount,
                LocalOriginalGraphKnotCount = root.LocalOriginalGraphKnotCount,
                AnimationNotifyComparison = root.AnimationNotifyComparison,
                LeftArc = root.LeftArc,
                RightArc = root.RightArc,
                TopArc = root.TopArc,
                BottomArc = root.BottomArc,
                Accuracy = root.Accuracy,
                AiSpread = root.AiSpread,
                PlayerSpread = root.PlayerSpread
            },
            Hints = new WeaponHintFields
            {
                UseHintStringPointer = root.UseHintStringPointer,
                UseHintString = useHintString,
                DropHintStringPointer = root.DropHintStringPointer,
                DropHintString = dropHintString,
                DropHintStringState = root.DropHintStringState
            },
            ScriptNamePointer = root.ScriptNamePointer,
            ScriptName = scriptName,
            LocationDamageMultipliersPointer = root.LocationDamageMultipliersPointer,
            LocationDamageMultipliers = locationDamageMultipliers,
            Rumble = new WeaponRumbleFields
            {
                FireRumblePointer = root.FireRumblePointer,
                FireRumble = fireRumble,
                MeleeImpactRumblePointer = root.MeleeImpactRumblePointer,
                MeleeImpactRumble = meleeImpactRumble
            },
            TracerPointer = root.TracerPointer,
            Turret = new WeaponTurretFields
            {
                OverheatSoundPointer = root.TurretOverheatSoundPointer,
                OverheatSound = turretOverheatSound,
                OverheatEffectPointer = root.TurretOverheatEffectPointer,
                BarrelSpinRumblePointer = root.TurretBarrelSpinRumblePointer,
                BarrelSpinRumble = turretBarrelSpinRumble,
                BarrelSpinMaxSoundPointer = root.TurretBarrelSpinMaxSoundPointer,
                BarrelSpinMaxSound = turretBarrelSpinMaxSound,
                BarrelSpinUpSoundPointers = root.TurretBarrelSpinUpSoundPointers,
                BarrelSpinUpSounds = barrelSpinUpSounds,
                BarrelSpinDownSoundPointers = root.TurretBarrelSpinDownSoundPointers,
                BarrelSpinDownSounds = barrelSpinDownSounds
            },
            MissileConeSound = new WeaponMissileConeSoundFields
            {
                AliasPointer = root.MissileConeSoundAliasPointer,
                Alias = missileConeSoundAlias,
                AliasAtBasePointer = root.MissileConeSoundAliasAtBasePointer,
                AliasAtBase = missileConeSoundAliasAtBase,
                RadiusAtTop = root.MissileConeFloats[0],
                RadiusAtBase = root.MissileConeFloats[1],
                Height = root.MissileConeFloats[2],
                OriginOffset = root.MissileConeFloats[3],
                VolumeScaleAtCore = root.MissileConeFloats[4],
                VolumeScaleAtEdge = root.MissileConeFloats[5],
                VolumeScaleCoreSize = root.MissileConeFloats[6],
                PitchAtTop = root.MissileConeFloats[7],
                PitchAtBottom = root.MissileConeFloats[8],
                PitchTopSize = root.MissileConeFloats[9],
                PitchBottomSize = root.MissileConeFloats[10],
                CrossfadeTopSize = root.MissileConeFloats[11],
                CrossfadeBottomSize = root.MissileConeFloats[12]
            },
            TailFlags = root.TailFlags
        };
    }

    private static IReadOnlyList<XPointer<XModelAsset>> ReadXModelPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        IReadOnlyList<XPointer<XModelAsset>> pointers = ReadAliasPointerArrayPayload<XModelAsset>(cursor, pointer, count, context);
        ReadXModelPointers(cursor, pointers, context);
        return pointers;
    }

    private static IReadOnlyList<XPointer<T>> ReadAliasPointerArrayPayload<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative alias pointer array count {count}.");

        int byteCount = checked(count * sizeof(int));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, $"{typeof(T).Name}*[]", context);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var pointers = new XPointer<T>[count];

        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadPointer<T>(pointerCursor, context, XPointerResolutionMode.AliasCell);

        return pointers;
    }

    private static IReadOnlyList<XString> ReadXStringPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative XString pointer array count {count}.");

        int byteCount = checked(count * sizeof(int));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, "XString[]", context);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var pointers = new XString[count];

        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadXStringPointer(pointerCursor, context);

        return pointers;
    }

    private static IReadOnlyList<ushort> ReadUInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative ushort array count {count}.");

        int byteCount = checked(count * sizeof(ushort));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, "ushort[]", context);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 2);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress arrayAddress);
        var arrayCursor = new FastFileCursor(bytes, arrayAddress);
        var values = new ushort[count];

        for (int i = 0; i < values.Length; i++)
            values[i] = arrayCursor.ReadUInt16();

        return values;
    }

    private static IReadOnlyList<float> ReadFloatArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative float array count {count}.");

        int byteCount = checked(count * sizeof(float));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, "float[]", context);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress arrayAddress);
        var arrayCursor = new FastFileCursor(bytes, arrayAddress);
        return ReadSingleArray(arrayCursor, count);
    }

    private static IReadOnlyList<Vec2> ReadVec2Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative Vec2 array count {count}.");

        int byteCount = checked(count * 2 * sizeof(float));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, "Vec2[]", context);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress arrayAddress);
        var arrayCursor = new FastFileCursor(bytes, arrayAddress);
        var values = new Vec2[count];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new Vec2
            {
                a = ReadSingle(arrayCursor),
                b = ReadSingle(arrayCursor)
            };
        }

        return values;
    }

    private static void ReadByteArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, count, alignment: 1, context);
    }

    private static void ReadInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, checked(count * sizeof(short)), alignment: 2, context);
    }

    private static void ReadRawBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Invalid negative byte count {byteCount}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, byteCount, "byte[]", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        context.Blocks.Load(cursor, byteCount);
    }

    private static void ReadNonNullPayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.Type == PointerType.Offset)
        {
            ValidateOffsetPointerRange(pointer, byteCount, "non-null payload", context);
            return;
        }

        PatchNonNullCurrentPointerCell(pointer, alignment, context);
        context.Blocks.Load(cursor, byteCount);
    }

    private static IReadOnlyList<string?> ReadXStrings(
        FastFileCursor cursor,
        IReadOnlyList<XString> pointers,
        FastFileLoadContext context)
    {
        var values = new string?[pointers.Count];
        for (int i = 0; i < pointers.Count; i++)
            values[i] = ReadXString(cursor, pointers[i], context);

        return values;
    }

    private static IReadOnlyList<XString> ReadSoundAliasCellPointers(
        FastFileCursor cursor,
        int count,
        FastFileLoadContext context)
    {
        var pointers = new XString[count];
        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadXStringPointer(cursor, context);

        return pointers;
    }

    private static IReadOnlyList<string?> ReadSoundAliasCells(
        FastFileCursor cursor,
        IReadOnlyList<XString> pointers,
        FastFileLoadContext context)
    {
        var values = new string?[pointers.Count];
        for (int i = 0; i < pointers.Count; i++)
            values[i] = ReadSoundAliasCell(cursor, pointers[i], context);

        return values;
    }

    private static string? ReadSoundAliasCell(
        FastFileCursor cursor,
        XString pointer,
        FastFileLoadContext context)
    {
        XPointerReference cellPointer = pointer.Untyped;
        if (cellPointer.Type == PointerType.Offset && cellPointer.PackedAddress is { } address)
            context.Blocks.ValidateMaterializedRange(address, sizeof(int), "snd_alias_list_name cell", cellPointer.Raw);

        if (!context.PointerReader.HasInlinePayload(cellPointer))
            return null;

        // 0xfedd8 -> 0x2613b0 -> 0x10b318: a non-null sound-alias custom cell
        // points at a nested XString cell, which then points at the C string.
        context.PointerReader.PatchInlinePointerCell(cellPointer, alignment: 4);
        byte[] nestedCellBytes = context.Blocks.Load(cursor, sizeof(int), out XBlockAddress nestedCellAddress);
        var nestedCellCursor = new FastFileCursor(nestedCellBytes, nestedCellAddress);
        XString nestedStringPointer = ReadXStringPointer(nestedCellCursor, context);
        return ReadXString(cursor, nestedStringPointer, context);
    }

    private static IReadOnlyList<XString> ReadSoundAliasCellArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative sound alias count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            if (pointer.PackedAddress is { } address)
                context.Blocks.ValidateMaterializedRange(address, checked(count * sizeof(int)), "snd_alias_list_name[]", pointer.Raw);
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] cellBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress arrayAddress);
        var cellCursor = new FastFileCursor(cellBytes, arrayAddress);
        IReadOnlyList<XString> pointers = ReadSoundAliasCellPointers(cellCursor, count, context);
        for (int i = 0; i < pointers.Count; i++)
        {
            context.Diagnostics.Trace(
                $"      SndAliasCustom[] entry[{i}] table={arrayAddress} raw=0x{pointers[i].Raw:X8} " +
                $"source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
            ReadSoundAliasCell(cursor, pointers[i], context);
        }

        return pointers;
    }

    private static void ReadMaterialPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<MaterialAsset>> pointers,
        FastFileLoadContext context)
    {
        foreach (XPointer<MaterialAsset> pointer in pointers)
            ReadMaterialPointer(cursor, pointer.Untyped, context);
    }

    private static void ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, MaterialSize, "Material"))
            return;

        ValidateDirectOffsetPointer<MaterialAsset>(pointer, context);
        MaterialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private static void ReadXModelPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<XModelAsset>> pointers,
        FastFileLoadContext context)
    {
        foreach (XPointer<XModelAsset> pointer in pointers)
            ReadXModelPointer(cursor, pointer.Untyped, context);
    }

    private static void ReadXModelPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, XModelSize, "XModel"))
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, XModelSize, "XModel", context);
            return;
        }

        ReadInlineXModel(cursor, pointer, context);
    }

    private static void ReadInlineXModel(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, XModelSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            XString namePointer = ReadXStringPointer(rootCursor, context);
            byte numBones = rootCursor.ReadByte();
            byte numRootBones = rootCursor.ReadByte();
            byte numSurfs = rootCursor.ReadByte();
            rootCursor.Skip(1);
            rootCursor.Skip(0x24 - rootCursor.Offset);
            XPointer<ushort[]> boneNamesPointer = ReadPointer<ushort[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<byte[]> parentListPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<short[]> quatsPointer = ReadPointer<short[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<float[]> transPointer = ReadPointer<float[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<byte[]> partClassificationPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<byte[]> baseMatPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            XPointer<XPointer<MaterialAsset>[]> materialHandlesPointer = ReadPointer<XPointer<MaterialAsset>[]>(rootCursor, context, XPointerResolutionMode.Direct);

            rootCursor.Skip(0xe4 - rootCursor.Offset);
            XPointer<byte[]> collSurfsPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            int numCollSurfs = rootCursor.ReadInt32();
            rootCursor.Skip(0xf0 - rootCursor.Offset);
            XPointer<byte[]> boneInfoPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            rootCursor.Skip(0x110 - rootCursor.Offset);
            XPointer<ushort[]> invHighMipRadiusPointer = ReadPointer<ushort[]>(rootCursor, context, XPointerResolutionMode.Direct);
            rootCursor.Skip(0x118 - rootCursor.Offset);
            XPointerReference physPresetPointer = ReadPointer<PhysPresetAsset>(rootCursor, context, XPointerResolutionMode.AliasCell).Untyped;
            XPointerReference physCollmapPointer = ReadPointer<PhysCollmapAsset>(rootCursor, context, XPointerResolutionMode.AliasCell).Untyped;

            if (rootCursor.Offset != XModelSize)
                throw new InvalidDataException($"XModel consumed 0x{rootCursor.Offset:X} bytes instead of 0x{XModelSize:X}.");

            int partCount = Math.Max(0, numBones - numRootBones);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadUInt16Array(cursor, boneNamesPointer.Untyped, numBones, context);
                ReadByteArray(cursor, parentListPointer.Untyped, partCount, context);
                ReadInt16Array(cursor, quatsPointer.Untyped, partCount * 4, context);
                ReadFloatArray(cursor, transPointer.Untyped, partCount * 3, context);
                ReadByteArray(cursor, partClassificationPointer.Untyped, numBones, context);
                ReadRawBytes(cursor, baseMatPointer.Untyped, numBones * 0x20, alignment: 4, context);
                IReadOnlyList<XPointer<MaterialAsset>> materialPointers =
                    ReadAliasPointerArrayPayload<MaterialAsset>(cursor, materialHandlesPointer.Untyped, numSurfs, context);
                ReadMaterialPointers(cursor, materialPointers, context);

                for (int i = 0; i < 4; i++)
                {
                    int lodOffset = 0x40 + (i * XModelLodInfoSize);
                    var lodCursor = new FastFileCursor(rootBytes.AsSpan(lodOffset, XModelLodInfoSize).ToArray(), rootAddress with { Offset = rootAddress.Offset + lodOffset });
                    lodCursor.Skip(0x04);
                    ushort lodNumSurfs = lodCursor.ReadUInt16();
                    lodCursor.Skip(0x08 - lodCursor.Offset);
                    XPointerReference modelSurfsPointer = ReadPointer<XModelSurfsAsset>(lodCursor, context, XPointerResolutionMode.AliasCell).Untyped;
                    ReadXModelSurfsPointer(cursor, modelSurfsPointer, lodNumSurfs, context);
                }

                ReadRawBytes(cursor, collSurfsPointer.Untyped, checked(numCollSurfs * 0x24), alignment: 4, context);
                ReadRawBytes(cursor, boneInfoPointer.Untyped, checked(numBones * 0x1c), alignment: 4, context);
                ReadUInt16Array(cursor, invHighMipRadiusPointer.Untyped, numSurfs, context);
                ReadPhysPresetPointer(cursor, physPresetPointer, context);
                ReadPhysCollmapPointer(cursor, physCollmapPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadXModelSurfsPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        ushort lodNumSurfs,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, XModelSurfsSize, "XModelSurfs"))
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, XModelSurfsSize, "XModelSurfs", context);
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, XModelSurfsSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor, context);
            XPointer<byte[]> surfsPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            ushort numSurfs = rootCursor.ReadUInt16();
            rootCursor.Skip(XModelSurfsSize - rootCursor.Offset);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadXSurfaceArray(cursor, surfsPointer.Untyped, numSurfs == 0 ? lodNumSurfs : numSurfs, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadXSurfaceArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, checked(count * XSurfaceSize), "XSurface[]", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] surfaceBytes = context.Blocks.Load(cursor, checked(count * XSurfaceSize), out XBlockAddress arrayAddress);

        for (int i = 0; i < count; i++)
        {
            int offset = i * XSurfaceSize;
            var surfaceCursor = new FastFileCursor(surfaceBytes.AsSpan(offset, XSurfaceSize).ToArray(), arrayAddress with { Offset = arrayAddress.Offset + offset });
            ReadXSurfaceChildren(cursor, surfaceCursor, context);
        }
    }

    private static void ReadXSurfaceChildren(
        FastFileCursor cursor,
        FastFileCursor surfaceCursor,
        FastFileLoadContext context)
    {
        surfaceCursor.Skip(0x02);
        byte streamFlags = surfaceCursor.ReadByte();
        surfaceCursor.Skip(0x04 - surfaceCursor.Offset);
        ushort vertCount = surfaceCursor.ReadUInt16();
        ushort triCount = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> triIndicesPointer = ReadPointer<ushort[]>(surfaceCursor, context, XPointerResolutionMode.Direct);
        ushort blend0 = surfaceCursor.ReadUInt16();
        ushort blend1 = surfaceCursor.ReadUInt16();
        ushort blend2 = surfaceCursor.ReadUInt16();
        ushort blend3 = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> vertsBlendPointer = ReadPointer<ushort[]>(surfaceCursor, context, XPointerResolutionMode.Direct);
        XPointer<byte[]> verts0Pointer = ReadPointer<byte[]>(surfaceCursor, context, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(0x24 - surfaceCursor.Offset);
        XPointer<byte[]> verts1Pointer = ReadPointer<byte[]>(surfaceCursor, context, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(0x30 - surfaceCursor.Offset);
        int vertListCount = surfaceCursor.ReadInt32();
        XPointer<byte[]> vertListPointer = ReadPointer<byte[]>(surfaceCursor, context, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(XSurfaceSize - surfaceCursor.Offset);

        int blendCount = blend0 + (blend1 * 3) + (blend2 * 5) + (blend3 * 7);

        ReadRawBytes(cursor, vertsBlendPointer.Untyped, checked(blendCount * sizeof(ushort)), alignment: 2, context);
        ReadSurfaceStreamBytes(cursor, verts0Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushStreamOne: (streamFlags & 0x01) == 0, context);
        ReadSurfaceStreamBytes(cursor, verts1Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushStreamOne: (streamFlags & 0x02) == 0, context);
        ReadRigidVertListArray(cursor, vertListPointer.Untyped, vertListCount, context);
        ReadSurfaceStreamBytes(cursor, triIndicesPointer.Untyped, checked(triCount * 3 * sizeof(ushort)), alignment: 16, pushStreamOne: (streamFlags & 0x04) == 0, context);
    }

    private static void ReadRigidVertListArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, checked(count * XRigidVertListSize), "XRigidVertList[]", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] listBytes = context.Blocks.Load(cursor, checked(count * XRigidVertListSize), out XBlockAddress listAddress);
        for (int i = 0; i < count; i++)
        {
            int offset = i * XRigidVertListSize;
            var listCursor = new FastFileCursor(listBytes.AsSpan(offset, XRigidVertListSize).ToArray(), listAddress with { Offset = listAddress.Offset + offset });
            listCursor.Skip(0x08);
            XPointer<byte[]> collisionTreePointer = ReadPointer<byte[]>(listCursor, context, XPointerResolutionMode.Direct);
            ReadXSurfaceCollisionTree(cursor, collisionTreePointer.Untyped, context);
        }
    }

    private static void ReadXSurfaceCollisionTree(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, XSurfaceCollisionTreeSize, "XSurfaceCollisionTree", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] treeBytes = context.Blocks.Load(cursor, XSurfaceCollisionTreeSize, out XBlockAddress treeAddress);
        var treeCursor = new FastFileCursor(treeBytes, treeAddress);
        treeCursor.Skip(0x18);
        int nodeCount = treeCursor.ReadInt32();
        XPointer<byte[]> nodesPointer = ReadPointer<byte[]>(treeCursor, context, XPointerResolutionMode.Direct);
        int leafCount = treeCursor.ReadInt32();
        XPointer<byte[]> leafsPointer = ReadPointer<byte[]>(treeCursor, context, XPointerResolutionMode.Direct);

        ReadRawBytes(cursor, nodesPointer.Untyped, checked(nodeCount * XSurfaceCollisionNodeSize), alignment: 16, context);
        ReadRawBytes(cursor, leafsPointer.Untyped, checked(leafCount * XSurfaceCollisionLeafSize), alignment: 2, context);
    }

    private static void ReadSurfaceStreamBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        bool pushStreamOne,
        FastFileLoadContext context)
    {
        if (!pushStreamOne)
        {
            ReadRawBytes(cursor, pointer, byteCount, alignment, context);
            return;
        }

        context.Blocks.Push(XFileBlockType.PHYSICAL);
        try
        {
            ReadRawBytes(cursor, pointer, byteCount, alignment, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadFxPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<FxEffectDefAsset>> pointers,
        FastFileLoadContext context)
    {
        foreach (XPointer<FxEffectDefAsset> pointer in pointers)
            ReadFxPointer(cursor, pointer.Untyped, context);
    }

    private static void ReadFxPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, FxEffectDefSize, "FxEffectDef"))
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, FxEffectDefSize, "FxEffectDef", context);
            return;
        }

        ReadInlineFxEffectDef(cursor, pointer, context);
    }

    private static void ReadInlineFxEffectDef(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, FxEffectDefSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            XString namePointer = ReadXStringPointer(rootCursor, context);
            rootCursor.Skip(0x10 - rootCursor.Offset);
            int elemDefCountLooping = rootCursor.ReadInt32();
            int elemDefCountOneShot = rootCursor.ReadInt32();
            int elemDefCountEmission = rootCursor.ReadInt32();
            XPointer<byte[]> elemDefsPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
            context.Diagnostics.Trace(
                $"      FxEffectDef root source=0x{cursor.Offset - FxEffectDefSize:X} ptr=0x{pointer.Raw:X8} name=0x{namePointer.Raw:X8} " +
                $"counts={elemDefCountLooping}/{elemDefCountOneShot}/{elemDefCountEmission} elemDefs=0x{elemDefsPointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");
            int elemDefCount = checked(elemDefCountLooping + elemDefCountOneShot + elemDefCountEmission);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadFxElemDefArray(cursor, elemDefsPointer.Untyped, elemDefCount, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadFxElemDefArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, checked(count * FxElemDefSize), "FxElemDef[]", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] elemBytes = context.Blocks.Load(cursor, checked(count * FxElemDefSize), out XBlockAddress elemAddress);

        for (int i = 0; i < count; i++)
        {
            int offset = i * FxElemDefSize;
            var elemCursor = new FastFileCursor(elemBytes.AsSpan(offset, FxElemDefSize).ToArray(), elemAddress with { Offset = elemAddress.Offset + offset });
            ReadFxElemDefChildren(cursor, elemCursor, context);
        }
    }

    private static void ReadFxElemDefChildren(
        FastFileCursor cursor,
        FastFileCursor elemCursor,
        FastFileLoadContext context)
    {
        elemCursor.Skip(0xb0);
        byte elemType = elemCursor.ReadByte();
        byte visualCount = elemCursor.ReadByte();
        byte velIntervalCount = elemCursor.ReadByte();
        byte visStateIntervalCount = elemCursor.ReadByte();
        XPointerReference velSamplesPointer = ReadPointer<byte[]>(elemCursor, context, XPointerResolutionMode.Direct).Untyped;
        XPointerReference visSamplesPointer = ReadPointer<byte[]>(elemCursor, context, XPointerResolutionMode.Direct).Untyped;
        XPointerReference visualsPointer = ReadPointer<byte[]>(elemCursor, context, XPointerResolutionMode.Direct).Untyped;
        elemCursor.Skip(0xd8 - elemCursor.Offset);
        XString effectOnImpactPointer = ReadXStringPointer(elemCursor, context);
        XString effectOnDeathPointer = ReadXStringPointer(elemCursor, context);
        XString effectEmittedPointer = ReadXStringPointer(elemCursor, context);
        elemCursor.Skip(0xf4 - elemCursor.Offset);
        XPointerReference extendedPointer = ReadPointer<byte[]>(elemCursor, context, XPointerResolutionMode.Direct).Untyped;

        ReadNonNullPayload(cursor, velSamplesPointer, (velIntervalCount + 1) * FxElemVelStateSampleSize, alignment: 4, context);
        ReadNonNullPayload(cursor, visSamplesPointer, (visStateIntervalCount + 1) * FxElemVisStateSampleSize, alignment: 4, context);
        ReadFxElemVisuals(cursor, visualsPointer, elemType, visualCount, context);
        ReadXString(cursor, effectOnImpactPointer, context);
        ReadXString(cursor, effectOnDeathPointer, context);
        ReadXString(cursor, effectEmittedPointer, context);
        ReadFxElemExtended(cursor, extendedPointer, elemType, context);
    }

    private static void ReadFxElemVisuals(
        FastFileCursor cursor,
        XPointerReference pointer,
        byte elemType,
        byte visualCount,
        FastFileLoadContext context)
    {
        if (elemType == 11)
        {
            ReadFxElemMarkVisuals(cursor, pointer, visualCount, context);
            return;
        }

        if (visualCount > 1)
        {
            if (!context.PointerReader.HasInlinePayload(pointer))
            {
                ValidateOffsetPointerRange(pointer, checked(visualCount * FxElemDefVisualSize), "FxElemDefVisual[]", context);
                return;
            }

            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] visualBytes = context.Blocks.Load(cursor, checked(visualCount * FxElemDefVisualSize), out XBlockAddress visualAddress);
            var visualCursor = new FastFileCursor(visualBytes, visualAddress);
            for (int i = 0; i < visualCount; i++)
            {
                XPointerReference visualPointer = ReadPointer<byte[]>(visualCursor, context, XPointerResolutionMode.AliasCell).Untyped;
                ReadFxElemVisual(cursor, visualPointer, elemType, context);
            }

            return;
        }

        ReadFxElemVisual(cursor, pointer, elemType, context);
    }

    private static void ReadFxElemMarkVisuals(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, checked(count * FxElemMarkVisualSize), "FxElemMarkVisual[]", context);
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] markBytes = context.Blocks.Load(cursor, checked(count * FxElemMarkVisualSize), out XBlockAddress markAddress);
        var markCursor = new FastFileCursor(markBytes, markAddress);
        for (int i = 0; i < count; i++)
        {
            XPointerReference material0 = ReadPointer<MaterialAsset>(markCursor, context, XPointerResolutionMode.AliasCell).Untyped;
            XPointerReference material1 = ReadPointer<MaterialAsset>(markCursor, context, XPointerResolutionMode.AliasCell).Untyped;
            ReadMaterialPointer(cursor, material0, context);
            ReadMaterialPointer(cursor, material1, context);
        }
    }

    private static void ReadFxElemVisual(
        FastFileCursor cursor,
        XPointerReference pointer,
        byte elemType,
        FastFileLoadContext context)
    {
        switch (elemType)
        {
            case 7:
                ReadXModelPointer(cursor, pointer, context);
                break;
            case 8:
            case 9:
                break;
            case 10:
                ReadXString(cursor, pointer.AsPointer<string>(), context);
                break;
            case 12:
                ReadXString(cursor, pointer.AsPointer<string>(), context);
                break;
            default:
                ReadMaterialPointer(cursor, pointer, context);
                break;
        }
    }

    private static void ReadFxElemExtended(
        FastFileCursor cursor,
        XPointerReference pointer,
        byte elemType,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.Type == PointerType.Offset)
        {
            int byteCount = elemType switch
            {
                3 => FxTrailDefSize,
                6 => FxSparkFountainDefSize,
                _ => 1
            };
            ValidateOffsetPointerRange(pointer, byteCount, "FxElemExtendedDef", context);
            return;
        }

        switch (elemType)
        {
            case 3:
                PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
                byte[] trailBytes = context.Blocks.Load(cursor, FxTrailDefSize, out XBlockAddress trailAddress);
                var trailCursor = new FastFileCursor(trailBytes, trailAddress);
                trailCursor.Skip(0x14);
                int vertCount = trailCursor.ReadInt32();
                XPointerReference vertsPointer = ReadPointer<byte[]>(trailCursor, context, XPointerResolutionMode.Direct).Untyped;
                int indCount = trailCursor.ReadInt32();
                XPointerReference indsPointer = ReadPointer<ushort[]>(trailCursor, context, XPointerResolutionMode.Direct).Untyped;
                ReadRawBytes(cursor, vertsPointer, checked(vertCount * FxTrailVertexSize), alignment: 4, context);
                ReadRawBytes(cursor, indsPointer, checked(indCount * sizeof(ushort)), alignment: 2, context);
                break;

            case 6:
                PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
                context.Blocks.Load(cursor, FxSparkFountainDefSize);
                break;

            default:
                PatchNonNullCurrentPointerCell(pointer, alignment: 1, context);
                context.Blocks.Load(cursor, 1);
                break;
        }
    }

    private static void ReadTracerPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, TracerDefSize, "TracerDef"))
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, TracerDefSize, "TracerDef", context);
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, TracerDefSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor, context);
            XPointerReference materialPointer = ReadPointer<MaterialAsset>(rootCursor, context, XPointerResolutionMode.AliasCell).Untyped;

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadMaterialPointer(cursor, materialPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadPhysPresetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, PhysPresetSize, "PhysPreset"))
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, PhysPresetSize, "PhysPreset", context);
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, PhysPresetSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor, context);
            rootCursor.Skip(0x1c - rootCursor.Offset);
            XString sndAliasPrefixPointer = ReadXStringPointer(rootCursor, context);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadXString(cursor, sndAliasPrefixPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadPhysCollmapPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, PhysCollmapSize, "PhysCollmap"))
            return;

        ValidateOffsetPointerRange(pointer, PhysCollmapSize, "PhysCollmap", context);
        ThrowIfInlineShell(pointer, "PhysCollmap", cursor, context);
    }

    private static void ReadGfxImagePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            ValidateOffsetPointerRange(pointer, GfxImageSize, "GfxImage", context);
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, GfxImageSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            rootCursor.Skip(0x28);
            XPointerReference payloadPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct).Untyped;
            rootCursor.Skip(0x4c - rootCursor.Offset);
            XString namePointer = ReadXStringPointer(rootCursor, context);

            if (rootCursor.Offset != GfxImageSize)
                throw new InvalidDataException($"GfxImage consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GfxImageSize:X}.");

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                if (context.PointerReader.HasInlinePayload(payloadPointer))
                {
                    throw new NotSupportedException(
                        $"Weapon GfxImage payload pointer {payloadPointer.Raw:X8} is inline; payload byte-count path is not implemented.");
                }
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ThrowIfInlineShell(
        XPointerReference pointer,
        string targetName,
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            return;

        throw new NotSupportedException(
            $"Weapon nested {targetName} pointer 0x{pointer.Raw:X8} is inline at source 0x{cursor.Offset:X}; " +
            $"implement the proven {targetName} loader before consuming this payload. blocks={context.Blocks.DescribePositions()}");
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        FastFileLoadContext context,
        XPointerResolutionMode mode)
    {
        // Weapon roots can point at cells materialized later in the proven child walk.
        // Validate these in the child consumer helpers instead of during root byte decode.
        int cellOffset = cursor.Offset;
        return new XPointer<T>(cursor.ReadInt32(), mode, cursor.AddressAt(cellOffset));
    }

    private static XString ReadXStringPointer(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return ReadPointer<string>(cursor, context, XPointerResolutionMode.Direct);
    }

    private static void ValidateDirectOffsetPointer<T>(
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type != PointerType.Offset)
            return;

        context.PointerReader.ValidateOffsetPointer<T>(pointer);
    }

    private static void ValidateOffsetPointerRange(
        XPointerReference pointer,
        int byteCount,
        string targetName,
        FastFileLoadContext context)
    {
        if (pointer.Type != PointerType.Offset)
            return;

        context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, targetName);
    }

    private static bool ResolveAliasCellOffset(
        XPointerReference pointer,
        FastFileLoadContext context,
        int targetByteCount,
        string targetName)
    {
        if (pointer.Type != PointerType.Offset || pointer.ResolutionMode != XPointerResolutionMode.AliasCell)
            return false;

        if (pointer.CellAddress is not { } destinationCell)
            throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} has no destination cell to patch.");

        int aliasedRaw = context.PointerReader.ReadAliasCellRaw(pointer);
        if (aliasedRaw != 0)
        {
            PointerType aliasedType = XPointerCodec.GetType(aliasedRaw);
            if (aliasedType != PointerType.Offset)
                throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} resolved to unresolved sentinel 0x{aliasedRaw:X8} for {targetName}.");

            context.PointerReader.ValidateOffsetPointerRange(
                XPointerReference.FromRaw(aliasedRaw, XPointerResolutionMode.Direct, pointer.PackedAddress),
                targetByteCount,
                targetName);
        }

        context.Blocks.WriteInt32(destinationCell, aliasedRaw);
        return true;
    }

    private static XBlockAddress PatchNonNullCurrentPointerCell(
        XPointerReference pointer,
        int alignment,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            throw new InvalidDataException("Cannot patch a null pointer cell to the current stream position.");

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Pointer 0x{pointer.Raw:X8} has no destination cell address to patch.");

        if (alignment > 1)
            context.Blocks.AlignCurrent(alignment);

        XBlockAddress targetAddress = context.Blocks.CurrentAddress;
        context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(targetAddress));
        return targetAddress;
    }

    private static string? ReadXString(
        FastFileCursor cursor,
        XString pointer,
        FastFileLoadContext context)
    {
        return context.PointerReader.LoadXString(cursor, pointer);
    }

    private static IReadOnlyList<XPointer<T>> ReadAliasPointerArray<T>(
        FastFileCursor cursor,
        int count,
        FastFileLoadContext context)
    {
        var values = new XPointer<T>[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadPointer<T>(cursor, context, XPointerResolutionMode.AliasCell);

        return values;
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private static float[] ReadSingleArray(FastFileCursor cursor, int count)
    {
        var values = new float[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadSingle(cursor);

        return values;
    }

    private static WeaponTailFlags ReadTailFlags(FastFileCursor cursor)
    {
        return new WeaponTailFlags
        {
            SharedAmmo = cursor.ReadByte(),
            LockonSupported = cursor.ReadByte(),
            RequireLockonToFire = cursor.ReadByte(),
            BigExplosion = cursor.ReadByte(),
            NoAdsWhenMagEmpty = cursor.ReadByte(),
            AvoidDropCleanup = cursor.ReadByte(),
            InheritsPerks = cursor.ReadByte(),
            CrosshairColorChange = cursor.ReadByte(),
            RifleBullet = cursor.ReadByte(),
            ArmorPiercing = cursor.ReadByte(),
            BoltAction = cursor.ReadByte(),
            AimDownSight = cursor.ReadByte(),
            RechamberWhileAds = cursor.ReadByte(),
            BulletExplosiveDamage = cursor.ReadByte(),
            CookOffHold = cursor.ReadByte(),
            ClipOnly = cursor.ReadByte(),
            NoAmmoPickup = cursor.ReadByte(),
            AdsFireOnly = cursor.ReadByte(),
            CancelAutoHolsterWhenEmpty = cursor.ReadByte(),
            DisableSwitchToWhenEmpty = cursor.ReadByte(),
            SuppressAmmoReserveDisplay = cursor.ReadByte(),
            LaserSightDuringNightvision = cursor.ReadByte(),
            MarkableViewmodel = cursor.ReadByte(),
            NoDualWield = cursor.ReadByte(),
            FlipKillIcon = cursor.ReadByte(),
            NoPartialReload = cursor.ReadByte(),
            SegmentedReload = cursor.ReadByte(),
            BlocksProne = cursor.ReadByte(),
            Silenced = cursor.ReadByte(),
            IsRollingGrenade = cursor.ReadByte(),
            ProjectileExplosionEffectForceNormalUp = cursor.ReadByte(),
            ProjectileImpactExplode = cursor.ReadByte(),
            StickToPlayers = cursor.ReadByte(),
            HasDetonator = cursor.ReadByte(),
            DisableFiring = cursor.ReadByte(),
            TimedDetonation = cursor.ReadByte(),
            Rotate = cursor.ReadByte(),
            HoldButtonToThrow = cursor.ReadByte(),
            FreezeMovementWhenFiring = cursor.ReadByte(),
            ThermalScope = cursor.ReadByte(),
            AltModeSameWeapon = cursor.ReadByte(),
            TurretBarrelSpinEnabled = cursor.ReadByte(),
            MissileConeSoundEnabled = cursor.ReadByte(),
            MissileConeSoundPitchShiftEnabled = cursor.ReadByte(),
            MissileConeSoundCrossfadeEnabled = cursor.ReadByte(),
            OffhandHoldIsCancelable = cursor.ReadByte(),
            ReservedPadding = cursor.ReadUInt16()
        };
    }

    private static void Seek(FastFileCursor cursor, int offset)
    {
        if (offset < cursor.Offset)
            throw new InvalidOperationException($"Cannot seek backwards from 0x{cursor.Offset:X} to 0x{offset:X}.");

        cursor.Skip(offset - cursor.Offset);
    }

    private sealed record WeaponVariantRoot(
        int Offset,
        XString InternalNamePointer,
        XPointer<WeaponDef> DefinitionPointer,
        XString DisplayNamePointer,
        XPointer<ushort[]> HideTagsPointer,
        XPointer<XString[]> AnimationNamesPointer,
        float AdsZoomFov,
        int AdsTransitionInTime,
        int AdsTransitionOutTime,
        int ClipSize,
        int ImpactType,
        int FireTime,
        int DpadIconRatio,
        float PenetrateMultiplier,
        float AdsViewKickCenterSpeed,
        float HipViewKickCenterSpeed,
        XString AlternateWeaponNamePointer,
        uint AlternateWeaponIndex,
        int AlternateRaiseTime,
        XPointer<MaterialAsset> KillIconPointer,
        XPointer<MaterialAsset> DpadIconPointer,
        int DropAmmoMin,
        int FirstRaiseTime,
        int DropAmmoMax,
        float AdsDofStart,
        float AdsDofEnd,
        ushort AccuracyGraphKnotCount,
        ushort OriginalAccuracyGraphKnotCount,
        XPointer<Vec2[]> AccuracyGraphKnotsPointer,
        XPointer<Vec2[]> OriginalAccuracyGraphKnotsPointer,
        byte MotionTracker,
        byte Enhanced,
        byte DpadIconShowsAmmo,
        byte Padding73);

    private readonly record struct WeaponNoteTrackMapPointers(
        XPointer<ushort[]> SoundMapKeysPointer,
        XPointer<ushort[]> SoundMapValuesPointer,
        XPointer<ushort[]> RumbleMapKeysPointer,
        XPointer<ushort[]> RumbleMapValuesPointer);

    private sealed class WeaponDefRoot
    {
        public int Offset { get; init; }
        public XString InternalNamePointer { get; set; }
        public XPointer<XPointer<XModelAsset>[]> GunModelsPointer { get; set; }
        public XPointer<XModelAsset> HandModelPointer { get; set; }
        public XPointer<XString[]> RightHandAnimationNamesPointer { get; set; }
        public XPointer<XString[]> LeftHandAnimationNamesPointer { get; set; }
        public XString ModeNamePointer { get; set; }
        public WeaponNoteTrackMapPointers NoteTrackMaps { get; set; }
        public WeaponType WeaponType { get; set; }
        public WeaponClass WeaponClass { get; set; }
        public IReadOnlyList<XPointer<FxEffectDefAsset>> FlashEffectPointers { get; set; } = [];
        public XPointer<XString[]> SoundAliasPointersPointer { get; set; }
        public IReadOnlyList<XString> SoundAliasPointers { get; set; } = [];
        public XPointer<XString[]> BounceSoundPointer { get; set; }
        public IReadOnlyList<XPointer<FxEffectDefAsset>> EffectPointers { get; set; } = [];
        public IReadOnlyList<XPointer<MaterialAsset>> MaterialPointers { get; set; } = [];
        public XPointer<XPointer<XModelAsset>[]> WorldGunModelsPointer { get; set; }
        public IReadOnlyList<XPointer<XModelAsset>> WorldModelPointers { get; set; } = [];
        public WeaponIconPointers Icons { get; set; } = new();
        public XString OverlayReticlePointer { get; set; }
        public int OverlayReticleCacheIndex { get; set; }
        public XString OverlayInterfacePointer { get; set; }
        public int OverlayInterfaceCacheIndex { get; set; }
        public XString AlternateModeNamePointer { get; set; }
        public int AlternateModeCacheIndex { get; set; }
        public IReadOnlyList<XPointer<MaterialAsset>> OverlayMaterials { get; set; } = [];
        public XPointer<GfxImageAsset> GfxImagePointer3C8 { get; set; }
        public XPointer<XModelAsset> ProjectileModelPointer { get; set; }
        public IReadOnlyList<XPointer<FxEffectDefAsset>> ProjectileEffectPointers { get; set; } = [];
        public XPointer<XString[]> ProjectileSoundAliasPointersPointer { get; set; }
        public IReadOnlyList<XString> ProjectileSoundAliasPointers { get; set; } = [];
        public XPointer<float[]> ParallelBouncePointer { get; set; }
        public XPointer<float[]> PerpendicularBouncePointer { get; set; }
        public IReadOnlyList<XPointer<FxEffectDefAsset>> ImpactEffectPointers { get; set; } = [];
        public XPointer<FxEffectDefAsset> ViewShellEjectEffectPointer { get; set; }
        public XString ShellEjectSoundPointer { get; set; }
        public XString AccuracyGraphName0Pointer { get; set; }
        public XString AccuracyGraphName1Pointer { get; set; }
        public XPointer<Vec2[]> AccuracyGraphKnotsPointer { get; set; }
        public XPointer<Vec2[]> OriginalAccuracyGraphKnotsPointer { get; set; }
        public ushort LocalGraphKnotCount { get; set; }
        public ushort LocalOriginalGraphKnotCount { get; set; }
        public int AnimationNotifyComparison { get; set; }
        public float LeftArc { get; set; }
        public float RightArc { get; set; }
        public float TopArc { get; set; }
        public float BottomArc { get; set; }
        public float Accuracy { get; set; }
        public float AiSpread { get; set; }
        public float PlayerSpread { get; set; }
        public XString UseHintStringPointer { get; set; }
        public XString DropHintStringPointer { get; set; }
        public int DropHintStringState { get; set; }
        public XString ScriptNamePointer { get; set; }
        public XPointer<float[]> LocationDamageMultipliersPointer { get; set; }
        public XString FireRumblePointer { get; set; }
        public XString MeleeImpactRumblePointer { get; set; }
        public XPointer<TracerDefAsset> TracerPointer { get; set; }
        public XString TurretOverheatSoundPointer { get; set; }
        public XPointer<FxEffectDefAsset> TurretOverheatEffectPointer { get; set; }
        public XString TurretBarrelSpinRumblePointer { get; set; }
        public XString TurretBarrelSpinMaxSoundPointer { get; set; }
        public XPointer<XString[]> TurretBarrelSpinUpSoundPointers { get; set; }
        public XPointer<XString[]> TurretBarrelSpinDownSoundPointers { get; set; }
        public XString MissileConeSoundAliasPointer { get; set; }
        public XString MissileConeSoundAliasAtBasePointer { get; set; }
        public float[] MissileConeFloats { get; set; } = [];
        public WeaponTailFlags TailFlags { get; set; } = new();
    }
}
