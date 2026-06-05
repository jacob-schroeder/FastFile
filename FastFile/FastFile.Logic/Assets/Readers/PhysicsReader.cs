using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class PhysicsReader
{
    public static PhysPreset ReadPhysPreset(ref XFileReadContext context)
    {
        var asset = new PhysPreset
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            PresetType = context.ReadInt32(),
            Mass = context.ReadFloat(),
            Bounce = context.ReadFloat(),
            Friction = context.ReadFloat(),
            BulletForceScale = context.ReadFloat(),
            ExplosiveForceScale = context.ReadFloat(),
            SndAliasPrefix = GenericReader.ReadStringPointer(ref context),
            PiecesSpreadFraction = context.ReadFloat(),
            PiecesUpwardVelocity = context.ReadFloat(),
            TempDefaultToCylinder = context.ReadByte() != 0,
            PerSurfaceSndAlias = context.ReadByte() != 0,
            BoolAlignmentPadding = context.ReadUInt16(),
        };

        return asset;
    }

    public static PhysCollmap ReadPhysCollmap(ref XFileReadContext context)
    {
        var asset = new PhysCollmap
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Count = context.ReadUInt32(),
            Geoms = context.ReadDirectPointer<PhysGeomInfo[]>("PhysCollmap.Geoms"),
            Mass = ReadPhysMass(ref context),
            Bounds = ReadBounds(ref context),
        };

        return asset;
    }

    public static ZonePointer<PhysPreset> ReadPhysPresetPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<PhysPreset>("PhysPresetAssetRef");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<PhysPreset> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadPhysPreset));
            });
        return pointer;
    }

    public static ZonePointer<PhysCollmap> ReadPhysCollmapPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<PhysCollmap>("PhysCollmapAssetRef");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<PhysCollmap> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadPhysCollmap));
            });
        return pointer;
    }

    private static PhysMass ReadPhysMass(ref XFileReadContext context)
    {
        return new PhysMass
        {
            CenterOfMass = ReadVec3(ref context),
            MomentsOfInertia = ReadVec3(ref context),
            ProductsOfInertia = ReadVec3(ref context),
        };
    }

    private static FastFile.Models.Utils.Bounds ReadBounds(ref XFileReadContext context)
    {
        return new FastFile.Models.Utils.Bounds
        {
            MidPoint = ReadVec3(ref context),
            HalfSize = ReadVec3(ref context),
        };
    }

    private static FastFile.Models.Utils.Vec3 ReadVec3(ref XFileReadContext context)
    {
        return new FastFile.Models.Utils.Vec3
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            Z = context.ReadFloat(),
        };
    }
}
