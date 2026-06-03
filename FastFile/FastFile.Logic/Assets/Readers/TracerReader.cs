using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

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
            Speed = context.ReadFloat(),
            BeamLength = context.ReadFloat(),
            BeamWidth = context.ReadFloat(),
            ScrewRadius = context.ReadFloat(),
            ScrewDist = context.ReadFloat(),
        };

        for (var i = 0; i < asset.Colors.Length; i++)
            asset.Colors[i] = context.ReadVec4();

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
