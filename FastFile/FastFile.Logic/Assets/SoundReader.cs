using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class SoundReader
{
    public static SndAliasList Read(ref ZoneReadContext context)
    {
        var asset = new SndAliasList
        {
            Offset = context.Position,
            AliasNamePtr = GenericReader.ReadStringPointer(ref context),
            Head = ReadSndAliasPointer(ref context),
            Count = context.ReadInt32(),
        };

        return asset;
    }

    public static ZonePointer<SndAliasList> ReadSndAliasListPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<SndAliasList>(
            (ref ZoneReadContext pointerContext, ZonePointer<SndAliasList> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, Read);
                pointer.SetResult(value);
            });
    }

    private static ZonePointer<SndAlias> ReadSndAliasPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<SndAlias>(
            (ref ZoneReadContext pointerContext, ZonePointer<SndAlias> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, ReadSndAlias);
                pointer.SetResult(value);
            });
    }

    private static SndAlias ReadSndAlias(ref ZoneReadContext context)
    {
        return new SndAlias
        {
            AliasNamePtr = GenericReader.ReadStringPointer(ref context),
            Subtitle = GenericReader.ReadStringPointer(ref context),
            SecondaryAliasName = GenericReader.ReadStringPointer(ref context),
            ChainAliasName = GenericReader.ReadStringPointer(ref context),
            MixerGroup = GenericReader.ReadStringPointer(ref context),
            SoundFile = ReadSoundFilePointer(ref context),
            Sequence = context.ReadInt32(),
            VolMin = context.ReadFloat(),
            VolMax = context.ReadFloat(),
            PitchMin = context.ReadFloat(),
            PitchMax = context.ReadFloat(),
            DistMin = context.ReadFloat(),
            DistMax = context.ReadFloat(),
            VelocityMin = context.ReadFloat(),
            Flags = context.ReadInt32(),
            SlavePercentage = context.ReadFloat(),
            Probability = context.ReadFloat(),
            LfePercentage = context.ReadFloat(),
            CenterPercentage = context.ReadFloat(),
            StartDelay = context.ReadInt32(),
            VolumeFalloffCurve = SndCurveReader.ReadSndCurvePointer(ref context),
            EnvelopMin = context.ReadFloat(),
            EnvelopMax = context.ReadFloat(),
            EnvelopPercentage = context.ReadFloat(),
            SpeakerMap = ReadSpeakerMapPointer(ref context),
        };
    }

    private static ZonePointer<SoundFile> ReadSoundFilePointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<SoundFile>(
            (ref ZoneReadContext pointerContext, ZonePointer<SoundFile> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, ReadSoundFile);
                pointer.SetResult(value);
            });
    }

    private static SoundFile ReadSoundFile(ref ZoneReadContext context)
    {
        var soundFile = new SoundFile
        {
            Type = (SndAliasType)context.ReadByte(),
            Exists = context.ReadBool(),
        };

        context.Position += 2;
        soundFile.Sound = new SoundData
        {
            Raw = context.ReadBytes(12),
        };

        return soundFile;
    }

    private static ZonePointer<SpeakerMap> ReadSpeakerMapPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<SpeakerMap>(
            (ref ZoneReadContext pointerContext, ZonePointer<SpeakerMap> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, ReadSpeakerMap);
                pointer.SetResult(value);
            });
    }

    private static SpeakerMap ReadSpeakerMap(ref ZoneReadContext context)
    {
        var speakerMap = new SpeakerMap
        {
            IsDefault = context.ReadBool(),
        };

        context.Position += 3;
        speakerMap.NamePtr = GenericReader.ReadStringPointer(ref context);
        speakerMap.ChannelMaps = new ChannelMap[2][];

        for (var i = 0; i < speakerMap.ChannelMaps.Length; i++)
        {
            speakerMap.ChannelMaps[i] = new ChannelMap[2];
            for (var j = 0; j < speakerMap.ChannelMaps[i].Length; j++)
                speakerMap.ChannelMaps[i][j] = ReadChannelMap(ref context);
        }

        return speakerMap;
    }

    private static ChannelMap ReadChannelMap(ref ZoneReadContext context)
    {
        var channelMap = new ChannelMap
        {
            EntryCount = context.ReadInt32(),
        };

        for (var i = 0; i < channelMap.Speakers.Length; i++)
            channelMap.Speakers[i] = ReadSpeakerLevels(ref context);

        return channelMap;
    }

    private static SpeakerLevels ReadSpeakerLevels(ref ZoneReadContext context)
    {
        var levels = new SpeakerLevels
        {
            Speaker = context.ReadInt32(),
            NumLevels = context.ReadInt32(),
        };

        for (var i = 0; i < levels.Levels.Length; i++)
            levels.Levels[i] = context.ReadFloat();

        return levels;
    }
}
