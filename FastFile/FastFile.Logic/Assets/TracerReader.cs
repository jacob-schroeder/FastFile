using FastFile.Logic.Assets.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class TracerReader
{
    public static TracerDef Read(ref ZoneReadContext context)
    {
        var asset = new TracerDef
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Material = MaterialReader.ReadMaterialPointer(ref context),
            DrawInterval = context.ReadUInt32(),
        };

        context.ReadBytes(5 * 4); // speed, beamLength, beamWidth, screwRadius, screwDist
        context.ReadBytes(5 * 4 * 4); // colors

        return asset;
    }

    public static ZonePointer<TracerDef> ReadTracerPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<TracerDef>(
            (ref ZoneReadContext pointerContext, ZonePointer<TracerDef> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
    }
}
