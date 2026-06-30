using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using FastFile.Models.Assets.Image;
using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Database.Streaming;

namespace FastFile.Render.Export;

internal sealed class GfxImageStreamResolver
{
    private const int PackageHeaderSize = 0x0c;
    private const int FullBlockSize = 0x10000;
    private const ushort BlockTerminator = 1;

    private readonly DbHeaderImageStreamEntry[] _entriesByStreamIndex;
    private readonly string _packageDirectory;

    public GfxImageStreamResolver(DbHeader header, string fastFilePath)
    {
        _entriesByStreamIndex = header.ImageStreamEntries;
        _packageDirectory = Path.GetDirectoryName(Path.GetFullPath(fastFilePath)) ?? Environment.CurrentDirectory;
    }

    public bool TryReadBestPayload(
        GfxImageAsset image,
        out byte[] payload,
        out int width,
        out int height,
        out string reason)
    {
        payload = [];
        width = 0;
        height = 0;
        reason = string.Empty;

        if (image.StreamImageIndex is not { } imageIndex)
        {
            reason = "image has no PS3 stream index";
            return false;
        }

        var candidates = image.StreamData
            .Select((streamData, partIndex) => new { streamData, partIndex })
            .Where(x => x.streamData.Width > 0 && x.streamData.Height > 0 && x.streamData.CumulativeByteCount != 0)
            .OrderByDescending(x => x.streamData.Width * x.streamData.Height);

        string? lastReason = null;
        foreach (var candidate in candidates)
        {
            GfxImageStreamData streamData = candidate.streamData;
            int previousByteCount = candidate.partIndex == 0
                ? 0
                : image.StreamData[candidate.partIndex - 1].CumulativeByteCount;
            int byteCount = checked(streamData.CumulativeByteCount - previousByteCount);
            if (byteCount <= 0)
            {
                lastReason = $"stream part {candidate.partIndex} byte count is zero";
                continue;
            }

            int streamEntryIndex = checked(imageIndex * GfxImageStreamTable.StreamPartsPerImage + candidate.partIndex);
            if (!TryGetEntry(streamEntryIndex, out DbHeaderImageStreamEntry entry, out reason))
            {
                lastReason = reason;
                continue;
            }

            if (!TryReadPackagePayload(entry, byteCount, out payload, out reason))
            {
                lastReason = reason;
                continue;
            }

            width = streamData.Width;
            height = streamData.Height;
            return true;
        }

        reason = lastReason ?? "no stream data";
        return false;
    }

    private bool TryGetEntry(int streamIndex, out DbHeaderImageStreamEntry entry, out string reason)
    {
        if (streamIndex >= _entriesByStreamIndex.Length)
        {
            entry = default;
            reason = $"stream index 0x{streamIndex:X} is outside the DB header table";
            return false;
        }

        entry = _entriesByStreamIndex[streamIndex];
        if (entry.IsEmpty)
        {
            reason = $"stream index 0x{streamIndex:X} is empty";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool TryReadPackagePayload(
        DbHeaderImageStreamEntry entry,
        int byteCount,
        out byte[] payload,
        out string reason)
    {
        payload = [];
        reason = string.Empty;

        if (entry.FileIndex == 0)
        {
            reason = "image stream points at the current fastfile; render package resolver only handles imagefileN.pak";
            return false;
        }

        string path = Path.Combine(_packageDirectory, $"imagefile{entry.FileIndex}.pak");
        if (!File.Exists(path))
        {
            reason = $"missing texture stream package {path}";
            return false;
        }

        using FileStream file = File.OpenRead(path);
        if (!ValidatePackageHeader(file, path, out reason))
            return false;

        if (entry.SourceStart < PackageHeaderSize || entry.SourceStart >= file.Length)
        {
            reason = $"texture stream source start 0x{entry.SourceStart:X} is outside {Path.GetFileName(path)}";
            return false;
        }

        file.Position = entry.SourceStart;
        using var output = new MemoryStream(byteCount);
        int skip = checked((int)entry.BlockOffset);
        while (output.Length < byteCount)
        {
            if (entry.SourceEnd != 0 && file.Position >= entry.SourceEnd)
            {
                reason = $"texture stream entry range 0x{entry.SourceStart:X}-0x{entry.SourceEnd:X} ended before texture payload was complete";
                return false;
            }

            byte[] block;
            if (!TryReadNextBlock(file, path, out block, out reason))
                return false;

            if (entry.SourceEnd != 0 && file.Position > entry.SourceEnd)
            {
                reason = $"texture stream package block crossed entry range 0x{entry.SourceStart:X}-0x{entry.SourceEnd:X}";
                return false;
            }

            if (skip >= block.Length)
            {
                skip -= block.Length;
                continue;
            }

            int remaining = byteCount - checked((int)output.Length);
            int take = Math.Min(remaining, block.Length - skip);
            output.Write(block, skip, take);
            skip = 0;
        }

        payload = output.ToArray();
        return true;
    }

    private static bool ValidatePackageHeader(FileStream file, string path, out string reason)
    {
        reason = string.Empty;
        if (file.Length < PackageHeaderSize)
        {
            reason = $"{Path.GetFileName(path)} is too small for an image package header";
            return false;
        }

        Span<byte> header = stackalloc byte[PackageHeaderSize];
        file.ReadExactly(header);
        string magic = Encoding.Latin1.GetString(header[..8]);
        if (magic is not ("IWffu100" or "S1ffu100"))
        {
            reason = $"{Path.GetFileName(path)} has unexpected package magic '{magic}'";
            return false;
        }

        return true;
    }

    private static bool TryReadNextBlock(FileStream file, string path, out byte[] block, out string reason)
    {
        block = [];
        reason = string.Empty;

        Span<byte> sizeBytes = stackalloc byte[sizeof(ushort)];
        if (!TryReadExactly(file, sizeBytes))
        {
            reason = $"unexpected end of {Path.GetFileName(path)} while reading package block size";
            return false;
        }

        ushort encodedSize = BinaryPrimitives.ReadUInt16BigEndian(sizeBytes);
        if (encodedSize == BlockTerminator)
        {
            reason = $"hit package block terminator in {Path.GetFileName(path)} before texture payload was complete";
            return false;
        }

        int byteCount = encodedSize == 0 ? FullBlockSize : encodedSize;
        if (file.Position + byteCount > file.Length)
        {
            reason = $"package block at 0x{file.Position - sizeof(ushort):X} in {Path.GetFileName(path)} extends past end of file";
            return false;
        }

        byte[] bytes = new byte[byteCount];
        file.ReadExactly(bytes);
        if (encodedSize == 0)
        {
            block = bytes;
            return true;
        }

        try
        {
            using var input = new MemoryStream(bytes);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(FullBlockSize);
            deflate.CopyTo(output);
            block = output.ToArray();
            return true;
        }
        catch (InvalidDataException ex)
        {
            reason = $"failed to inflate package block at 0x{file.Position - byteCount - sizeof(ushort):X} in {Path.GetFileName(path)}: {ex.Message}";
            return false;
        }
    }

    private static bool TryReadExactly(Stream stream, Span<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            int read = stream.Read(buffer);
            if (read == 0)
                return false;

            buffer = buffer[read..];
        }

        return true;
    }
}
