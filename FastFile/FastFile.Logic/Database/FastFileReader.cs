using FastFile.Logic.Compression;
using FastFile.Logic.Extensions;
using FastFile.Logic.Hashing;
using FastFile.Models.Database;

namespace FastFile.Logic.Database;

public sealed class FastFileReader(byte[] buffer, int length)
{
    private const ushort ZoneBlockTerminator = 1;

    private readonly ReadOnlyMemory<byte> _memory = buffer.AsMemory(0, length);
    private int _position;
    private int _length;
    
    private readonly IList<string> _warnings = new List<string>();

    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    private ReadOnlySpan<byte> Span => _memory.Span;

    public DB_Header ParseHeader()
    {
        var span = Span;

        var header = new DB_Header
        {
            Magic = span.ReadString(ref _position, 8),
            Version = (XFileVersion)span.ReadInt32(ref _position),
            AllowOnlineUpdate = span.ReadBool(ref _position),
            FileCreationTime = span.ReadUInt64(ref _position),
            Region = (Language)span.ReadInt32(ref _position),
            EntryCount = span.ReadInt32(ref _position),
        };

        // TO DO: IMPLEMENT PROPER FIX FOR ME
        header.ImageStreamEntries = new ImageStreamEntry[header.EntryCount];
        for (var i = 0; i < header.EntryCount; i++)
        {
            header.ImageStreamEntries[i] = new ImageStreamEntry
            {
                FileIndex = span.ReadUInt32(ref _position),
                SourceStart = span.ReadUInt32(ref _position),
                SourceEnd = span.ReadUInt32(ref _position),
                BlockOffset = span.ReadUInt32(ref _position),
                StreamOffset = span.ReadUInt32(ref _position)
            };
            
            //old
            //header.EntryBytes = span.Read(ref _position, header.EntryCount * 0x14);
        }

        header.FileSize = span.ReadInt32(ref _position);
        header.MaxFileSize = span.ReadInt32(ref _position);

        _length = header.FileSize;

        return header;
    }

    public byte[] UnpackZone()
    {
        using MemoryStream ms = new();
        
        while (_position < _length)
        {
            ushort blockSize = Span.ReadUInt16(ref _position);

            if (blockSize == ZoneBlockTerminator)
            {
                TryConsumeTrailingTerminatorWord();
                break;
            }

            //Bypass fastfiles compiled out of spec
            if (blockSize == 0)
            {
                _warnings.Add($"Encountered invalid block size: {blockSize}");
                _position += 2; //ZLib.HEADER_SIZE = uint16_t;
                continue;
            }

            ushort dataSize = (ushort)(blockSize - 4); //ZLib.ADLR32_SIZE = 32 bits, uint32_t

            byte[] compressed = Span.Read(ref _position, dataSize);
            uint checksum = Span.ReadUInt32(ref _position);

            byte[] decompressed = Deflate.Decompress(compressed);
            ms.Write(decompressed);
            
            //check checksum
            uint actual = Adler32.HashToUInt32(decompressed);
            if (actual != checksum)
            {
                //instead of exception throw a warning
                _warnings.Add($"Checksum mismatch. Expected {checksum:X8}, got {actual:X8}");
            }

        }

        return ms.ToArray();
    }

    private void TryConsumeTrailingTerminatorWord()
    {
        if (_position + sizeof(ushort) > Span.Length)
            return;

        var peekPosition = _position;
        if (Span.ReadUInt16(ref peekPosition) == ZoneBlockTerminator)
            _position = peekPosition;
    }
}