using System.Buffers.Binary;
using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class SoundReader
{
    private static readonly bool TraceSound =
        IsTraceEnabled("FASTFILE_SOUND_TRACE") || IsTraceEnabled("FASTFILE_TRACE_SOUND");
    private static readonly bool StopOnSuspiciousSoundCount =
        IsTraceEnabled("FASTFILE_SOUND_TRACE_STOP_SUSPICIOUS_COUNT");
    private static readonly int TraceSoundLimit = GetTraceLimit("FASTFILE_SOUND_TRACE_LIMIT");
    private static int _traceSoundCount;

    public static SndAliasList Read(ref XFileReadContext context)
    {
        var rootOffset = context.Position;
        var rootStream = context.GetActiveStreamAddress();
        var asset = new SndAliasList
        {
            Offset = context.Position,
            AliasNamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            AliasesPtr = context.ReadDirectPointer<SndAlias[]>("SndAliasList.Head"),
            Count = context.ReadInt32(),
        };

        Trace(
            $"{AssetLabel(context)} Sound root src=0x{rootOffset:X8} stream=b{rootStream.BlockIndex}:0x{rootStream.Offset:X8} "
            + $"nameRaw=0x{asset.AliasNamePtr.Raw:X8} headRaw=0x{asset.AliasesPtr.Raw:X8} count={asset.Count}");

        asset.Head = context.CreatePointer<SndAlias>(
            asset.AliasesPtr.Raw,
            register: false,
            PointerResolutionKind.Direct,
            "SndAliasList.Head[0]");

        ResolveLargeString(ref context, asset.AliasNamePtr, "SndAliasList.AliasName");
        context.ResolvePointerInBlock(
            asset.AliasesPtr,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<SndAlias[]> pointer) =>
            {
                var tableOffset = pointerContext.Position;
                var tableStream = pointerContext.GetActiveStreamAddress();
                Trace(
                    $"{AssetLabel(pointerContext)} Sound alias table start src=0x{tableOffset:X8} stream=b{tableStream.BlockIndex}:0x{tableStream.Offset:X8} "
                    + $"count={asset.Count} raw=0x{pointer.Raw:X8}");

                if (StopOnSuspiciousSoundCount && (asset.Count < 0 || asset.Count > 100_000))
                    throw new InvalidDataException(
                        $"Trace stop: suspicious SndAliasList.Count={asset.Count:N0} at zone offset 0x{tableOffset:X8}.");

                var aliases = new SndAlias[Math.Max(0, asset.Count)];
                for (var i = 0; i < aliases.Length; i++)
                {
                    if (i < 4 || i == aliases.Length - 1)
                    {
                        var rowStream = pointerContext.GetActiveStreamAddress();
                        Trace(
                            $"{AssetLabel(pointerContext)} Sound alias row[{i}] src=0x{pointerContext.Position:X8} "
                            + $"stream=b{rowStream.BlockIndex}:0x{rowStream.Offset:X8}");
                    }

                    aliases[i] = ReadSndAlias(ref pointerContext, i);
                }

                pointer.SetResult(aliases);
                asset.Head.SetResult(aliases.Length > 0 ? aliases[0] : null);
            });

        return asset;
    }

    public static ZonePointer<SndAliasList> ReadSndAliasListPointer(ref XFileReadContext context)
    {
        return context.ReadPointer<SndAliasList>(
            (ref XFileReadContext pointerContext, ZonePointer<SndAliasList> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, Read);
                pointer.SetResult(value);
            },
            PointerResolutionKind.Alias,
            "SndAliasListAssetRef");
    }

    private static SndAlias ReadSndAlias(ref XFileReadContext context, int rowIndex)
    {
        var rowStart = context.Position;
        var rowStream = context.GetActiveStreamAddress();
        var alias = new SndAlias
        {
            AliasNamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            Subtitle = GenericReader.ReadStringPointer(ref context, resolve: false),
            SecondaryAliasName = GenericReader.ReadStringPointer(ref context, resolve: false),
            ChainAliasName = GenericReader.ReadStringPointer(ref context, resolve: false),
            MixerGroup = GenericReader.ReadStringPointer(ref context, resolve: false),
            SoundFile = ReadSoundFilePointer(ref context, resolve: false),
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
            VolumeFalloffCurve = SndCurveReader.ReadSndCurvePointer(ref context, resolve: false),
            EnvelopMin = context.ReadFloat(),
            EnvelopMax = context.ReadFloat(),
            EnvelopPercentage = context.ReadFloat(),
            SpeakerMap = ReadSpeakerMapPointer(ref context, resolve: false),
        };

        Trace(
            $"{AssetLabel(context)} Sound alias row[{rowIndex}] parsed src=0x{rowStart:X8}/0x{context.Position - rowStart:X} "
            + $"stream=b{rowStream.BlockIndex}:0x{rowStream.Offset:X8} "
            + $"aliasRaw=0x{alias.AliasNamePtr.Raw:X8} subtitleRaw=0x{alias.Subtitle.Raw:X8} "
            + $"secondaryRaw=0x{alias.SecondaryAliasName.Raw:X8} chainRaw=0x{alias.ChainAliasName.Raw:X8} "
            + $"mixerRaw=0x{alias.MixerGroup.Raw:X8} soundFileRaw=0x{alias.SoundFile.Raw:X8} "
            + $"curveRaw=0x{alias.VolumeFalloffCurve.Raw:X8} speakerRaw=0x{alias.SpeakerMap.Raw:X8} "
            + $"seq={alias.Sequence}");

        ResolveLargeString(ref context, alias.AliasNamePtr, $"SndAlias[{rowIndex}].AliasName");
        ResolveLargeString(ref context, alias.Subtitle, $"SndAlias[{rowIndex}].Subtitle");
        ResolveLargeString(ref context, alias.SecondaryAliasName, $"SndAlias[{rowIndex}].SecondaryAliasName");
        ResolveLargeString(ref context, alias.ChainAliasName, $"SndAlias[{rowIndex}].ChainAliasName");
        ResolveLargeString(ref context, alias.MixerGroup, $"SndAlias[{rowIndex}].MixerGroup");
        context.ResolvePointerInBlock(
            alias.SoundFile,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<SoundFile> pointer) =>
                ReadSoundFilePointerValue(ref pointerContext, pointer, $"SndAlias[{rowIndex}].SoundFile"));
        context.ResolvePointerInBlock(
            alias.VolumeFalloffCurve,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<SndCurve> pointer) =>
                ReadSndCurvePointerValue(ref pointerContext, pointer, $"SndAlias[{rowIndex}].VolumeFalloffCurve"));
        context.ResolvePointerInBlock(
            alias.SpeakerMap,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<SpeakerMap> pointer) =>
                ReadSpeakerMapPointerValue(ref pointerContext, pointer, $"SndAlias[{rowIndex}].SpeakerMap"));

        return alias;
    }

    private static ZonePointer<SoundFile> ReadSoundFilePointer(
        ref XFileReadContext context,
        bool resolve = true)
    {
        var pointer = context.ReadAliasPointer<SoundFile>("SndAlias.SoundFile");
        if (resolve)
            context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadSoundFilePointerValue);

        return pointer;
    }

    private static void ReadSoundFilePointerValue(
        ref XFileReadContext context,
        ZonePointer<SoundFile> pointer)
    {
        ReadSoundFilePointerValue(ref context, pointer, "SndAlias.SoundFile");
    }

    private static void ReadSoundFilePointerValue(
        ref XFileReadContext context,
        ZonePointer<SoundFile> pointer,
        string label)
    {
        var start = context.Position;
        var stream = context.GetActiveStreamAddress();
        Trace(
            $"{AssetLabel(context)} {label} begin src=0x{start:X8} "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} raw=0x{pointer.Raw:X8} kind={pointer.Kind}");

        var value = context.ReadPointerValue(pointer, ReadSoundFile);
        pointer.SetResult(value);

        Trace(
            $"{AssetLabel(context)} {label} end src=0x{start:X8}/0x{context.Position - start:X} "
            + $"type={value?.Type.ToString() ?? "<null>"} exists={value?.Exists.ToString() ?? "<null>"}");
    }

    private static SoundFile ReadSoundFile(ref XFileReadContext context)
    {
        var rootStart = context.Position;
        var rootStream = context.GetActiveStreamAddress();
        var soundFile = new SoundFile
        {
            Type = (SndAliasType)context.ReadByte(),
            Exists = context.ReadByte(),
        };

        soundFile.Padding = context.ReadBytes(2);

        var unionStart = context.Position;
        var unionRaw = context.Span.Slice(unionStart, 12).ToArray();
        var unionFirstWord = BinaryPrimitives.ReadUInt32BigEndian(unionRaw.AsSpan(0, 4));
        soundFile.Sound = new SoundData
        {
            Raw = unionRaw,
        };

        Trace(
            $"{AssetLabel(context)} SoundFile root src=0x{rootStart:X8} "
            + $"stream=b{rootStream.BlockIndex}:0x{rootStream.Offset:X8} "
            + $"type=0x{(byte)soundFile.Type:X2}/{soundFile.Type} exists=0x{soundFile.Exists:X2} "
            + $"union=0x{unionStart:X8} firstWord=0x{unionFirstWord:X8}");

        if (soundFile.Type == SndAliasType.SAT_LOADED)
        {
            soundFile.LoadedSoundPtr = context.ReadPointer<LoadedSound>(
                PointerResolutionKind.Alias,
                "SoundFile.LoadedSound");
            context.ReadBytes(8);
            Trace(
                $"{AssetLabel(context)} SoundFile loaded branch loadedSoundRaw=0x{soundFile.LoadedSoundPtr.Raw:X8}");
            context.ResolvePointerInBlock(
                soundFile.LoadedSoundPtr,
                XFILE_BLOCK.TEMP,
                LoadedSoundReader.ReadLoadedSoundPointerValue);
        }
        else
        {
            soundFile.StreamFileName = ReadStreamFileName(ref context);
        }

        return soundFile;
    }

    private static StreamFileName ReadStreamFileName(ref XFileReadContext context)
    {
        var start = context.Position;
        var filename = new StreamFileName
        {
            FileIndex = context.ReadUInt32(),
            Info = new StreamFileInfo(),
        };

        if (filename.FileIndex == 0)
        {
            filename.Info.Raw = new StreamFileNameRaw
            {
                Dir = GenericReader.ReadStringPointer(ref context, resolve: false),
                Name = GenericReader.ReadStringPointer(ref context, resolve: false),
            };

            ResolveLargeString(ref context, filename.Info.Raw.Dir, "StreamFileName.Dir");
            ResolveLargeString(ref context, filename.Info.Raw.Name, "StreamFileName.Name");
            Trace(
                $"{AssetLabel(context)} StreamFileName raw src=0x{start:X8}/0x{context.Position - start:X} "
                + $"fileIndex=0x{filename.FileIndex:X8} "
                + $"dirRaw=0x{filename.Info.Raw.Dir.Raw:X8} nameRaw=0x{filename.Info.Raw.Name.Raw:X8}");
        }
        else
        {
            filename.Info.Packed = new StreamFileNamePacked
            {
                Offset = context.ReadUInt32(),
                Length = context.ReadUInt32(),
            };
            Trace(
                $"{AssetLabel(context)} StreamFileName packed src=0x{start:X8}/0x{context.Position - start:X} "
                + $"fileIndex=0x{filename.FileIndex:X8} "
                + $"offset=0x{filename.Info.Packed.Offset:X8} length=0x{filename.Info.Packed.Length:X8}");
        }

        return filename;
    }

    private static ZonePointer<SpeakerMap> ReadSpeakerMapPointer(
        ref XFileReadContext context,
        bool resolve = true)
    {
        var pointer = context.ReadDirectPointer<SpeakerMap>("SndAlias.SpeakerMap");
        if (resolve)
            context.ResolvePointerInBlock(pointer, XFILE_BLOCK.LARGE, ReadSpeakerMapPointerValue);

        return pointer;
    }

    private static void ReadSpeakerMapPointerValue(
        ref XFileReadContext context,
        ZonePointer<SpeakerMap> pointer)
    {
        ReadSpeakerMapPointerValue(ref context, pointer, "SndAlias.SpeakerMap");
    }

    private static void ReadSndCurvePointerValue(
        ref XFileReadContext context,
        ZonePointer<SndCurve> pointer,
        string label)
    {
        var start = context.Position;
        var stream = context.GetActiveStreamAddress();
        Trace(
            $"{AssetLabel(context)} {label} begin src=0x{start:X8} "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} raw=0x{pointer.Raw:X8} kind={pointer.Kind}");

        SndCurveReader.ReadSndCurvePointerValue(ref context, pointer);

        Trace(
            $"{AssetLabel(context)} {label} end src=0x{start:X8}/0x{context.Position - start:X} "
            + $"filenameRaw=0x{pointer.Result?.FilenamePtr.Raw ?? 0:X8} knotCount={pointer.Result?.KnotCount.ToString() ?? "<null>"}");
    }

    private static void ReadSpeakerMapPointerValue(
        ref XFileReadContext context,
        ZonePointer<SpeakerMap> pointer,
        string label)
    {
        var start = context.Position;
        var stream = context.GetActiveStreamAddress();
        Trace(
            $"{AssetLabel(context)} {label} begin src=0x{start:X8} "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} raw=0x{pointer.Raw:X8} kind={pointer.Kind}");

        var value = context.ReadPointerValue(pointer, ReadSpeakerMap);
        pointer.SetResult(value);

        Trace(
            $"{AssetLabel(context)} {label} end src=0x{start:X8}/0x{context.Position - start:X} "
            + $"isDefault={value?.IsDefault.ToString() ?? "<null>"} nameRaw=0x{value?.NamePtr.Raw ?? 0:X8}");
    }

    private static SpeakerMap ReadSpeakerMap(ref XFileReadContext context)
    {
        var start = context.Position;
        var stream = context.GetActiveStreamAddress();
        var speakerMap = new SpeakerMap
        {
            IsDefault = context.ReadByte(),
        };

        speakerMap.Padding = context.ReadBytes(3);
        speakerMap.NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false);
        speakerMap.ChannelMaps = new ChannelMap[2][];

        for (var i = 0; i < speakerMap.ChannelMaps.Length; i++)
        {
            speakerMap.ChannelMaps[i] = new ChannelMap[2];
            for (var j = 0; j < speakerMap.ChannelMaps[i].Length; j++)
                speakerMap.ChannelMaps[i][j] = ReadChannelMap(ref context);
        }

        Trace(
            $"{AssetLabel(context)} SpeakerMap root src=0x{start:X8}/0x{context.Position - start:X} "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} isDefault={speakerMap.IsDefault} "
            + $"nameRaw=0x{speakerMap.NamePtr.Raw:X8}");

        ResolveLargeString(ref context, speakerMap.NamePtr, "SpeakerMap.Name");

        return speakerMap;
    }

    private static ChannelMap ReadChannelMap(ref XFileReadContext context)
    {
        var channelMap = new ChannelMap
        {
            EntryCount = context.ReadInt32(),
        };

        for (var i = 0; i < channelMap.Speakers.Length; i++)
            channelMap.Speakers[i] = ReadSpeakerLevels(ref context);

        return channelMap;
    }

    private static SpeakerLevels ReadSpeakerLevels(ref XFileReadContext context)
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

    private static void ResolveLargeString(
        ref XFileReadContext context,
        ZonePointer<string> pointer,
        string label)
    {
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<string> resolvedPointer) =>
                ReadStringPointerValue(ref pointerContext, resolvedPointer, label));
    }

    private static void ReadStringPointerValue(
        ref XFileReadContext context,
        ZonePointer<string> pointer,
        string label)
    {
        var start = context.Position;
        var stream = context.GetActiveStreamAddress();

        GenericReader.ReadStringPointerValue(ref context, pointer);

        Trace(
            $"{AssetLabel(context)} {label} string src=0x{start:X8}/0x{context.Position - start:X} "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} raw=0x{pointer.Raw:X8} "
            + $"kind={pointer.Kind} value=\"{FormatTraceString(pointer.Result)}\"");
    }

    private static string AssetLabel(in XFileReadContext context)
    {
        return context.CurrentAssetIndex >= 0
            ? $"asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}]"
            : "asset[-----]";
    }

    private static string FormatTraceString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        return escaped.Length <= 96
            ? escaped
            : escaped[..96] + "...";
    }

    private static void Trace(string message)
    {
        if (!TraceSound || _traceSoundCount >= TraceSoundLimit)
            return;

        _traceSoundCount++;
        Console.Error.WriteLine($"[sound-trace] {message}");
    }

    private static bool IsTraceEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }

    private static int GetTraceLimit(string name)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value >= 0
            ? value
            : int.MaxValue;
    }
}
