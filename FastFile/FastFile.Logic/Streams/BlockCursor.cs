using System;
using System.Buffers.Binary;

namespace FastFile.Logic.Streams;

public sealed class BlockCursor : IBlockCursor
{

    private readonly bool _bigEndian;
    private readonly string _name;
    private byte[] _buffer;
    private int _position;

    public int DeclaredSize { get; }
    public bool CanGrow => DeclaredSize == 0;

    public BlockCursor(int declaredSize, bool bigEndian = true, string? debugName = null)
    {
        if (declaredSize < 0)
            throw new ArgumentOutOfRangeException(nameof(declaredSize));

        DeclaredSize = declaredSize;
        _bigEndian = bigEndian;
        _name = debugName ?? "Block";
        _buffer = new byte[Math.Max(0, declaredSize)];
        _position = 0;
    }

    public long Position
    {
        get => _position;
        set
        {
            if (value < 0)
                throw new InvalidOperationException($"[{_name}] cannot seek to negative position {value}.");

            if (value > int.MaxValue)
                throw new InvalidOperationException($"[{_name}] seek value out of range: {value}.");

            var next = (int)value;
            EnsureWritable(next);
            _position = next;
        }
    }

    public long Length => _position;

    public int Align(int alignmentMask)
    {
        if (alignmentMask < 0)
            throw new ArgumentOutOfRangeException(nameof(alignmentMask));

        if (alignmentMask == 0)
            return _position;

        int target = (_position + alignmentMask) & ~alignmentMask;
        DB_IncStreamPos(target - _position);
        return _position;
    }

    public int DB_AllocStreamPos(int alignmentMask)
    {
        return Align(alignmentMask);
    }

    public void DB_IncStreamPos(int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        int target = checked(_position + byteCount);
        EnsureWritable(target);
        _position = target;
    }

    public T ReadStruct<T>() where T : struct
    {
        return StructReaderRegistry.Read<T>(this);
    }

    public byte ReadByte()
    {
        const int size = sizeof(byte);
        EnsureReadable(_position, size);

        byte value = _buffer[_position];
        _position += size;
        return value;
    }

    public sbyte ReadSByte()
    {
        return unchecked((sbyte)ReadByte());
    }

    public ushort ReadUInt16()
    {
        const int size = sizeof(ushort);
        EnsureReadable(_position, size);

        ushort value = _bigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public short ReadInt16()
    {
        const int size = sizeof(short);
        EnsureReadable(_position, size);

        short value = _bigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public int ReadInt32()
    {
        const int size = sizeof(int);
        EnsureReadable(_position, size);

        int value = _bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public uint ReadUInt32()
    {
        const int size = sizeof(uint);
        EnsureReadable(_position, size);

        uint value = _bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public long ReadInt64()
    {
        const int size = sizeof(long);
        EnsureReadable(_position, size);

        long value = _bigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public ulong ReadUInt64()
    {
        const int size = sizeof(ulong);
        EnsureReadable(_position, size);

        ulong value = _bigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return value;
    }

    public float ReadFloat()
    {
        const int size = sizeof(int);
        EnsureReadable(_position, size);

        int bits = _bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(_position, size))
            : BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position, size));

        _position += size;
        return BitConverter.Int32BitsToSingle(bits);
    }

    public double ReadDouble()
    {
        long bits = ReadInt64();
        return BitConverter.Int64BitsToDouble(bits);
    }

    public void WriteByte(byte value)
    {
        EnsureWritable(_position + 1);
        _buffer[_position++] = value;
    }

    public void WriteSByte(sbyte value)
    {
        WriteByte(unchecked((byte)value));
    }

    public void WriteUInt16(ushort value)
    {
        EnsureWritable(_position + sizeof(ushort));

        if (_bigEndian)
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position, sizeof(ushort)), value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_position, sizeof(ushort)), value);

        _position += sizeof(ushort);
    }

    public void WriteInt16(short value)
    {
        EnsureWritable(_position + sizeof(short));

        if (_bigEndian)
            BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(_position, sizeof(short)), value);
        else
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_position, sizeof(short)), value);

        _position += sizeof(short);
    }

    public void WriteInt32(int value)
    {
        EnsureWritable(_position + sizeof(int));

        if (_bigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_position, sizeof(int)), value);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position, sizeof(int)), value);
        }

        _position += sizeof(int);
    }

    public void WriteUInt32(uint value)
    {
        EnsureWritable(_position + sizeof(uint));

        if (_bigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_position, sizeof(uint)), value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position, sizeof(uint)), value);
        }

        _position += sizeof(uint);
    }

    public void WriteInt64(long value)
    {
        EnsureWritable(_position + sizeof(long));

        if (_bigEndian)
            BinaryPrimitives.WriteInt64BigEndian(_buffer.AsSpan(_position, sizeof(long)), value);
        else
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position, sizeof(long)), value);

        _position += sizeof(long);
    }

    public void WriteUInt64(ulong value)
    {
        EnsureWritable(_position + sizeof(ulong));

        if (_bigEndian)
            BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(_position, sizeof(ulong)), value);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position, sizeof(ulong)), value);

        _position += sizeof(ulong);
    }

    public void WriteFloat(float value)
    {
        WriteInt32(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteDouble(double value)
    {
        WriteInt64(BitConverter.DoubleToInt64Bits(value));
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return;

        int target = checked(_position + data.Length);
        EnsureWritable(target);
        data.CopyTo(_buffer.AsSpan(_position, data.Length));
        _position = target;
    }

    public void PatchInt32(int offset, int value)
    {
        if (offset < 0)
            throw new InvalidOperationException($"[{_name}] patch offset cannot be negative: {offset}.");

        EnsureWritable(offset + sizeof(int));

        if (_bigEndian)
        {
            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(offset, sizeof(int)), value);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(offset, sizeof(int)), value);
        }
    }

    public ReadOnlySpan<byte> AsSpan(int offset, int count)
    {
        EnsureReadable(offset, count);
        return _buffer.AsSpan(offset, count);
    }

    public byte[] ToArray()
    {
        return _buffer.AsSpan(0, _position).ToArray();
    }

    private void EnsureWritable(int target)
    {
        if (target < 0)
            throw new InvalidOperationException($"[{_name}] negative write target {target}.");

        if (!CanGrow && target > DeclaredSize)
            throw new InvalidOperationException(
                $"[{_name}] write to 0x{target:X} exceeds declared size 0x{DeclaredSize:X}.");

        if (target <= _buffer.Length)
            return;

        if (!CanGrow)
            throw new InvalidOperationException(
                $"[{_name}] write to 0x{target:X} exceeds fixed size 0x{DeclaredSize:X}.");

        int next = _buffer.Length;
        while (next < target)
            next = Math.Max(1, next * 2);

        Array.Resize(ref _buffer, next);
    }

    private void EnsureReadable(int offset, int count)
    {
        if (offset < 0 || count < 0)
            throw new ArgumentOutOfRangeException($"[{_name}] invalid read params.");

        long end = (long)offset + count;
        if (end > _position)
            throw new InvalidOperationException(
                $"[{_name}] read {count} at 0x{offset:X} exceeds logical length 0x{_position:X}.");
    }
}
