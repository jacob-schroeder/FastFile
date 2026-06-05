using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class LoadedSoundReader
{
    public static LoadedSound Read(ref XFileReadContext context)
    {
        var asset = new LoadedSound
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
        };

        context.ReadBytes(36); // AILSOUNDINFO
        context.ReadDirectPointer<byte>("LoadedSound.SoundData"); // MssSound.data

        return asset;
    }

    public static ZonePointer<LoadedSound> ReadLoadedSoundPointer(ref XFileReadContext context)
    {
        return context.ReadPointer<LoadedSound>(
            (ref XFileReadContext pointerContext, ZonePointer<LoadedSound> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            },
            PointerResolutionKind.Alias,
            "LoadedSoundAssetRef");
    }
}
