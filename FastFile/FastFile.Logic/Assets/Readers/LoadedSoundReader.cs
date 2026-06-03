using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class LoadedSoundReader
{
    public static LoadedSound Read(ref ZoneReadContext context)
    {
        var asset = new LoadedSound
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
        };

        context.ReadBytes(36); // AILSOUNDINFO
        context.ReadPointer<byte>(); // MssSound.data

        return asset;
    }

    public static ZonePointer<LoadedSound> ReadLoadedSoundPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<LoadedSound>(
            (ref ZoneReadContext pointerContext, ZonePointer<LoadedSound> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
    }
}
