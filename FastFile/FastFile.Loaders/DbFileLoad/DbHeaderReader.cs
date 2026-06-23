using FastFile.Models.Database;
using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Database.Streaming;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.DbFileLoad;

public sealed class DbHeaderReader
{
    private const string Ps3Magic = "IWffu100";
    private const int LanguageBitCount = 15;
    private const int ImageStreamEntrySize = 0x14;
    private const uint MaxHeaderEntries = 0x3800;

    public DbHeader Read(FastFileCursor cursor, FastFileLoadContext context)
    {
        int startOffset = cursor.Offset;
        string magic = cursor.ReadFixedString(8);
        if (magic != Ps3Magic)
            throw new InvalidDataException($"Expected PS3 fastfile magic '{Ps3Magic}' at 0x{startOffset:X}, got '{magic}'.");

        XFileVersion version = (XFileVersion)cursor.ReadUInt32();

        bool allowOnlineUpdate = cursor.ReadByte() switch
        {
            0 => false,
            1 => true,
            var value => throw new InvalidDataException($"Invalid allowOnlineUpdate byte {value} at 0x{cursor.Offset - 1:X}.")
        };

        DateTime fileCreationTime = DateTime.FromFileTimeUtc((long)cursor.ReadUInt64());
        uint languageMask = cursor.ReadUInt32();

        ResolveSelectedLanguage(languageMask, context);
        LanguageScan languageScan = ScanLanguages(languageMask, context.SelectedLanguageMask);

        uint entryCount = cursor.ReadUInt32();
        if (entryCount > MaxHeaderEntries)
            throw new InvalidDataException($"DB header EntryCount 0x{entryCount:X} exceeds PS3 maximum 0x{MaxHeaderEntries:X}.");

        DbHeaderImageStreamEntry[] entries = ReadSelectedLanguageEntries(cursor, context.SelectedLanguageMask, languageMask, entryCount, context);

        uint fileSize = cursor.ReadUInt32();
        uint maxFileSize = cursor.ReadUInt32();

        var header = new DbHeader(
            Magic: magic,
            Version: version,
            AllowOnlineUpdate: allowOnlineUpdate,
            FileCreationTime: fileCreationTime,
            LanguageMask: languageMask,
            SelectedLanguageMask: context.SelectedLanguageMask,
            LanguageCount: languageScan.LanguageCount,
            SelectedLanguageIndex: languageScan.SelectedLanguageIndex,
            EntryCount: entryCount,
            ImageStreamEntries: entries,
            FileSize: fileSize,
            MaxFileSize: maxFileSize,
            PackedStreamOffset: cursor.Offset);

        context.Header = header;
        return header;
    }

    private static void ResolveSelectedLanguage(uint headerLanguageMask, FastFileLoadContext context)
    {
        if (context.SelectedLanguageMask == 0)
        {
            context.SelectedLanguageMask = headerLanguageMask;
            return;
        }

        if ((headerLanguageMask & context.SelectedLanguageMask) == 0)
            throw new InvalidDataException($"Fastfile language mask 0x{headerLanguageMask:X} does not contain selected language 0x{context.SelectedLanguageMask:X}.");
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

    private static DbHeaderImageStreamEntry[] ReadSelectedLanguageEntries(
        FastFileCursor cursor,
        uint selectedLanguageMask,
        uint headerLanguageMask,
        uint entryCount,
        FastFileLoadContext context)
    {
        var entries = new DbHeaderImageStreamEntry[entryCount];

        for (int bitIndex = 0; bitIndex < LanguageBitCount; bitIndex++)
        {
            uint bit = 1u << bitIndex;
            if ((headerLanguageMask & bit) == 0)
                continue;

            if (bit != selectedLanguageMask)
            {
                cursor.Skip(checked((int)(entryCount * ImageStreamEntrySize)));
                continue;
            }

            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                entries[entryIndex] = ReadImageStreamEntry(cursor, context);
        }

        return entries;
    }

    private static DbHeaderImageStreamEntry ReadImageStreamEntry(FastFileCursor cursor, FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        var entry = new DbHeaderImageStreamEntry(
            FileIndex: cursor.ReadUInt32(),
            SourceStart: cursor.ReadUInt32(),
            SourceEnd: cursor.ReadUInt32(),
            BlockOffset: cursor.ReadUInt32(),
            StreamOffset: cursor.ReadUInt32(),
            SerializedOffset: offset);

        if (entry.SourceEnd != 0 && entry.SourceEnd < entry.SourceStart)
            context.Diagnostics.Warn($"Image stream entry at 0x{offset:X} has SourceEnd before SourceStart.");

        if ((entry.StreamOffset & 0xffff) != entry.BlockOffset)
            context.Diagnostics.Warn($"Image stream entry at 0x{offset:X} has StreamOffset low16 0x{entry.StreamOffset & 0xffff:X} != BlockOffset 0x{entry.BlockOffset:X}.");

        return entry;
    }

    private readonly record struct LanguageScan(uint LanguageCount, uint SelectedLanguageIndex);
}
