using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class SndCurveReader
{
    public static SndCurve Read(ref XFileReadContext context)
    {
        var asset = new SndCurve
        {
            Offset = context.Position,
            FilenamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            KnotCount = context.ReadUInt16(),
        };

        asset.AlignmentPadding = context.ReadBytes(2);
        asset.KnotBytes = context.ReadBytes(16 * 2 * 4);
        context.ResolvePointerInBlock(asset.FilenamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);

        return asset;
    }

    public static ZonePointer<SndCurve> ReadSndCurvePointer(
        ref XFileReadContext context,
        bool resolve = true)
    {
        var pointer = context.ReadAliasPointer<SndCurve>("SndCurveAssetRef");
        if (resolve)
            context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadSndCurvePointerValue);

        return pointer;
    }

    public static void ReadSndCurvePointerValue(
        ref XFileReadContext context,
        ZonePointer<SndCurve> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }
}
