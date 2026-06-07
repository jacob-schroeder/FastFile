using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private static void WriteSndAliasList(XFileWriterContext context, SndAliasList asset)
    {
        context.WritePointerRaw(asset.AliasNamePtr, PointerResolutionKind.Direct, "SndAliasList.AliasName");
        WriteSndAliasTablePointer(context, asset);
        context.WriteInt32(asset.Count);

        WriteQueuedLargeString(context, asset.AliasNamePtr);
        WriteQueuedSndAliasTable(context, asset);
    }

    private static void WriteSndCurve(XFileWriterContext context, SndCurve curve)
    {
        context.WritePointerRaw(curve.FilenamePtr, PointerResolutionKind.Direct, "SndCurve.Filename");
        context.WriteUInt16(curve.KnotCount);
        WriteFixedBytes(context, curve.AlignmentPadding, 2);
        WriteFixedBytes(context, curve.KnotBytes, 16 * 2 * 4);

        WriteQueuedLargeString(context, curve.FilenamePtr);
    }

    private static void WriteLoadedSound(XFileWriterContext context, LoadedSound sound)
    {
        context.WritePointerRaw(sound.NamePtr, PointerResolutionKind.Direct, "LoadedSound.Name");
        context.WriteInt32(sound.PhysicalDataByteCount);
        WriteFixedBytes(context, sound.SoundInfoBytes, 10);
        context.WriteUInt16(sound.SeekTableCount);
        context.WritePointerRaw(sound.SeekTablePtr, PointerResolutionKind.Direct, "LoadedSound.SeekTable");
        context.WritePointerRaw(sound.PhysicalDataPtr, PointerResolutionKind.Direct, "LoadedSound.PhysicalData");

        WriteQueuedLargeString(context, sound.NamePtr);
        WriteQueuedLoadedSoundSeekTable(context, sound);
        WriteQueuedLoadedSoundPhysicalData(context, sound);
    }

    private static void WriteSndAliasTablePointer(XFileWriterContext context, SndAliasList asset)
    {
        if (asset.AliasesPtr is { Kind: not PointerKind.Null })
        {
            context.WritePointerRaw(asset.AliasesPtr, PointerResolutionKind.Direct, "SndAliasList.Head");
            return;
        }

        context.WritePointerRaw(asset.Head, PointerResolutionKind.Direct, "SndAliasList.Head");
    }

    private static void WriteQueuedSndAliasTable(XFileWriterContext context, SndAliasList asset)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSndAliasTable(context, asset)))
            return;

        var aliases = GetSndAliasRows(asset);
        if (aliases.Length == 0)
            return;

        if (asset.AliasesPtr is { IsInlineData: true })
        {
            context.RegisterMaterializedPointerValue(asset.AliasesPtr);

            foreach (var alias in aliases)
                WriteSndAlias(context, alias);

            foreach (var alias in aliases)
                WriteQueuedSndAliasPointers(context, alias);

            return;
        }

        if (asset.Head is not { IsInlineData: true })
            return;

        context.RegisterMaterializedPointerValue(asset.Head);
        foreach (var alias in aliases)
            WriteSndAlias(context, alias);

        foreach (var alias in aliases)
            WriteQueuedSndAliasPointers(context, alias);
    }

    private static SndAlias[] GetSndAliasRows(SndAliasList asset)
    {
        if (asset.AliasesPtr?.Result is { Length: > 0 } aliases)
            return aliases;

        if (asset.Head?.Result is null)
            return [];

        var count = Math.Max(1, asset.Count);
        var rows = new SndAlias[count];
        rows[0] = asset.Head.Result;
        for (var i = 1; i < rows.Length; i++)
            rows[i] = new SndAlias();

        return rows;
    }

    private static void WriteSndAlias(XFileWriterContext context, SndAlias alias)
    {
        context.WritePointerRaw(alias.AliasNamePtr, PointerResolutionKind.Direct, "SndAlias.AliasName");
        context.WritePointerRaw(alias.Subtitle, PointerResolutionKind.Direct, "SndAlias.Subtitle");
        context.WritePointerRaw(alias.SecondaryAliasName, PointerResolutionKind.Direct, "SndAlias.SecondaryAliasName");
        context.WritePointerRaw(alias.ChainAliasName, PointerResolutionKind.Direct, "SndAlias.ChainAliasName");
        context.WritePointerRaw(alias.MixerGroup, PointerResolutionKind.Direct, "SndAlias.MixerGroup");
        context.WritePointerRaw(alias.SoundFile, PointerResolutionKind.Alias, "SndAlias.SoundFile");
        context.WriteInt32(alias.Sequence);
        context.WriteFloat(alias.VolMin);
        context.WriteFloat(alias.VolMax);
        context.WriteFloat(alias.PitchMin);
        context.WriteFloat(alias.PitchMax);
        context.WriteFloat(alias.DistMin);
        context.WriteFloat(alias.DistMax);
        context.WriteFloat(alias.VelocityMin);
        context.WriteInt32(alias.Flags);
        context.WriteFloat(alias.SlavePercentage);
        context.WriteFloat(alias.Probability);
        context.WriteFloat(alias.LfePercentage);
        context.WriteFloat(alias.CenterPercentage);
        context.WriteInt32(alias.StartDelay);
        context.WritePointerRaw(alias.VolumeFalloffCurve, PointerResolutionKind.Alias, "SndAlias.VolumeFalloffCurve");
        context.WriteFloat(alias.EnvelopMin);
        context.WriteFloat(alias.EnvelopMax);
        context.WriteFloat(alias.EnvelopPercentage);
        context.WritePointerRaw(alias.SpeakerMap, PointerResolutionKind.Direct, "SndAlias.SpeakerMap");
    }

    private static void WriteQueuedSndAliasPointers(XFileWriterContext context, SndAlias alias)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSndAliasPointers(context, alias)))
            return;

        WriteQueuedLargeString(context, alias.AliasNamePtr);
        WriteQueuedLargeString(context, alias.Subtitle);
        WriteQueuedLargeString(context, alias.SecondaryAliasName);
        WriteQueuedLargeString(context, alias.ChainAliasName);
        WriteQueuedLargeString(context, alias.MixerGroup);
        WriteQueuedSoundFile(context, alias.SoundFile);
        WriteQueuedSndCurve(context, alias.VolumeFalloffCurve);
        WriteQueuedSpeakerMap(context, alias.SpeakerMap);
    }

    private static void WriteQueuedSoundFile(XFileWriterContext context, ZonePointer<SoundFile>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSoundFile(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.RegisterMaterializedPointerValue(pointer);
            WriteSoundFile(context, pointer.Result);
        });
    }

    private static void WriteSoundFile(XFileWriterContext context, SoundFile soundFile)
    {
        context.WriteByte((byte)soundFile.Type);
        context.WriteByte(soundFile.Exists);
        WriteFixedBytes(context, soundFile.Padding, 2);

        if (soundFile.Type == SndAliasType.SAT_LOADED
            && soundFile.LoadedSoundPtr is { Kind: not PointerKind.Null })
        {
            context.WritePointerRaw(soundFile.LoadedSoundPtr, PointerResolutionKind.Alias, "SoundFile.LoadedSound");
            WriteFixedBytes(context, Slice(soundFile.Sound?.Raw, 4, 8), 8);
            WriteQueuedLoadedSoundReference(context, soundFile.LoadedSoundPtr);
            return;
        }

        if (soundFile.StreamFileName is not null)
        {
            WriteStreamFileName(context, soundFile.StreamFileName);
            return;
        }

        WriteFixedBytes(context, soundFile.Sound?.Raw, 12);
    }

    private static void WriteStreamFileName(XFileWriterContext context, StreamFileName filename)
    {
        context.WriteUInt32(filename.FileIndex);

        if (filename.FileIndex == 0)
        {
            context.WritePointerRaw(filename.Info.Raw.Dir, PointerResolutionKind.Direct, "StreamFileName.Dir");
            context.WritePointerRaw(filename.Info.Raw.Name, PointerResolutionKind.Direct, "StreamFileName.Name");
            WriteQueuedLargeString(context, filename.Info.Raw.Dir);
            WriteQueuedLargeString(context, filename.Info.Raw.Name);
            return;
        }

        context.WriteUInt32(filename.Info.Packed.Offset);
        context.WriteUInt32(filename.Info.Packed.Length);
    }

    private static void WriteQueuedLoadedSoundReference(
        XFileWriterContext context,
        ZonePointer<LoadedSound>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedLoadedSoundReference(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        WriteInlineAssetReferenceBody(context, pointer, WriteLoadedSound);
    }

    private static void WriteQueuedSndCurve(XFileWriterContext context, ZonePointer<SndCurve>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSndCurve(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        WriteInlineAssetReferenceBody(context, pointer, WriteSndCurve);
    }

    private static void WriteQueuedSpeakerMap(XFileWriterContext context, ZonePointer<SpeakerMap>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSpeakerMap(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);
        WriteSpeakerMap(context, pointer.Result);
        WriteQueuedLargeString(context, pointer.Result.NamePtr);
    }

    private static void WriteSpeakerMap(XFileWriterContext context, SpeakerMap speakerMap)
    {
        context.WriteByte(speakerMap.IsDefault);
        WriteFixedBytes(context, speakerMap.Padding, 3);
        context.WritePointerRaw(speakerMap.NamePtr, PointerResolutionKind.Direct, "SpeakerMap.Name");

        for (var i = 0; i < 2; i++)
        {
            var channelSet = speakerMap.ChannelMaps is not null && i < speakerMap.ChannelMaps.Length
                ? speakerMap.ChannelMaps[i]
                : [];

            for (var j = 0; j < 2; j++)
            {
                var channelMap = channelSet is not null && j < channelSet.Length
                    ? channelSet[j]
                    : new ChannelMap();
                WriteChannelMap(context, channelMap);
            }
        }
    }

    private static void WriteChannelMap(XFileWriterContext context, ChannelMap channelMap)
    {
        context.WriteInt32(channelMap.EntryCount);

        for (var i = 0; i < 6; i++)
        {
            var levels = channelMap.Speakers is not null && i < channelMap.Speakers.Length
                ? channelMap.Speakers[i]
                : new SpeakerLevels();
            WriteSpeakerLevels(context, levels);
        }
    }

    private static void WriteSpeakerLevels(XFileWriterContext context, SpeakerLevels levels)
    {
        context.WriteInt32(levels.Speaker);
        context.WriteInt32(levels.NumLevels);

        for (var i = 0; i < 2; i++)
        {
            var value = levels.Levels is not null && i < levels.Levels.Length
                ? levels.Levels[i]
                : 0f;
            context.WriteFloat(value);
        }
    }

    private static void WriteQueuedLoadedSoundSeekTable(XFileWriterContext context, LoadedSound sound)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedLoadedSoundSeekTable(context, sound)))
            return;

        if (sound.SeekTablePtr is not { IsInlineData: true, Result: not null })
            return;

        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.RegisterMaterializedPointerValue(sound.SeekTablePtr);
            WriteFixedBytes(context, sound.SeekTablePtr.Result, checked(sound.SeekTableCount * 4));
        });
    }

    private static void WriteQueuedLoadedSoundPhysicalData(XFileWriterContext context, LoadedSound sound)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedLoadedSoundPhysicalData(context, sound)))
            return;

        if (sound.PhysicalDataPtr is not { IsInlineData: true, Result: not null })
            return;

        context.WithStreamBlock(XFILE_BLOCK.PHYSICAL, () =>
        {
            context.RegisterMaterializedPointerValue(sound.PhysicalDataPtr);
            WriteFixedBytes(context, sound.PhysicalDataPtr.Result, Math.Max(0, sound.PhysicalDataByteCount));
        });
    }

    private static void WriteFixedBytes(XFileWriterContext context, byte[]? value, int count)
    {
        if (count <= 0)
            return;

        var copyCount = Math.Min(value?.Length ?? 0, count);
        if (copyCount > 0)
            context.WriteBytes(value!.AsSpan(0, copyCount));

        if (copyCount < count)
            context.WriteZeroes(count - copyCount);
    }

    private static byte[]? Slice(byte[]? value, int offset, int count)
    {
        if (value is null || offset >= value.Length || count <= 0)
            return null;

        var copyCount = Math.Min(count, value.Length - offset);
        var result = new byte[copyCount];
        Array.Copy(value, offset, result, 0, copyCount);
        return result;
    }
}
