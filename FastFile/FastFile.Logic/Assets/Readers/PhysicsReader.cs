using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class PhysicsReader
{
    public static PhysPreset ReadPhysPreset(ref ZoneReadContext context)
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

    public static PhysCollmap ReadPhysCollmap(ref ZoneReadContext context)
    {
        var asset = new PhysCollmap
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Count = context.ReadUInt32(),
            Geoms = context.ReadPointer<PhysGeomInfo[]>(),
            Mass = ReadPhysMass(ref context),
            Bounds = ReadBounds(ref context),
        };

        return asset;
    }

    public static ZonePointer<PhysPreset> ReadPhysPresetPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<PhysPreset>(
            (ref ZoneReadContext pointerContext, ZonePointer<PhysPreset> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadPhysPreset));
            });
    }

    public static ZonePointer<PhysCollmap> ReadPhysCollmapPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<PhysCollmap>(
            (ref ZoneReadContext pointerContext, ZonePointer<PhysCollmap> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, ReadPhysCollmap));
            });
    }

    private static PhysMass ReadPhysMass(ref ZoneReadContext context)
    {
        return new PhysMass
        {
            CenterOfMass = ReadVec3(ref context),
            MomentsOfInertia = ReadVec3(ref context),
            ProductsOfInertia = ReadVec3(ref context),
        };
    }

    private static FastFile.Models.Utils.Bounds ReadBounds(ref ZoneReadContext context)
    {
        return new FastFile.Models.Utils.Bounds
        {
            MidPoint = ReadVec3(ref context),
            HalfSize = ReadVec3(ref context),
        };
    }

    private static FastFile.Models.Utils.Vec3 ReadVec3(ref ZoneReadContext context)
    {
        return new FastFile.Models.Utils.Vec3
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            Z = context.ReadFloat(),
        };
    }
}
