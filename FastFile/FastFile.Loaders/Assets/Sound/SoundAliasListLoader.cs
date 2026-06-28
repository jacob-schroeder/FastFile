using System.Buffers.Binary;
using FastFile.Models.Assets.Sound;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Sound;

public sealed class SoundAliasListLoader
{
    public SoundAliasListAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level Sound pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            SoundAliasListAsset sound = ReadSoundAliasList(cursor, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return sound;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static SoundAliasListAsset ReadSoundAliasList(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, SoundAliasListAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XString aliasNamePointer = ReadXStringPointer(rootCursor, context);
        XPointer<SndAlias[]> aliasesPointer = ReadPointer<SndAlias[]>(rootCursor, context, XPointerResolutionMode.Direct);
        int count = rootCursor.ReadInt32();

        if (rootCursor.Offset != SoundAliasListAsset.SerializedSize)
            throw new InvalidDataException($"snd_alias_list_t consumed 0x{rootCursor.Offset:X} bytes instead of 0x{SoundAliasListAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  Sound root source=0x{sourceOffset:X} name=0x{aliasNamePointer.Raw:X8} aliases=0x{aliasesPointer.Raw:X8} " +
            $"count={count} blocks={context.Blocks.DescribePositions()}");

        string? aliasName;
        IReadOnlyList<SndAlias> aliases;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            aliasName = LoadSoundXString(cursor, aliasNamePointer, context);
            aliases = ReadAliasArray(cursor, aliasesPointer.Untyped, count, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new SoundAliasListAsset
        {
            Offset = sourceOffset,
            AliasNamePointer = aliasNamePointer,
            AliasName = aliasName,
            AliasesPointer = aliasesPointer,
            Count = count,
            Aliases = aliases
        };
    }

    private static IReadOnlyList<SndAlias> ReadAliasArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative snd_alias_t count {count}.");

        int byteCount = checked(count * SndAlias.SerializedSize);
        if (pointer.Type == PointerType.Null || count == 0)
            return [];

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "snd_alias_t[]");
            return [];
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return [];

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress aliasesAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] aliasBytes = context.Blocks.Load(cursor, byteCount);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(aliasesAddress));

        var aliasCursor = new FastFileCursor(aliasBytes, aliasesAddress);
        var roots = new SndAliasRoot[count];
        for (int i = 0; i < roots.Length; i++)
            roots[i] = ReadAliasRoot(aliasCursor, context);

        var aliases = new SndAlias[count];
        for (int i = 0; i < aliases.Length; i++)
            aliases[i] = ReadAliasChildren(cursor, roots[i], context);

        context.Diagnostics.Trace(
            $"    Sound aliases sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} count={count} target={aliasesAddress} " +
            $"blocks={context.Blocks.DescribePositions()}");

        return aliases;
    }

    private static SndAliasRoot ReadAliasRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.AddressAt(cursor.Offset)?.Offset ?? cursor.Offset;
        int start = cursor.Offset;
        var root = new SndAliasRoot(
            offset,
            ReadXStringPointer(cursor, context),
            ReadXStringPointer(cursor, context),
            ReadXStringPointer(cursor, context),
            ReadXStringPointer(cursor, context),
            ReadXStringPointer(cursor, context),
            ReadPointerCellNoValidation<SoundFile[]>(cursor, XPointerResolutionMode.Direct),
            cursor.ReadInt32(),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            cursor.ReadInt32(),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            cursor.ReadInt32(),
            ReadPointerCellNoValidation<SndCurve>(cursor, XPointerResolutionMode.AliasCell),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadPointerCellNoValidation<SpeakerMap>(cursor, XPointerResolutionMode.Direct));

        if (cursor.Offset - start != SndAlias.SerializedSize)
            throw new InvalidDataException($"snd_alias_t consumed 0x{cursor.Offset - start:X} bytes instead of 0x{SndAlias.SerializedSize:X}.");

        return root;
    }

    private static SndAlias ReadAliasChildren(
        FastFileCursor cursor,
        SndAliasRoot root,
        FastFileLoadContext context)
    {
        string? aliasName = LoadSoundXString(cursor, root.AliasNamePointer, context);
        string? subtitle = LoadSoundXString(cursor, root.SubtitlePointer, context);
        string? secondaryAliasName = LoadSoundXString(cursor, root.SecondaryAliasNamePointer, context);
        string? chainAliasName = LoadSoundXString(cursor, root.ChainAliasNamePointer, context);
        string? mixerGroup = LoadSoundXString(cursor, root.MixerGroupPointer, context);

        IReadOnlyList<SoundFile> soundFiles;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            soundFiles = ReadSoundFileArray(cursor, root.SoundFilesPointer.Untyped, soundFileCount: 1, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        SndCurve? volumeFalloffCurve = ReadSndCurvePointer(cursor, root.VolumeFalloffCurvePointer.Untyped, context);
        SpeakerMap? speakerMap = ReadSpeakerMapPointer(cursor, root.SpeakerMapPointer.Untyped, context);

        return new SndAlias
        {
            Offset = root.Offset,
            AliasNamePointer = root.AliasNamePointer,
            AliasName = aliasName,
            SubtitlePointer = root.SubtitlePointer,
            Subtitle = subtitle,
            SecondaryAliasNamePointer = root.SecondaryAliasNamePointer,
            SecondaryAliasName = secondaryAliasName,
            ChainAliasNamePointer = root.ChainAliasNamePointer,
            ChainAliasName = chainAliasName,
            MixerGroupPointer = root.MixerGroupPointer,
            MixerGroup = mixerGroup,
            SoundFilesPointer = root.SoundFilesPointer,
            SoundFileCount = 1,
            SoundFiles = soundFiles,
            Sequence = root.Sequence,
            VolumeMin = root.VolumeMin,
            VolumeMax = root.VolumeMax,
            PitchMin = root.PitchMin,
            PitchMax = root.PitchMax,
            DistanceMin = root.DistanceMin,
            DistanceMax = root.DistanceMax,
            VelocityMin = root.VelocityMin,
            Flags = root.Flags,
            SlavePercentage = root.SlavePercentage,
            Probability = root.Probability,
            LfePercentage = root.LfePercentage,
            CenterPercentage = root.CenterPercentage,
            StartDelay = root.StartDelay,
            VolumeFalloffCurvePointer = root.VolumeFalloffCurvePointer,
            VolumeFalloffCurve = volumeFalloffCurve,
            EnvelopMin = root.EnvelopMin,
            EnvelopMax = root.EnvelopMax,
            EnvelopPercentage = root.EnvelopPercentage,
            SpeakerMapPointer = root.SpeakerMapPointer,
            SpeakerMap = speakerMap
        };
    }

    private static IReadOnlyList<SoundFile> ReadSoundFileArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int soundFileCount,
        FastFileLoadContext context)
    {
        if (soundFileCount < 0)
            throw new InvalidDataException($"Invalid negative SoundFile count {soundFileCount}.");

        int byteCount = checked(soundFileCount * SoundFile.SerializedSize);
        if (pointer.Type == PointerType.Null || soundFileCount == 0)
            return [];

        if (pointer.Type == PointerType.Offset)
        {
            if (pointer.PackedAddress == context.Blocks.CurrentAddress)
                return ReadInlineSoundFileArray(cursor, soundFileCount, context);

            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "SoundFile[]");
            return [];
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return [];

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress soundFilesAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        return ReadInlineSoundFileArray(cursor, soundFileCount, context, soundFilesAddress, insertCell);
    }

    private static IReadOnlyList<SoundFile> ReadInlineSoundFileArray(
        FastFileCursor cursor,
        int soundFileCount,
        FastFileLoadContext context,
        XBlockAddress? soundFilesAddressOverride = null,
        XBlockAddress? insertCell = null)
    {
        int byteCount = checked(soundFileCount * SoundFile.SerializedSize);
        XBlockAddress soundFilesAddress = soundFilesAddressOverride ?? context.Blocks.CurrentAddress;
        byte[] soundFileBytes = context.Blocks.Load(cursor, byteCount);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(soundFilesAddress));

        var soundFileCursor = new FastFileCursor(soundFileBytes, soundFilesAddress);
        var roots = new SoundFileRoot[soundFileCount];
        for (int i = 0; i < roots.Length; i++)
            roots[i] = ReadSoundFileRoot(soundFileCursor);

        var soundFiles = new SoundFile[soundFileCount];
        for (int i = 0; i < soundFiles.Length; i++)
            soundFiles[i] = ReadSoundFileChildren(cursor, roots[i], context);

        return soundFiles;
    }

    private static SoundFileRoot ReadSoundFileRoot(FastFileCursor cursor)
    {
        int offset = cursor.AddressAt(cursor.Offset)?.Offset ?? cursor.Offset;
        int start = cursor.Offset;
        var type = (SndAliasType)cursor.ReadByte();
        byte exists = cursor.ReadByte();
        ushort padding = cursor.ReadUInt16();
        int unionCellOffset = cursor.Offset;
        byte[] unionData = cursor.ReadBytes(12);
        int unionRaw0 = BinaryPrimitives.ReadInt32BigEndian(unionData.AsSpan(0, sizeof(int)));
        int unionRaw1 = BinaryPrimitives.ReadInt32BigEndian(unionData.AsSpan(4, sizeof(int)));
        int unionRaw2 = BinaryPrimitives.ReadInt32BigEndian(unionData.AsSpan(8, sizeof(int)));

        if (cursor.Offset - start != SoundFile.SerializedSize)
            throw new InvalidDataException($"SoundFile consumed 0x{cursor.Offset - start:X} bytes instead of 0x{SoundFile.SerializedSize:X}.");

        return new SoundFileRoot(
            offset,
            type,
            exists,
            padding,
            unionData,
            unionRaw0,
            unionRaw1,
            unionRaw2,
            cursor.AddressAt(unionCellOffset) ?? throw new InvalidDataException("SoundFile union cell has no runtime destination address."),
            cursor.AddressAt(unionCellOffset + sizeof(int)) ?? throw new InvalidDataException("StreamedSound directory cell has no runtime destination address."),
            cursor.AddressAt(unionCellOffset + (sizeof(int) * 2)) ?? throw new InvalidDataException("StreamedSound filename cell has no runtime destination address."));
    }

    private static SoundFile ReadSoundFileChildren(
        FastFileCursor cursor,
        SoundFileRoot root,
        FastFileLoadContext context)
    {
        XPointer<LoadedSound>? loadedSoundPointer = null;
        LoadedSound? loadedSound = null;
        StreamedSound? streamedSound = null;

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            if (root.Type == SndAliasType.Loaded)
            {
                loadedSoundPointer = context.PointerReader.FromRaw<LoadedSound>(
                    root.UnionRaw0,
                    XPointerResolutionMode.AliasCell,
                    root.UnionCellAddress);
                loadedSound = ReadLoadedSoundPointer(cursor, loadedSoundPointer.Value.Untyped, context);
            }
            else
            {
                streamedSound = ReadStreamedSound(cursor, root, context);
            }
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new SoundFile
        {
            Offset = root.Offset,
            Type = root.Type,
            Exists = root.Exists,
            Padding = root.Padding,
            UnionData = root.UnionData,
            UnionRaw0 = root.UnionRaw0,
            UnionRaw1 = root.UnionRaw1,
            UnionRaw2 = root.UnionRaw2,
            LoadedSoundPointer = loadedSoundPointer,
            LoadedSound = loadedSound,
            StreamedSound = streamedSound
        };
    }

    private static StreamedSound ReadStreamedSound(
        FastFileCursor cursor,
        SoundFileRoot root,
        FastFileLoadContext context)
    {
        uint fileIndex = unchecked((uint)root.UnionRaw0);
        XString? directoryPointer = null;
        XString? filenamePointer = null;
        string? directory = null;
        string? filename = null;

        if (fileIndex == 0)
        {
            directoryPointer = new XString(
                root.UnionRaw1,
                XPointerResolutionMode.Direct,
                root.StreamedDirectoryCellAddress);
            filenamePointer = new XString(
                root.UnionRaw2,
                XPointerResolutionMode.Direct,
                root.StreamedFilenameCellAddress);
            directory = LoadSoundXString(cursor, directoryPointer.Value, context);
            filename = LoadSoundXString(cursor, filenamePointer.Value, context);
        }

        return new StreamedSound
        {
            FileIndex = fileIndex,
            RawOffsetOrDirectoryPointer = root.UnionRaw1,
            RawLengthOrFilenamePointer = root.UnionRaw2,
            DirectoryPointer = directoryPointer,
            Directory = directory,
            FilenamePointer = filenamePointer,
            Filename = filename
        };
    }

    private static LoadedSound? ReadLoadedSoundPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointer<LoadedSound>(pointer);
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress loadedSoundAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            LoadedSound loadedSound = ReadLoadedSound(cursor, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(loadedSoundAddress));

            return loadedSound;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static LoadedSound ReadLoadedSound(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, LoadedSound.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XString namePointer = ReadXStringPointer(rootCursor, context);
        int physicalDataByteCount = rootCursor.ReadInt32();
        byte[] soundInfoBytes = rootCursor.ReadBytes(10);
        ushort seekTableCount = rootCursor.ReadUInt16();
        XPointer<byte[]> seekTablePointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);
        XPointer<byte[]> physicalDataPointer = ReadPointer<byte[]>(rootCursor, context, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != LoadedSound.SerializedSize)
            throw new InvalidDataException($"LoadedSound consumed 0x{rootCursor.Offset:X} bytes instead of 0x{LoadedSound.SerializedSize:X}.");

        string? name;
        byte[]? seekTable;
        byte[]? physicalData;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = LoadSoundXString(cursor, namePointer, context);
            seekTable = ReadByteArrayPointer(cursor, seekTablePointer.Untyped, checked(seekTableCount * sizeof(uint)), "LoadedSound.seekTable", 4, context);

            context.Blocks.Push(XFileBlockType.PHYSICAL);
            try
            {
                physicalData = ReadByteArrayPointer(cursor, physicalDataPointer.Untyped, physicalDataByteCount, "LoadedSound.physicalData", 64, context);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"      LoadedSound source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} physicalBytes={physicalDataByteCount} " +
            $"seekCount={seekTableCount} blocks={context.Blocks.DescribePositions()}");

        return new LoadedSound
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            PhysicalDataByteCount = physicalDataByteCount,
            SoundInfoBytes = soundInfoBytes,
            SeekTableCount = seekTableCount,
            SeekTablePointer = seekTablePointer,
            SeekTable = seekTable,
            PhysicalDataPointer = physicalDataPointer,
            PhysicalData = physicalData
        };
    }

    private static SndCurve? ReadSndCurvePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointer<SndCurve>(pointer);
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress curveAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            SndCurve curve = ReadSndCurve(cursor, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(curveAddress));

            return curve;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static SndCurve ReadSndCurve(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, SndCurve.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XString filenamePointer = ReadXStringPointer(rootCursor, context);
        ushort knotCount = rootCursor.ReadUInt16();
        ushort padding = rootCursor.ReadUInt16();
        byte[] knotBytes = rootCursor.ReadBytes(SndCurve.KnotBytesSize);

        if (rootCursor.Offset != SndCurve.SerializedSize)
            throw new InvalidDataException($"SndCurve consumed 0x{rootCursor.Offset:X} bytes instead of 0x{SndCurve.SerializedSize:X}.");

        string? filename;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            filename = LoadSoundXString(cursor, filenamePointer, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new SndCurve
        {
            Offset = sourceOffset,
            FilenamePointer = filenamePointer,
            Filename = filename,
            KnotCount = knotCount,
            Padding = padding,
            KnotBytes = knotBytes
        };
    }

    private static SpeakerMap? ReadSpeakerMapPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            if (pointer.PackedAddress == context.Blocks.CurrentAddress)
                return ReadSpeakerMap(cursor, context.Blocks.CurrentAddress, context);

            context.PointerReader.ValidateOffsetPointerRange(pointer, SpeakerMap.SerializedSize, "SpeakerMap");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress speakerMapAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        SpeakerMap speakerMap = ReadSpeakerMap(cursor, speakerMapAddress, context);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(speakerMapAddress));

        return speakerMap;
    }

    private static SpeakerMap ReadSpeakerMap(
        FastFileCursor cursor,
        XBlockAddress speakerMapAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, SpeakerMap.SerializedSize);
        var rootCursor = new FastFileCursor(rootBytes, speakerMapAddress);

        byte isDefault = rootCursor.ReadByte();
        byte[] padding = rootCursor.ReadBytes(3);
        XString namePointer = ReadXStringPointer(rootCursor, context);
        IReadOnlyList<SpeakerMapChannel> channels = ReadSpeakerMapChannels(rootCursor);

        if (rootCursor.Offset != SpeakerMap.SerializedSize)
            throw new InvalidDataException($"SpeakerMap consumed 0x{rootCursor.Offset:X} bytes instead of 0x{SpeakerMap.SerializedSize:X}.");

        string? name = LoadSoundXString(cursor, namePointer, context);

        context.Diagnostics.Trace(
            $"      SpeakerMap source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} isDefault={isDefault} " +
            $"target={speakerMapAddress} blocks={context.Blocks.DescribePositions()}");

        return new SpeakerMap
        {
            Offset = speakerMapAddress.Offset,
            IsDefault = isDefault,
            Padding = padding,
            NamePointer = namePointer,
            Name = name,
            Channels = channels
        };
    }

    private static IReadOnlyList<SpeakerMapChannel> ReadSpeakerMapChannels(FastFileCursor cursor)
    {
        var channels = new SpeakerMapChannel[2];
        for (int i = 0; i < channels.Length; i++)
        {
            var outputs = new XAudioChannelMap[2];
            for (int outputIndex = 0; outputIndex < outputs.Length; outputIndex++)
            {
                int entryCount = cursor.ReadInt32();
                var speakers = new SpeakerLevels[6];
                for (int speakerIndex = 0; speakerIndex < speakers.Length; speakerIndex++)
                {
                    speakers[speakerIndex] = new SpeakerLevels
                    {
                        Speaker = cursor.ReadInt32(),
                        NumLevels = cursor.ReadInt32(),
                        Level0 = ReadSingle(cursor),
                        Level1 = ReadSingle(cursor)
                    };
                }

                outputs[outputIndex] = new XAudioChannelMap
                {
                    EntryCount = entryCount,
                    Speakers = speakers
                };
            }

            channels[i] = new SpeakerMapChannel
            {
                Outputs = outputs
            };
        }

        return channels;
    }

    private static byte[]? ReadByteArrayPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        string targetName,
        int alignment,
        FastFileLoadContext context)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"{targetName} has invalid negative byte count {byteCount}.");

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            if (pointer.PackedAddress == context.Blocks.CurrentAddress)
                return context.Blocks.Load(cursor, byteCount);

            if (byteCount > 0)
                context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, targetName);
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        byte[] bytes = context.Blocks.Load(cursor, byteCount);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(targetAddress));

        return bytes;
    }

    private static XString ReadXStringPointer(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int cellOffset = cursor.Offset;
        return new XString(
            cursor.ReadInt32(),
            XPointerResolutionMode.Direct,
            cursor.AddressAt(cellOffset));
    }

    private static string? LoadSoundXString(
        FastFileCursor cursor,
        XString pointer,
        FastFileLoadContext context)
    {
        XPointerReference untyped = pointer.Untyped;
        if (untyped.Type == PointerType.Null)
            return null;

        if (untyped.Type == PointerType.Offset)
        {
            if (untyped.PackedAddress == context.Blocks.CurrentAddress)
                return context.Blocks.LoadCString(cursor);

            return context.PointerReader.LoadXString(cursor, pointer);
        }

        if (untyped.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = untyped.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(untyped, alignment: 0);
        string value = context.Blocks.LoadCString(cursor);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(targetAddress));

        return value;
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        FastFileLoadContext context,
        XPointerResolutionMode resolutionMode)
    {
        return context.PointerReader.ReadPointer<T>(cursor, resolutionMode);
    }

    private static XPointer<T> ReadPointerCellNoValidation<T>(
        FastFileCursor cursor,
        XPointerResolutionMode resolutionMode)
    {
        int cellOffset = cursor.Offset;
        return new XPointer<T>(
            cursor.ReadInt32(),
            resolutionMode,
            cursor.AddressAt(cellOffset));
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private sealed record SndAliasRoot(
        int Offset,
        XString AliasNamePointer,
        XString SubtitlePointer,
        XString SecondaryAliasNamePointer,
        XString ChainAliasNamePointer,
        XString MixerGroupPointer,
        XPointer<SoundFile[]> SoundFilesPointer,
        int Sequence,
        float VolumeMin,
        float VolumeMax,
        float PitchMin,
        float PitchMax,
        float DistanceMin,
        float DistanceMax,
        float VelocityMin,
        int Flags,
        float SlavePercentage,
        float Probability,
        float LfePercentage,
        float CenterPercentage,
        int StartDelay,
        XPointer<SndCurve> VolumeFalloffCurvePointer,
        float EnvelopMin,
        float EnvelopMax,
        float EnvelopPercentage,
        XPointer<SpeakerMap> SpeakerMapPointer);

    private sealed record SoundFileRoot(
        int Offset,
        SndAliasType Type,
        byte Exists,
        ushort Padding,
        byte[] UnionData,
        int UnionRaw0,
        int UnionRaw1,
        int UnionRaw2,
        XBlockAddress UnionCellAddress,
        XBlockAddress StreamedDirectoryCellAddress,
        XBlockAddress StreamedFilenameCellAddress);
}
