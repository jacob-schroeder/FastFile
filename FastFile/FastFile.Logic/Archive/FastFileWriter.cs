using System.Buffers.Binary;
using System.Text;
using FastFile.Logic.Compression;
using FastFile.Models.Archive;

namespace FastFile.Logic.Archive;

public sealed class FastFileWriter
{
    private const int ZoneBlockSize = 0x10000;
    private const ushort ZoneBlockTerminator = 1;
#if PS3
    private const int EntrySize = 0x14;
#endif

    private readonly DB_Header _header;
    private readonly byte[] _zoneBuffer;

    public FastFileWriter(byte[] zoneBuffer)
        : this(CreateDefaultHeader(), zoneBuffer)
    {
    }

    public FastFileWriter(DB_Header header, byte[] zoneBuffer)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        _zoneBuffer = zoneBuffer ?? throw new ArgumentNullException(nameof(zoneBuffer));
    }

    public DB_Header Header => _header;

    public byte[] Write()
    {
        var packedZone = PackZone();
        var fileSize = GetHeaderSize(_header) + packedZone.Length;

        _header.FileSize = fileSize - 2;
        _header.MaxFileSize = fileSize - 2;

        using var stream = new MemoryStream(fileSize);
        WriteHeader(stream);
        stream.Write(packedZone);

        return stream.ToArray();
    }

    public byte[] WriteHeader()
    {
        using var stream = new MemoryStream(GetHeaderSize(_header));
        WriteHeader(stream);
        return stream.ToArray();
    }

    public byte[] PackZone()
    {
        using var stream = new MemoryStream();

        for (var offset = 0; offset < _zoneBuffer.Length; offset += ZoneBlockSize)
        {
            var blockLength = Math.Min(ZoneBlockSize, _zoneBuffer.Length - offset);
            var block = new byte[ZoneBlockSize];
            Array.Copy(_zoneBuffer, offset, block, 0, blockLength);

            WriteCompressedBlock(stream, block);
        }

        WriteBlockTerminator(stream);

        return stream.ToArray();
    }

    private void WriteHeader(Stream stream)
    {
        WriteFixedString(stream, _header.Magic, 8);
        WriteInt32(stream, (int)_header.Version);
        stream.WriteByte(_header.AllowOnlineUpdate ? (byte)1 : (byte)0);
        WriteUInt64(stream, _header.FileCreationTime);
        WriteInt32(stream, (int)_header.Region);
        WriteInt32(stream, _header.EntryCount);

#if PS3
        var expectedEntryBytes = checked(_header.EntryCount * EntrySize);
        if (_header.EntryBytes.Length == 0 && expectedEntryBytes > 0)
        {
            stream.Write(new byte[expectedEntryBytes]);
        }
        else
        {
            if (_header.EntryBytes.Length != expectedEntryBytes)
                throw new InvalidDataException($"Header entry byte length {_header.EntryBytes.Length:N0} does not match EntryCount {_header.EntryCount:N0} * 0x{EntrySize:X}.");

            stream.Write(_header.EntryBytes);
        }
#endif

        WriteInt32(stream, _header.FileSize);
        WriteInt32(stream, _header.MaxFileSize);
    }

    private static void WriteCompressedBlock(Stream stream, byte[] block)
    {
        if (block.Length != ZoneBlockSize)
            throw new InvalidDataException($"Fastfile zone block length must be 0x{ZoneBlockSize:X}; got 0x{block.Length:X}.");

        var compressed = ZLib.Compress(block);
        if (compressed.Length < ZLib.HEADER_SIZE + ZLib.ADLR32_SIZE)
            throw new InvalidDataException("Zlib returned an invalid compressed block.");

        var storedBlockSize = compressed.Length - ZLib.HEADER_SIZE;
        if (storedBlockSize > ushort.MaxValue)
            throw new InvalidDataException($"Compressed zone block is too large for UInt16 storage: {storedBlockSize:N0} bytes.");

        Span<byte> sizeBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(sizeBytes, (ushort)storedBlockSize);
        stream.Write(sizeBytes);
        stream.Write(compressed.AsSpan(ZLib.HEADER_SIZE));
    }

    private static void WriteBlockTerminator(Stream stream)
    {
        Span<byte> terminator = stackalloc byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(terminator[..2], ZoneBlockTerminator);
        BinaryPrimitives.WriteUInt16BigEndian(terminator[2..], ZoneBlockTerminator);
        stream.Write(terminator);
    }

    private static void WriteFixedString(Stream stream, string? value, int length)
    {
        Span<byte> buffer = stackalloc byte[length];
        if (!string.IsNullOrEmpty(value))
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            bytes.AsSpan(0, Math.Min(bytes.Length, length)).CopyTo(buffer);
        }

        stream.Write(buffer);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static int GetHeaderSize(DB_Header header)
    {
#if PS3
        return checked(8 + 4 + 1 + 8 + 4 + 4 + header.EntryCount * EntrySize + 4 + 4);
#else
        return 8 + 4 + 1 + 8 + 4 + 4 + 4 + 4;
#endif
    }

    private static DB_Header CreateDefaultHeader()
    {
        return new DB_Header
        {
            Magic = "IWffu100",
            Version = XFILE_VERSION.Mw2,
            AllowOnlineUpdate = true,
            FileCreationTime = (ulong)DateTime.UtcNow.ToFileTimeUtc(),
            Region = Language.LANGUAGE_ENGLISH,
            EntryCount = 0,
            EntryBytes = [],
        };
    }
}
