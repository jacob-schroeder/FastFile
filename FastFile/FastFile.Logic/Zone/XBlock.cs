using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public class XBlock
{
    public readonly XFILE_BLOCK BlockType;
    private readonly MemoryStream _stream;
    private readonly int _capacity;
    
    public int Position => (int)_stream.Position;
    public ReadOnlySpan<byte> WrittenSpan => _stream.GetBuffer().AsSpan(0, (int)_stream.Length);
    public ReadOnlySpan<byte> BlockSpan => _stream.GetBuffer().AsSpan(0, _capacity);
    
    public XBlockAddress Address => new XBlockAddress(BlockType, Position);
    
    public XBlock(XFILE_BLOCK blockType, int  capacity)
    {
        BlockType = blockType;

        if(capacity < 0)
            throw new InvalidDataException($"Invalid negative XFILE block size {capacity} for block {blockType}.");
        
        _capacity = capacity;
        _stream = new MemoryStream(capacity);
    }
    
    public void PatchInt32(int offset, int value)
    {
        EnsureCanWrite(offset, sizeof(int));

        long oldPosition = _stream.Position;

        _stream.Position = offset;
        WriteInt32(value);

        _stream.Position = oldPosition;
    }

    public void WriteInt32(int value)
    {
        EnsureCanWrite(Position, sizeof(int));

        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        _stream.Write(buffer);
    }

    public void WriteCString(string value)
    {
        int byteCount = Encoding.ASCII.GetByteCount(value);
        EnsureCanWrite(Position, byteCount + 1);

        Span<byte> buffer = byteCount <= 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
        Encoding.ASCII.GetBytes(value, buffer);
        buffer[byteCount] = 0;

        _stream.Write(buffer);
    }

    public void Write(byte[] value)
    {
        EnsureCanWrite(Position, value.Length);
        _stream.Write(value, 0, value.Length);
    }

    public void Align(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        var padding = (alignment - (Position % alignment)) % alignment;
        if (padding == 0)
            return;

        EnsureCanWrite(Position, padding);
        Span<byte> buffer = padding <= 16 ? stackalloc byte[padding] : new byte[padding];
        _stream.Write(buffer);
    }

    public void Seek(int offset)
    {
        if (offset < 0 || offset > _capacity)
        {
            throw new InvalidDataException(
                $"Seeking {BlockType} to 0x{offset:X} exceeds block size 0x{_capacity:X}.");
        }

        _stream.Position = offset;
    }

    public void PadToCapacity()
    {
        if (_stream.Length > _capacity)
        {
            throw new InvalidDataException(
                $"{BlockType} stream length 0x{_stream.Length:X} exceeds block size 0x{_capacity:X}.");
        }

        _stream.Position = _stream.Length;

        var remaining = _capacity - (int)_stream.Length;
        if (remaining == 0)
            return;

        var buffer = new byte[Math.Min(remaining, 4096)];
        while (remaining > 0)
        {
            var count = Math.Min(remaining, buffer.Length);
            _stream.Write(buffer, 0, count);
            remaining -= count;
        }
    }

    private void EnsureCanWrite(int offset, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (offset < 0 || offset + count > _capacity)
        {
            throw new InvalidDataException(
                $"Writing 0x{count:X} bytes at {BlockType}:0x{offset:X} exceeds block size 0x{_capacity:X}.");
        }
    }

    ~XBlock()
    {
        _stream.Dispose();
    }
}
