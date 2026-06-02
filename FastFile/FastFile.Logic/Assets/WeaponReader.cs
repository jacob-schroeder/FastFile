using FastFile.Logic.Assets.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Utils;

namespace FastFile.Logic.Assets;

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

        SkipRemainingFixed(ref context, start, WeaponVariantDefSize, "WeaponVariantDef");
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

        ReadUShortArrayPointer(ref context, NoteTrackMapCount);
        ReadUShortArrayPointer(ref context, NoteTrackMapCount);
        ReadUShortArrayPointer(ref context, NoteTrackMapCount);
        ReadUShortArrayPointer(ref context, NoteTrackMapCount);

        Skip32(ref context, 8); // playerAnimType through stance
        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);

        for (var i = 0; i < WeaponSoundAliasCount; i++)
            ReadSoundAliasPointer(ref context);
        GenericReader.ReadStringPointerArrayPointer(ref context, SurfaceCount); // bounceSound

        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        Skip32(ref context, 4); // reticle sizes and active reticle
        Skip32(ref context, 30); // ten vec3 view movement/rotation groups
        Skip32(ref context, 10); // positional movement/rotation rates

        XModelReader.ReadXModelPointerArrayPointer(ref context, GunModelCount);
        XModelReader.ReadXModelPointer(ref context);
        XModelReader.ReadXModelPointer(ref context);
        XModelReader.ReadXModelPointer(ref context);
        XModelReader.ReadXModelPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        Skip32(ref context, 1);
        MaterialReader.ReadMaterialPointer(ref context);
        Skip32(ref context, 1);
        MaterialReader.ReadMaterialPointer(ref context);
        Skip32(ref context, 3);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 1);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 3);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 6);
        Skip32(ref context, 40); // weapon timing fields
        Skip32(ref context, 10); // aim/movement tuning through ADS zoom fractions

        MaterialReader.ReadMaterialPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        MaterialReader.ReadMaterialPointer(ref context);
        Skip32(ref context, 6); // overlay enums and dimensions
        Skip32(ref context, 38); // bob, spread, idle, sway, and ADS view error values

        PhysicsReader.ReadPhysCollmapPointer(ref context);
        Skip32(ref context, 2);
        Skip32(ref context, 5);
        Skip32(ref context, 7);
        Skip32(ref context, 7);
        XModelReader.ReadXModelPointer(ref context);
        Skip32(ref context, 1);
        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        ReadSoundAliasPointer(ref context);
        ReadSoundAliasPointer(ref context);
        Skip32(ref context, 3);
        ReadFloatArrayPointer(ref context, SurfaceCount);
        ReadFloatArrayPointer(ref context, SurfaceCount);
        FxReader.ReadFxPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        Skip32(ref context, 3);
        Skip32(ref context, 1);
        Skip32(ref context, 2);
        FxReader.ReadFxPointer(ref context);
        ReadSoundAliasPointer(ref context);
        Skip32(ref context, 3);
        Skip32(ref context, 35); // ADS/hip gun kick and AI distance fields

        GenericReader.ReadStringPointer(ref context);
        GenericReader.ReadStringPointer(ref context);
        var accuracyGraphKnots = context.ReadPointer<Vec2[]>();
        var originalAccuracyGraphKnots = context.ReadPointer<Vec2[]>();
        var accuracyGraphKnotCount = context.ReadUInt16();
        var originalAccuracyGraphKnotCount = context.ReadUInt16();
        ResolveVec2ArrayPointer(ref context, accuracyGraphKnots, accuracyGraphKnotCount);
        ResolveVec2ArrayPointer(ref context, originalAccuracyGraphKnots, originalAccuracyGraphKnotCount);

        Skip32(ref context, 1);
        Skip32(ref context, 17);
        GenericReader.ReadStringPointer(ref context);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 2);
        Skip32(ref context, 5);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 2);
        Skip32(ref context, 6);
        Skip32(ref context, 1);
        ReadFloatArrayPointer(ref context, HitLocationCount);
        GenericReader.ReadStringPointer(ref context);
        GenericReader.ReadStringPointer(ref context);
        TracerReader.ReadTracerPointer(ref context);

        Skip32(ref context, 6);
        ReadSoundAliasPointer(ref context);
        FxReader.ReadFxPointer(ref context);
        GenericReader.ReadStringPointer(ref context);
        Skip32(ref context, 3);
        ReadSoundAliasPointer(ref context);
        for (var i = 0; i < 4; i++)
            ReadSoundAliasPointer(ref context);
        for (var i = 0; i < 4; i++)
            ReadSoundAliasPointer(ref context);
        ReadSoundAliasPointer(ref context);
        ReadSoundAliasPointer(ref context);
        Skip32(ref context, 14);

        SkipRemainingFixed(ref context, start, WeaponDefSize, "WeaponDef");
        return weaponDef;
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

    private static void Skip32(ref ZoneReadContext context, int count)
    {
        Skip(ref context, checked(count * 4));
    }

    private static void Skip(ref ZoneReadContext context, int length)
    {
        if (length == 0)
            return;

        context.ReadBytes(length);
    }

    private static void SkipRemainingFixed(
        ref ZoneReadContext context,
        int start,
        int expectedSize,
        string typeName)
    {
        var read = context.Position - start;
        var remaining = expectedSize - read;
        if (remaining < 0)
            throw new InvalidDataException($"{typeName} read 0x{read:X} bytes; expected 0x{expectedSize:X}.");

        Skip(ref context, remaining);
    }
}
