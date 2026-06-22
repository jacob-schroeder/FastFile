using FastFile.Logic.Compression;
using FastFile.Logic.Database.DbFileLoad;
using FastFile.Logic.Database.Streaming;
using FastFile.Logic.Hashing;
using FastFile.Models.Database;
using FastFile.Models.Zone;

namespace FastFile.Logic.Database;

public sealed class FastFileReaderNew
{
    private const string Ps3Magic = "IWffu100";
    private const uint Ps3Version = 0x10D;
    private const int LanguageBitCount = 15;
    private const int ImageStreamEntrySize = 0x14;
    private const uint MaxHeaderEntries = 0x3800;
    private const ushort ZoneBlockTerminator = 1;

    private readonly DirectFastFileCursor _cursor;
    private readonly DbLoaderState _state;
    private readonly List<string> _warnings = new();
    private uint? _parsedFileSize;

    public FastFileReaderNew(byte[] buffer, int length, DbLoaderState? state = null)
    {
        _cursor = new DirectFastFileCursor(buffer.AsMemory(0, length));
        _state = state ?? new DbLoaderState();
    }

    public IReadOnlyList<string> Warnings => _warnings;

    public FastFileLoad LoadXFile()
    {
        DbHeader header = ReadDbHeaderPs3();
        byte[] zone = UnpackZone(header.FileSize);

        var zoneCursor = new DirectFastFileCursor(zone);
        XFile xfile = ReadXFileHeader(zoneCursor);

        return new FastFileLoad(header, xfile, zone, _state.ImageStreams, _warnings.ToArray());
    }

    public DbHeader ReadDbHeaderPs3()
    {
        var startOffset = _cursor.Offset;
        string magic = _cursor.ReadFixedString(8);
        if (magic != Ps3Magic)
            throw new InvalidDataException($"Expected PS3 fastfile magic '{Ps3Magic}' at 0x{startOffset:X}, got '{magic}'.");

        uint version = _cursor.ReadUInt32();
        if (version != Ps3Version)
            throw new InvalidDataException($"Expected PS3 fastfile version 0x{Ps3Version:X}, got 0x{version:X}.");

        bool allowOnlineUpdate = _cursor.ReadByte() switch
        {
            0 => false,
            1 => true,
            var value => throw new InvalidDataException($"Invalid allowOnlineUpdate byte {value} at 0x{_cursor.Offset - 1:X}.")
        };

        ulong fileCreationTime = _cursor.ReadUInt64();
        uint languageMask = _cursor.ReadUInt32();

        ResolveSelectedLanguage(languageMask);
        LanguageScan languageScan = ScanLanguages(languageMask, _state.SelectedLanguageMask);

        uint entryCount = _cursor.ReadUInt32();
        if (entryCount > MaxHeaderEntries)
            throw new InvalidDataException($"DB header EntryCount 0x{entryCount:X} exceeds PS3 maximum 0x{MaxHeaderEntries:X}.");

        DbHeaderImageStreamEntry[] entries = ReadSelectedLanguageEntries(languageMask, entryCount);
        _state.ResetHeaderEntries(entries);

        uint fileSize = _cursor.ReadUInt32();
        uint maxFileSize = _cursor.ReadUInt32();
        _parsedFileSize = fileSize;

        return new DbHeader(
            Magic: magic,
            Version: version,
            AllowOnlineUpdate: allowOnlineUpdate,
            FileCreationTime: fileCreationTime,
            LanguageMask: languageMask,
            SelectedLanguageMask: _state.SelectedLanguageMask,
            LanguageCount: languageScan.LanguageCount,
            SelectedLanguageIndex: languageScan.SelectedLanguageIndex,
            EntryCount: entryCount,
            ImageStreamEntries: entries,
            FileSize: fileSize,
            MaxFileSize: maxFileSize,
            PackedStreamOffset: _cursor.Offset);
    }

    public DB_Header ParseHeader()
    {
        DbHeader header = ReadDbHeaderPs3();

        return new DB_Header
        {
            Magic = header.Magic,
            Version = (XFileVersion)header.Version,
            AllowOnlineUpdate = header.AllowOnlineUpdate,
            FileCreationTime = header.FileCreationTime,
            Region = (Language)header.LanguageMask,
            EntryCount = checked((int)header.EntryCount),
            ImageStreamEntries = header.ImageStreamEntries.Select(x => new ImageStreamEntry
            {
                FileIndex = x.FileIndex,
                SourceStart = x.SourceStart,
                SourceEnd = x.SourceEnd,
                BlockOffset = x.BlockOffset,
                StreamOffset = x.StreamOffset
            }).ToArray(),
            FileSize = checked((int)header.FileSize),
            MaxFileSize = checked((int)header.MaxFileSize)
        };
    }

    public byte[] UnpackZone()
    {
        if (_parsedFileSize is not { } fileSize)
            throw new InvalidOperationException("Call ReadDbHeaderPs3() before UnpackZone(), so the packed stream starts at the correct offset.");

        return UnpackZone(fileSize);
    }

    public void DB_CreateGfxImageStream(GfxImageShell image, int imageIndex)
    {
        if (!image.HasStreamingData)
            return;

        for (int partIndex = 0; partIndex < DbLoaderState.StreamPartsPerImage; partIndex++)
        {
            DbHeaderImageStreamEntry raw = _state.TakeNextHeaderEntry();

            var record = new GfxImageStreamRecord(
                SourceStart: raw.SourceStart,
                BlockOffset: raw.BlockOffset,
                StreamOffset: raw.StreamOffset,
                SourceEnd: raw.SourceEnd,
                File: ResolveStreamFile(raw));

            _state.ImageStreams.Set(imageIndex, partIndex, record);
        }
    }

    public GfxImageStreamRecord DB_GetGfxImageStreamRecord(ushort streamIndex)
    {
        return _state.ImageStreams.GetByStreamIndex(streamIndex);
    }

    public bool DB_PrefetchStreamFile(ushort streamIndex)
    {
        GfxImageStreamRecord record = DB_GetGfxImageStreamRecord(streamIndex);
        if (!record.HasFile)
            return true;

        // The eboot passes SourceStart/SourceEnd into the file cache layer here.
        return record.SourceEnd >= record.SourceStart;
    }

    private void ResolveSelectedLanguage(uint headerLanguageMask)
    {
        if (_state.SelectedLanguageMask == 0)
        {
            _state.SelectedLanguageMask = headerLanguageMask;
            return;
        }

        if ((headerLanguageMask & _state.SelectedLanguageMask) == 0)
            throw new InvalidDataException($"Fastfile language mask 0x{headerLanguageMask:X} does not contain selected language 0x{_state.SelectedLanguageMask:X}.");
    }

    private static LanguageScan ScanLanguages(uint headerLanguageMask, uint selectedLanguageMask)
    {
        uint languageCount = 0;
        uint selectedLanguageIndex = 0;

        for (int bitIndex = 0; bitIndex < LanguageBitCount; bitIndex++)
        {
            uint bit = 1u << bitIndex;
            if ((headerLanguageMask & bit) == 0)
                continue;

            if (bit == selectedLanguageMask)
                selectedLanguageIndex = languageCount;

            languageCount++;
        }

        return new LanguageScan(languageCount, selectedLanguageIndex);
    }

    private DbHeaderImageStreamEntry[] ReadSelectedLanguageEntries(uint headerLanguageMask, uint entryCount)
    {
        var entries = new DbHeaderImageStreamEntry[entryCount];

        for (int bitIndex = 0; bitIndex < LanguageBitCount; bitIndex++)
        {
            uint bit = 1u << bitIndex;
            if ((headerLanguageMask & bit) == 0)
                continue;

            if (bit != _state.SelectedLanguageMask)
            {
                _cursor.Skip(checked((int)(entryCount * ImageStreamEntrySize)));
                continue;
            }

            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                entries[entryIndex] = ReadImageStreamEntry();
        }

        return entries;
    }

    private DbHeaderImageStreamEntry ReadImageStreamEntry()
    {
        int offset = _cursor.Offset;
        var entry = new DbHeaderImageStreamEntry(
            FileIndex: _cursor.ReadUInt32(),
            SourceStart: _cursor.ReadUInt32(),
            SourceEnd: _cursor.ReadUInt32(),
            BlockOffset: _cursor.ReadUInt32(),
            StreamOffset: _cursor.ReadUInt32(),
            SerializedOffset: offset);

        if (entry.SourceEnd != 0 && entry.SourceEnd < entry.SourceStart)
            _warnings.Add($"Image stream entry at 0x{offset:X} has SourceEnd before SourceStart.");

        if ((entry.StreamOffset & 0xffff) != entry.BlockOffset)
            _warnings.Add($"Image stream entry at 0x{offset:X} has StreamOffset low16 0x{entry.StreamOffset & 0xffff:X} != BlockOffset 0x{entry.BlockOffset:X}.");

        return entry;
    }

    private byte[] UnpackZone(uint fileSize)
    {
        if (fileSize > int.MaxValue)
            throw new InvalidDataException($"FileSize 0x{fileSize:X} does not fit in this reader.");

        using var output = new MemoryStream();
        int packedEnd = checked((int)fileSize);

        while (_cursor.Offset < packedEnd)
        {
            ushort blockSize = _cursor.ReadUInt16();

            if (blockSize == ZoneBlockTerminator)
            {
                TryConsumeTrailingTerminatorWord();
                break;
            }

            if (blockSize == 0)
            {
                _warnings.Add($"Encountered invalid compressed block size 0 at 0x{_cursor.Offset - 2:X}.");
                _cursor.Skip(sizeof(ushort));
                continue;
            }

            int compressedSize = blockSize - sizeof(uint);
            byte[] compressed = _cursor.ReadBytes(compressedSize);
            uint expectedAdler32 = _cursor.ReadUInt32();

            byte[] decompressed = Deflate.Decompress(compressed);
            output.Write(decompressed);

            uint actualAdler32 = Adler32.HashToUInt32(decompressed);
            if (actualAdler32 != expectedAdler32)
                _warnings.Add($"Checksum mismatch at compressed block ending 0x{_cursor.Offset:X}. Expected {expectedAdler32:X8}, got {actualAdler32:X8}.");
        }

        return output.ToArray();
    }

    private void TryConsumeTrailingTerminatorWord()
    {
        if (_cursor.Remaining < sizeof(ushort))
            return;

        if (_cursor.PeekUInt16() == ZoneBlockTerminator)
            _cursor.Skip(sizeof(ushort));
    }

    private static XFile ReadXFileHeader(DirectFastFileCursor zoneCursor)
    {
        var header = new XFile
        {
            Size = zoneCursor.ReadInt32(),
            ExternalSize = zoneCursor.ReadInt32(),
            BlockSize = new int[(int)XFileBlockType.COUNT]
        };

        for (int i = 0; i < header.BlockSize.Length; i++)
            header.BlockSize[i] = zoneCursor.ReadInt32();

        return header;
    }

    private StreamFileRef? ResolveStreamFile(DbHeaderImageStreamEntry entry)
    {
        if (entry.SourceEnd == 0)
            return null;

        if (entry.FileIndex == 0)
            return _state.CurrentFastFile;

        if (entry.FileIndex > DbLoaderState.MaxImageFileIndex)
            throw new InvalidDataException($"Unexpected PS3 imagefile index {entry.FileIndex}.");

        return _state.GetOrCreateImageFile(entry.FileIndex);
    }

    private readonly record struct LanguageScan(uint LanguageCount, uint SelectedLanguageIndex);
}
