using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;
using FastFile.Models.Utils;

namespace FastFile.Logic.Assets.Writers;

internal static class PhysicsWriter
{
    public static void WritePhysPreset(ZoneWriterContext context, BaseAsset asset)
    {
        WritePhysPresetValue(context, (PhysPreset)asset);
    }

    public static void WritePhysCollmap(ZoneWriterContext context, BaseAsset asset)
    {
        WritePhysCollmapValue(context, (PhysCollmap)asset);
    }

    public static void WritePhysPresetPointer(ZoneWriterContext context, ZonePointer<PhysPreset>? pointer)
    {
        context.WritePointer(pointer, WritePhysPresetPointerValue);
    }

    public static void WritePhysCollmapPointer(ZoneWriterContext context, ZonePointer<PhysCollmap>? pointer)
    {
        context.WritePointer(pointer, WritePhysCollmapPointerValue);
    }

    private static void WritePhysPresetPointerValue(ZoneWriterContext context, ZonePointer<PhysPreset> pointer)
    {
        if (pointer.Result is { } value)
            WritePhysPresetValue(context, value);
    }

    private static void WritePhysCollmapPointerValue(ZoneWriterContext context, ZonePointer<PhysCollmap> pointer)
    {
        if (pointer.Result is { } value)
            WritePhysCollmapValue(context, value);
    }

    private static void WritePhysPresetValue(ZoneWriterContext context, PhysPreset asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WriteInt32(asset.PresetType);
        context.WriteFloat(asset.Mass);
        context.WriteFloat(asset.Bounce);
        context.WriteFloat(asset.Friction);
        context.WriteFloat(asset.BulletForceScale);
        context.WriteFloat(asset.ExplosiveForceScale);
        GenericWriter.WriteStringPointer(context, asset.SndAliasPrefix);
        context.WriteFloat(asset.PiecesSpreadFraction);
        context.WriteFloat(asset.PiecesUpwardVelocity);
        context.WriteBool(asset.TempDefaultToCylinder);
        context.WriteBool(asset.PerSurfaceSndAlias);
        context.WriteUInt16(asset.BoolAlignmentPadding);
    }

    private static void WritePhysCollmapValue(ZoneWriterContext context, PhysCollmap asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WriteUInt32(asset.Count);
        context.WritePointerRaw(asset.Geoms);
        WritePhysMass(context, asset.Mass);
        context.WriteBounds(asset.Bounds);
    }

    private static void WritePhysMass(ZoneWriterContext context, PhysMass mass)
    {
        context.WriteVec3(mass.CenterOfMass);
        context.WriteVec3(mass.MomentsOfInertia);
        context.WriteVec3(mass.ProductsOfInertia);
    }
}
