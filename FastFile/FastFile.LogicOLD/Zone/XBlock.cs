using System.Buffers.Binary;
using System.Text;
using FastFile.ModelsOLD.Zone;

namespace FastFile.LogicOLD.Zone;

public class XBlock
{
    public readonly XFILE_BLOCK BlockType;
    private readonly MemoryStream _stream;
    private readonly int _declaredSize;
    
    public int Position => (int)_stream.Position;
    public int DeclaredSize => _declaredSize;
    public ReadOnlySpan<byte> WrittenSpan => _stream.GetBuffer().AsSpan(0, (int)_stream.Length);
    public ReadOnlySpan<byte> BlockSpan => _stream.GetBuffer().AsSpan(0, LogicalSize);
    
    public XBlockAddress Address => new XBlockAddress(BlockType, Position);

    private bool CanGrow => _declaredSize == 0;
    public int LogicalSize => Math.Max(_declaredSize, (int)_stream.Length);
    
    public XBlock(XFILE_BLOCK blockType, int  capacity)
    {
        BlockType = blockType;

        if(capacity < 0)
            throw new InvalidDataException($"Invalid negative XFILE block size {capacity} for block {blockType}.");
        
        _declaredSize = capacity;
        _stream = new MemoryStream(capacity);
    }
    
    public void PatchInt32(int offset, int value)
    {
        EnsureCanAccess(offset, sizeof(int));

        long oldPosition = _stream.Position;

        _stream.Position = offset;
        WriteInt32(value);

        _stream.Position = oldPosition;
    }

    public int ReadInt32(int offset)
    {
        EnsureCanAccess(offset, sizeof(int));
        return BinaryPrimitives.ReadInt32BigEndian(BlockSpan.Slice(offset, sizeof(int)));
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

    public void DB_AllocStreamPos(int alignmentMask)
    {
        if (alignmentMask < 0)
            throw new ArgumentOutOfRangeException(nameof(alignmentMask));

        var alignedPosition = (Position + alignmentMask) & ~alignmentMask;
        var padding = alignedPosition - Position;
        if (padding == 0)
            return;

        EnsureCanWrite(Position, padding);
        Span<byte> buffer = padding <= 16 ? stackalloc byte[padding] : new byte[padding];
        _stream.Write(buffer);
    }

    public void DB_IncStreamPos(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var target = Position + count;
        EnsureCanWrite(Position, count);
        _stream.Position = target;
    }

    public void Seek(int offset)
    {
        if (offset < 0)
            throw new InvalidDataException($"Seeking {BlockType} to negative offset 0x{offset:X}.");

        if (!CanGrow && offset > _declaredSize)
        {
            throw new InvalidDataException(
                $"Seeking {BlockType} to 0x{offset:X} exceeds block size 0x{_declaredSize:X}.");
        }

        _stream.Position = offset;
    }

    public void PadToCapacity()
    {
        if (CanGrow)
            return;

        if (_stream.Length > _declaredSize)
        {
            throw new InvalidDataException(
                $"{BlockType} stream length 0x{_stream.Length:X} exceeds block size 0x{_declaredSize:X}.");
        }

        _stream.Position = _stream.Length;

        var remaining = _declaredSize - (int)_stream.Length;
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

        if (offset < 0)
            throw new InvalidDataException($"Writing at negative {BlockType} offset 0x{offset:X}.");

        if (!CanGrow && offset + count > _declaredSize)
        {
            throw new InvalidDataException(
                $"Writing 0x{count:X} bytes at {BlockType}:0x{offset:X} exceeds block size 0x{_declaredSize:X}.");
        }
    }

    private void EnsureCanAccess(int offset, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (offset < 0 || offset + count > LogicalSize)
        {
            throw new InvalidDataException(
                $"Accessing 0x{count:X} bytes at {BlockType}:0x{offset:X} exceeds logical block size 0x{LogicalSize:X}.");
        }
    }

    ~XBlock()
    {
        _stream.Dispose();
    }
}
