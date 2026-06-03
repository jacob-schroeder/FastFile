using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class TracerWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteTracerDef(context, (TracerDef)asset);
    }

    public static void WriteTracerPointer(ZoneWriterContext context, ZonePointer<TracerDef>? pointer)
    {
        context.WritePointer(pointer, WriteTracerPointerValue);
    }

    private static void WriteTracerPointerValue(ZoneWriterContext context, ZonePointer<TracerDef> pointer)
    {
        if (pointer.Result is { } value)
            WriteTracerDef(context, value);
    }

    private static void WriteTracerDef(ZoneWriterContext context, TracerDef asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        MaterialWriter.WriteMaterialPointer(context, asset.Material);
        context.WriteUInt32(asset.DrawInterval);
        context.WriteFloat(asset.Speed);
        context.WriteFloat(asset.BeamLength);
        context.WriteFloat(asset.BeamWidth);
        context.WriteFloat(asset.ScrewRadius);
        context.WriteFloat(asset.ScrewDist);
        foreach (var color in asset.Colors)
            context.WriteVec4(color);
    }
}
