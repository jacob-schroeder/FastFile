using System.Globalization;
using FastFile.Logic.Database;
using FastFile.Logic.Database.DbFileLoad;
using FastFile.Logic.Database.Streaming;

namespace Scratch;

class Program
{
    const string scratchRoot = "/Users/jacob/Repositories/FastFile/FastFile/Scratch";
    const string root = "/Users/jacob/Repositories/FastFile/Data/official_ff";
    const string patch_mp = $"{root}/patch_mp.ff";
    const string common_mp = $"{root}/common_mp.ff";
    const string mp_boneyard_load = $"{root}/mp_boneyard_load.ff";
    
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : common_mp;
        DebugGfxImageStreamEntries(path);
    }

    static void DebugGfxImageStreamEntries(string path)
    {
        byte[] buffer = File.ReadAllBytes(path);
        int length = buffer.Length;

        var ffReader = new FastFileReaderNew(buffer, length);
        var header = ffReader.ReadDbHeaderPs3();

        GfxImageEntryDebugRow[] rows = BuildGfxImageRows(header.ImageStreamEntries);
        GfxImageEntrySummary summary = Summarize(rows);

        string outputDirectory = Path.Combine(scratchRoot, "debug-output");
        Directory.CreateDirectory(outputDirectory);

        string fastFileName = Path.GetFileNameWithoutExtension(path);
        string csvPath = Path.Combine(outputDirectory, $"{fastFileName}.gfx-image-stream-entries.csv");
        string textPath = Path.Combine(outputDirectory, $"{fastFileName}.gfx-image-stream-summary.txt");

        WriteCsv(csvPath, rows);
        WriteSummary(textPath, path, header, summary, rows);

        Console.WriteLine($"Fastfile: {path}");
        Console.WriteLine($"Magic/version: {header.Magic} / 0x{header.Version:X}");
        Console.WriteLine($"Language mask: 0x{header.LanguageMask:X8}, selected: 0x{header.SelectedLanguageMask:X8}, selected ordinal: {header.SelectedLanguageIndex}");
        Console.WriteLine($"EntryCount: {header.EntryCount:N0} rows = {summary.ImageCount:N0} GfxImage stream groups x4");
        Console.WriteLine($"Packed stream offset: 0x{header.PackedStreamOffset:X}, FileSize: 0x{header.FileSize:X}, MaxFileSize: 0x{header.MaxFileSize:X}");
        Console.WriteLine($"Non-empty rows: {summary.NonEmptyRowCount:N0}; empty rows: {summary.EmptyRowCount:N0}");
        Console.WriteLine($"Rows by file: {FormatFileCounts(summary.RowsByFileIndex)}");
        Console.WriteLine($"Warnings: {ffReader.Warnings.Count:N0}");
        Console.WriteLine();
        Console.WriteLine("First 12 non-empty rows:");

        foreach (GfxImageEntryDebugRow row in rows.Where(x => !x.IsEmpty).Take(12))
        {
            Console.WriteLine(
                $"image={row.ImageIndex,4} part={row.PartIndex} entry={row.EntryIndex,4} " +
                $"file={row.FileName,-10} src=[0x{row.SourceStart:X8},0x{row.SourceEnd:X8}) " +
                $"size=0x{row.SourceSize:X6} block=0x{row.BlockOffset:X4} stream=0x{row.StreamOffset:X8}");
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote CSV:     {csvPath}");
        Console.WriteLine($"Wrote summary: {textPath}");
    }

    static GfxImageEntryDebugRow[] BuildGfxImageRows(IReadOnlyList<DbHeaderImageStreamEntry> entries)
    {
        var rows = new GfxImageEntryDebugRow[entries.Count];

        for (int i = 0; i < entries.Count; i++)
        {
            DbHeaderImageStreamEntry entry = entries[i];
            int imageIndex = i / DbLoaderState.StreamPartsPerImage;
            int partIndex = i % DbLoaderState.StreamPartsPerImage;

            rows[i] = new GfxImageEntryDebugRow(
                ImageIndex: imageIndex,
                PartIndex: partIndex,
                EntryIndex: i,
                SerializedOffset: entry.SerializedOffset,
                FileIndex: entry.FileIndex,
                FileName: GetFileName(entry),
                SourceStart: entry.SourceStart,
                SourceEnd: entry.SourceEnd,
                SourceSize: entry.IsEmpty ? 0 : entry.SourceSize,
                BlockOffset: entry.BlockOffset,
                StreamOffset: entry.StreamOffset,
                StreamBlockBase: entry.StreamBlockBase,
                StreamLow16MatchesBlockOffset: (entry.StreamOffset & 0xffff) == entry.BlockOffset,
                IsSourceRangeValid: entry.SourceEnd >= entry.SourceStart,
                IsEmpty: entry.IsEmpty);
        }

        return rows;
    }

    static string GetFileName(DbHeaderImageStreamEntry entry)
    {
        if (entry.IsEmpty)
            return "<none>";

        return entry.FileIndex == 0
            ? "<fastfile>"
            : $"imagefile{entry.FileIndex}";
    }

    static GfxImageEntrySummary Summarize(IReadOnlyList<GfxImageEntryDebugRow> rows)
    {
        return new GfxImageEntrySummary(
            ImageCount: (rows.Count + DbLoaderState.StreamPartsPerImage - 1) / DbLoaderState.StreamPartsPerImage,
            NonEmptyRowCount: rows.Count(x => !x.IsEmpty),
            EmptyRowCount: rows.Count(x => x.IsEmpty),
            InvalidSourceRanges: rows.Count(x => !x.IsEmpty && !x.IsSourceRangeValid),
            InvalidStreamLow16: rows.Count(x => !x.IsEmpty && !x.StreamLow16MatchesBlockOffset),
            RowsByFileIndex: rows
                .Where(x => !x.IsEmpty)
                .GroupBy(x => x.FileIndex)
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Count()));
    }

    static void WriteCsv(string path, IReadOnlyList<GfxImageEntryDebugRow> rows)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(',',
            "imageIndex",
            "partIndex",
            "entryIndex",
            "serializedOffsetHex",
            "fileIndex",
            "fileName",
            "sourceStartHex",
            "sourceEndHex",
            "sourceSizeHex",
            "blockOffsetHex",
            "streamOffsetHex",
            "streamBlockBaseHex",
            "streamLow16MatchesBlockOffset",
            "isSourceRangeValid",
            "isEmpty"));

        foreach (GfxImageEntryDebugRow row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.ImageIndex.ToString(CultureInfo.InvariantCulture),
                row.PartIndex.ToString(CultureInfo.InvariantCulture),
                row.EntryIndex.ToString(CultureInfo.InvariantCulture),
                Hex(row.SerializedOffset),
                row.FileIndex.ToString(CultureInfo.InvariantCulture),
                Csv(row.FileName),
                Hex(row.SourceStart),
                Hex(row.SourceEnd),
                Hex(row.SourceSize),
                Hex(row.BlockOffset),
                Hex(row.StreamOffset),
                Hex(row.StreamBlockBase),
                row.StreamLow16MatchesBlockOffset,
                row.IsSourceRangeValid,
                row.IsEmpty));
        }
    }

    static void WriteSummary(
        string path,
        string fastFilePath,
        DbHeader header,
        GfxImageEntrySummary summary,
        IReadOnlyList<GfxImageEntryDebugRow> rows)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine($"Fastfile: {fastFilePath}");
        writer.WriteLine($"Magic: {header.Magic}");
        writer.WriteLine($"Version: 0x{header.Version:X}");
        writer.WriteLine($"LanguageMask: 0x{header.LanguageMask:X8}");
        writer.WriteLine($"SelectedLanguageMask: 0x{header.SelectedLanguageMask:X8}");
        writer.WriteLine($"SelectedLanguageIndex: {header.SelectedLanguageIndex}");
        writer.WriteLine($"EntryCount: {header.EntryCount}");
        writer.WriteLine($"ImageGroups: {summary.ImageCount}");
        writer.WriteLine($"PackedStreamOffset: 0x{header.PackedStreamOffset:X}");
        writer.WriteLine($"FileSize: 0x{header.FileSize:X}");
        writer.WriteLine($"MaxFileSize: 0x{header.MaxFileSize:X}");
        writer.WriteLine($"NonEmptyRows: {summary.NonEmptyRowCount}");
        writer.WriteLine($"EmptyRows: {summary.EmptyRowCount}");
        writer.WriteLine($"InvalidSourceRanges: {summary.InvalidSourceRanges}");
        writer.WriteLine($"InvalidStreamLow16: {summary.InvalidStreamLow16}");
        writer.WriteLine($"RowsByFileIndex: {FormatFileCounts(summary.RowsByFileIndex)}");
        writer.WriteLine();
        writer.WriteLine("First 32 rows:");

        foreach (GfxImageEntryDebugRow row in rows.Take(32))
        {
            writer.WriteLine(
                $"image={row.ImageIndex} part={row.PartIndex} entry={row.EntryIndex} offset=0x{row.SerializedOffset:X} " +
                $"file={row.FileName} source=[0x{row.SourceStart:X},0x{row.SourceEnd:X}) size=0x{row.SourceSize:X} " +
                $"block=0x{row.BlockOffset:X} stream=0x{row.StreamOffset:X}");
        }
    }

    static string FormatFileCounts(IReadOnlyDictionary<uint, int> counts)
    {
        return string.Join(", ", counts.Select(x => $"{GetFileLabel(x.Key)}={x.Value:N0}"));
    }

    static string GetFileLabel(uint fileIndex)
    {
        return fileIndex == 0 ? "<fastfile>" : $"imagefile{fileIndex}";
    }

    static string Csv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    static string Hex(int value) => $"0x{value:X}";

    static string Hex(uint value) => $"0x{value:X}";

    readonly record struct GfxImageEntryDebugRow(
        int ImageIndex,
        int PartIndex,
        int EntryIndex,
        int SerializedOffset,
        uint FileIndex,
        string FileName,
        uint SourceStart,
        uint SourceEnd,
        uint SourceSize,
        uint BlockOffset,
        uint StreamOffset,
        uint StreamBlockBase,
        bool StreamLow16MatchesBlockOffset,
        bool IsSourceRangeValid,
        bool IsEmpty);

    readonly record struct GfxImageEntrySummary(
        int ImageCount,
        int NonEmptyRowCount,
        int EmptyRowCount,
        int InvalidSourceRanges,
        int InvalidStreamLow16,
        IReadOnlyDictionary<uint, int> RowsByFileIndex);
}
