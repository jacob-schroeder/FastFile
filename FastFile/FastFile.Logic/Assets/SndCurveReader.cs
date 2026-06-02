using FastFile.Logic.Assets.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class SndCurveReader
{
    public static SndCurve Read(ref ZoneReadContext context)
    {
        var asset = new SndCurve
        {
            Offset = context.Position,
            FilenamePtr = GenericReader.ReadStringPointer(ref context),
            KnotCount = context.ReadUInt16(),
        };

        context.ReadBytes(2); // align float knots
        context.ReadBytes(16 * 2 * 4);

        return asset;
    }

    public static ZonePointer<SndCurve> ReadSndCurvePointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<SndCurve>(
            (ref ZoneReadContext pointerContext, ZonePointer<SndCurve> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
    }
}
