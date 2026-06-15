using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

public sealed class WeaponAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute WeaponDefAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4
    };

    private static readonly XPointerFieldAttribute ScriptStringArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(WeaponVariantDef.HideTagCount)
    };

    private static readonly XPointerFieldAttribute WeaponAnimPointerArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.WeaponAnimCount)
    };

    private static readonly XPointerFieldAttribute VariantAccuracyGraphArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponVariantDef.accuracyGraphKnotCount)
    };

    private static readonly XPointerFieldAttribute VariantOriginalAccuracyGraphArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponVariantDef.originalAccuracyGraphKnotCount)
    };

    private static readonly XPointerFieldAttribute WeaponDefAccuracyGraphArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.accuracyGraphKnotCount)
    };

    private static readonly XPointerFieldAttribute WeaponDefOriginalAccuracyGraphArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.originalAccuracyGraphKnotCount)
    };

    private static readonly XPointerFieldAttribute XModelPointerArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Alias,
        ElementTarget = XPointerTarget.None,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.GunModelCount)
    };

    private static readonly XPointerFieldAttribute SoundAliasPointerArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.None,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.SurfaceCount)
    };

    private static readonly XPointerFieldAttribute TurretSoundAliasPointerArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.None,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.TurretBarrelSpinSoundCount)
    };

    private static readonly XPointerFieldAttribute NoteTrackMapAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(WeaponDef.NoteTrackMapCount)
    };

    private static readonly XPointerFieldAttribute SurfaceFloatArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.SurfaceCount)
    };

    private static readonly XPointerFieldAttribute HitLocationFloatArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(WeaponDef.HitLocationCount)
    };

    private static readonly XPointerFieldAttribute TempAliasWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case WeaponVariantDef variant:
                Load_WeaponVariantDef(variant, context);
                return true;

            case WeaponDef def:
                Load_WeaponDef(def, context);
                return true;

            default:
                return false;
        }
    }

    // PS3 0x1152f8: Load_WeaponVariantDef body after the asset wrapper pushes stream block 4.
    private static void Load_WeaponVariantDef(
        WeaponVariantDef variant,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            TraceWeaponPhase("WeaponVariantDef begin", variant, context);
            context.ResolvePointerProperty(variant, nameof(WeaponVariantDef.InternalNamePtr));
            context.ResolvePointerValue(variant.WeaponDefPtr, WeaponDefAttribute, variant);
            context.ResolvePointerProperty(variant, nameof(WeaponVariantDef.DisplayNamePtr));
            context.ResolvePointerValue(variant.HideTags, ScriptStringArrayAttribute, variant);
            context.ResolvePointerValue(variant.XAnims, WeaponAnimPointerArrayAttribute, variant);
            context.ResolvePointerProperty(variant, nameof(WeaponVariantDef.szAltWeaponName));

            Load_TempAliasPtr(variant.killIcon, context, variant);
            Load_TempAliasPtr(variant.dpadIcon, context, variant);

            context.ResolvePointerValue(variant.accuracyGraphKnots, VariantAccuracyGraphArrayAttribute, variant);
            context.ResolvePointerValue(variant.originalAccuracyGraphKnots, VariantOriginalAccuracyGraphArrayAttribute, variant);
            TraceWeaponPhase("WeaponVariantDef end", variant, context);
        });
    }

    // PS3 0x114678: Load_WeaponDef, preserving the observed pointer-resolution order.
    private static void Load_WeaponDef(
        WeaponDef def,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            TraceWeaponPhase("WeaponDef begin", def, context);
            context.ResolvePointerProperty(def, nameof(WeaponDef.InternalNamePtr));
            Load_XModelPtrArray(def.gunXModel, context, def);
            Load_TempAliasPtr(def.handXModel, context, def);
            context.ResolvePointerValue(def.szXAnimsR, WeaponAnimPointerArrayAttribute, def);
            context.ResolvePointerValue(def.szXAnimsL, WeaponAnimPointerArrayAttribute, def);
            context.ResolvePointerProperty(def, nameof(WeaponDef.ModeNamePtr));
            Load_NoteTrackMaps(def, context);
            TraceWeaponPhase("WeaponDef after note maps", def, context);

            Load_TempAliasPtrs(def.FlashEffects, context, def);
            TraceWeaponPhase("WeaponDef before sound aliases", def, context);
            Load_SndAliasCustomNames(def.SoundAliases, context);
            TraceWeaponPhase("WeaponDef after sound aliases", def, context);
            Load_SndAliasCustomNamePointerArray(def.BounceSound, SoundAliasPointerArrayAttribute, context, def);
            TraceWeaponPhase("WeaponDef after bounce sound", def, context);
            Load_TempAliasPtrs(def.EffectPointersA, context, def);
            TraceWeaponPhase("WeaponDef after effect pointers A", def, context);
            TraceWeaponPhase("WeaponDef before material pointers A", def, context);
            Load_TempAliasPtrs(def.MaterialPointersA, context, def);
            TraceWeaponPhase("WeaponDef after material pointers A", def, context);

            Load_XModelPtrArray(def.WorldGunXModel, context, def);
            Load_TempAliasPtrs(def.WorldModelPointers, context, def);
            Load_TempAliasPtr(def.AmmoCounterIcon, context, def);
            Load_TempAliasPtr(def.CompassIcon, context, def);
            Load_TempAliasPtr(def.OverlayMaterial, context, def);
            context.ResolvePointerProperty(def, nameof(WeaponDef.OverlayReticle));
            context.ResolvePointerProperty(def, nameof(WeaponDef.OverlayInterface));
            context.ResolvePointerProperty(def, nameof(WeaponDef.ModeNameAlt));

            Load_TempAliasPtrs(def.OverlayMaterials, context, def);
            Load_TempAliasPtr(def.PhysCollmap, context, def);
            Load_TempAliasPtr(def.ProjectileModel, context, def);
            Load_TempAliasPtrs(def.ProjectileEffects, context, def);
            Load_SndAliasCustomNames(def.ProjectileSoundAliases, context);

            context.ResolvePointerValue(def.ParallelBounce, SurfaceFloatArrayAttribute, def);
            context.ResolvePointerValue(def.PerpendicularBounce, SurfaceFloatArrayAttribute, def);
            Load_TempAliasPtrs(def.ImpactEffects, context, def);
            Load_TempAliasPtr(def.ViewShellEjectEffect, context, def);
            Load_SndAliasCustomName(def.ShellEjectSound, context);

            context.ResolvePointerProperty(def, nameof(WeaponDef.AccuracyGraphName0));
            context.ResolvePointerValue(def.accuracyGraphKnots, WeaponDefAccuracyGraphArrayAttribute, def);
            context.ResolvePointerProperty(def, nameof(WeaponDef.AccuracyGraphName1));
            context.ResolvePointerValue(def.originalAccuracyGraphKnots, WeaponDefOriginalAccuracyGraphArrayAttribute, def);

            context.ResolvePointerProperty(def, nameof(WeaponDef.UseHintString));
            context.ResolvePointerProperty(def, nameof(WeaponDef.DropHintString));
            context.ResolvePointerProperty(def, nameof(WeaponDef.ScriptName));
            context.ResolvePointerValue(def.LocationDamageMultipliers, HitLocationFloatArrayAttribute, def);
            context.ResolvePointerProperty(def, nameof(WeaponDef.FireRumble));
            context.ResolvePointerProperty(def, nameof(WeaponDef.MeleeImpactRumble));
            Load_TempAliasPtr(def.Tracer, context, def);
            Load_SndAliasCustomName(def.TurretOverheatSound, context);
            Load_TempAliasPtr(def.TurretOverheatEffect, context, def);
            context.ResolvePointerProperty(def, nameof(WeaponDef.TurretBarrelSpinRumble));
            Load_SndAliasCustomName(def.TurretBarrelSpinMaxSnd, context);
            Load_SndAliasCustomNamePointerArray(def.TurretBarrelSpinUpSnd, TurretSoundAliasPointerArrayAttribute, context, def);
            Load_SndAliasCustomNamePointerArray(def.TurretBarrelSpinDownSnd, TurretSoundAliasPointerArrayAttribute, context, def);
            Load_SndAliasCustomName(def.MissileConeSoundAlias, context);
            Load_SndAliasCustomName(def.MissileConeSoundAliasAtBase, context);
            TraceWeaponPhase("WeaponDef end", def, context);
        });
    }

    private static void Load_NoteTrackMaps(
        WeaponDef def,
        IXAssetReaderContext context)
    {
        foreach (XPointer<ushort[]> pointer in def.NoteTrackMaps)
            context.ResolvePointerValue(pointer, NoteTrackMapAttribute, def);
    }

    private static void Load_XModelPtrArray(
        XPointer<XPointer<XModel>[]> pointer,
        IXAssetReaderContext context,
        WeaponDef owner)
    {
        context.ResolvePointerValue(pointer, XModelPointerArrayAttribute, owner);

        if (pointer.Value is null)
            return;

        foreach (XPointer<XModel>? model in pointer.Value)
        {
            if (model is { IsResolved: false })
                Load_TempAliasPtr(model, context, owner);
        }
    }

    private static void Load_SndAliasCustomNames(
        IEnumerable<XPointer<string>> pointers,
        IXAssetReaderContext context)
    {
        foreach (XPointer<string>? pointer in pointers)
        {
            if (pointer is not null)
                Load_SndAliasCustomName(pointer, context);
        }
    }

    private static void Load_SndAliasCustomName(
        XPointer<string> pointer,
        IXAssetReaderContext context)
    {
        context.ResolveSndAliasCustomName(pointer);
    }

    private static void Load_SndAliasCustomNamePointerArray(
        XPointer<XPointer<string>[]> pointer,
        XPointerFieldAttribute attribute,
        IXAssetReaderContext context,
        object owner)
    {
        var shouldResolveElements = pointer.Kind is PointerKind.Inline or PointerKind.Insert;
        context.ResolvePointerValue(pointer, attribute, owner);

        if (!shouldResolveElements || pointer.Value is null)
            return;

        foreach (XPointer<string>? element in pointer.Value)
        {
            if (element is not null)
                Load_SndAliasCustomName(element, context);
        }
    }

    private static void Load_TempAliasPtrs<T>(
        IEnumerable<XPointer<T>> pointers,
        IXAssetReaderContext context,
        object owner)
        where T : class
    {
        foreach (XPointer<T>? pointer in pointers)
        {
            if (pointer is not null)
                Load_TempAliasPtr(pointer, context, owner);
        }
    }

    private static void Load_TempAliasPtr<T>(
        XPointer<T> pointer,
        IXAssetReaderContext context,
        object owner)
        where T : class
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_WEAPON") == "1")
        {
            Console.Error.WriteLine(
                $"Weapon temp alias {owner.GetType().Name}->{typeof(T).Name}: " +
                $"src=0x{context.SourcePosition:X} active={context.ActiveStreamBlock} " +
                $"temp=0x{context.GetStreamPosition(XFILE_BLOCK.TEMP):X} " +
                $"large=0x{context.GetStreamPosition(XFILE_BLOCK.LARGE):X} " +
                $"raw=0x{pointer.Raw:X8} kind={pointer.Kind} address={FormatAddress(pointer.Address)}");
        }

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(pointer, TempAliasWrapperAttribute, owner);
        });
    }

    private static void TraceWeaponPhase(
        string phase,
        object owner,
        IXAssetReaderContext context)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_WEAPON") != "1")
            return;

        string detail = owner switch
        {
            WeaponVariantDef variant =>
                $"name={DescribePointerObject(variant.InternalNamePtr)} " +
                $"weaponDef={DescribePointerObject(variant.WeaponDefPtr)} " +
                $"hideTags={DescribePointerObject(variant.HideTags)} " +
                $"xAnims={DescribePointerObject(variant.XAnims)} " +
                $"accCounts={variant.accuracyGraphKnotCount}/{variant.originalAccuracyGraphKnotCount}",

            WeaponDef def =>
                $"name={DescribePointerObject(def.InternalNamePtr)} " +
                $"gunXModel={DescribePointerObject(def.gunXModel)} " +
                $"soundFirst={DescribePointerObject(def.SoundAliases)} " +
                $"bounce={DescribePointerObject(def.BounceSound)} " +
                $"fxA={DescribePointerObject(def.EffectPointersA)} " +
                $"matA={DescribePointerObject(def.MaterialPointersA)}",

            _ => owner.GetType().Name
        };

        Console.Error.WriteLine(
            $"Weapon phase {phase}: src=0x{context.SourcePosition:X} active={context.ActiveStreamBlock} " +
            $"temp=0x{context.GetStreamPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{context.GetStreamPosition(XFILE_BLOCK.LARGE):X} " +
            $"runtime=0x{context.GetStreamPosition(XFILE_BLOCK.RUNTIME):X} {detail}");
    }

    private static string DescribePointerObject(object? value)
    {
        switch (value)
        {
            case null:
                return "<null>";

            case XPointer<XPointer<string>[]> stringPointerArray:
                return DescribePointerArrayPointer(stringPointerArray);

            case XPointer<XPointer<XModel>[]> modelPointerArray:
                return DescribePointerArrayPointer(modelPointerArray);

            case Pointer pointer:
                return DescribePointer(pointer);

            case Array array:
                return DescribePointerArray(array);

            default:
                return value.GetType().Name;
        }
    }

    private static string DescribePointerArrayPointer<T>(XPointer<XPointer<T>[]> pointer)
    {
        string elements = pointer.Value is null
            ? string.Empty
            : $" elems={DescribePointerArray(pointer.Value)}";

        return $"{DescribePointer(pointer)}{elements}";
    }

    private static string DescribePointerArray(Array array)
    {
        var pointers = array
            .Cast<object?>()
            .OfType<Pointer>()
            .Take(6)
            .Select(pointer => $"0x{pointer.Raw:X8}/{pointer.Kind}");

        return $"array[{array.Length}] [{string.Join(", ", pointers)}]";
    }

    private static string DescribePointer(Pointer pointer)
    {
        return $"raw=0x{pointer.Raw:X8} kind={pointer.Kind} address={FormatAddress(pointer.Address)}";
    }

    private static string FormatAddress(XBlockAddress? address)
    {
        return address is { } value
            ? $"{value.Block}:0x{value.Offset:X}"
            : "<none>";
    }
}
