using FastFile.Models;
using FastFile.Models.Archive;
using FastFile.Logic.Compression;
using FastFile.Logic.Extensions;
using FastFile.Logic.Hashing;

namespace FastFile.Logic.Archive;

public sealed class FastFileReader(byte[] buffer, int length)
{
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
            Version = (XFILE_VERSION)span.ReadInt32(ref _position),
            AllowOnlineUpdate = span.ReadBool(ref _position),
            FileCreationTime = span.ReadUInt64(ref _position),
            Region = (Language)span.ReadInt32(ref _position),
            EntryCount = span.ReadInt32(ref _position),
        };

        //bypass Entry_t
        #if PS3
        _position += header.EntryCount * 0x14;
        #endif
        
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

            //Bypass fastfiles compiled out of spec
            if (blockSize is 0 or 1)
            {
                _warnings.Add($"Encountered invalid block size: {blockSize}");
                _position += ZLib.HEADER_SIZE;
                continue;
            }

            ushort dataSize = (ushort)(blockSize - ZLib.ADLR32_SIZE);

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
}
