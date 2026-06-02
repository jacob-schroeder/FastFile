using FastFile.Logic.Assets.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

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
        };

        context.ReadFloat(); // bulletForceScale
        context.ReadFloat(); // explosiveForceScale
        GenericReader.ReadStringPointer(ref context); // sndAliasPrefix
        context.ReadFloat(); // piecesSpreadFraction
        context.ReadFloat(); // piecesUpwardVelocity
        context.ReadByte(); // tempDefaultToCylinder
        context.ReadByte(); // perSurfaceSndAlias
        context.ReadBytes(2);

        return asset;
    }

    public static PhysCollmap ReadPhysCollmap(ref ZoneReadContext context)
    {
        var asset = new PhysCollmap
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Count = context.ReadUInt32(),
        };

        context.ReadPointer<byte>(); // geoms
        context.ReadBytes(36); // PhysMass
        context.ReadBytes(24); // Bounds

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
}
