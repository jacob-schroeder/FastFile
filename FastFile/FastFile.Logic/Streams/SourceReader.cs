using System;
using System.Buffers.Binary;

namespace FastFile.Logic.Streams;

public sealed class SourceReader(byte[] buffer, bool bigEndian = true) : IReadCursor
{
    private readonly ReadOnlyMemory<byte> _memory = new(buffer);
    private readonly bool _bigEndian = bigEndian;
    private readonly string _name = "Source";

    public long Position { get; set; }
    public long Length => buffer.Length;

    public T ReadStruct<T>() where T : struct
    {
        return StructReaderRegistry.Read<T>(this);
    }

    public byte ReadByte()
    {
        const int size = sizeof(byte);
        EnsureReadable(Position, size);

        byte value = _memory.Span[(int)Position];
        Position += size;
        return value;
    }

    public sbyte ReadSByte()
    {
        return unchecked((sbyte)ReadByte());
    }

    public ushort ReadUInt16()
    {
        const int size = sizeof(ushort);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadUInt16LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public short ReadInt16()
    {
        const int size = sizeof(short);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadInt16LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public int ReadInt32()
    {
        const int size = sizeof(int);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadInt32LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public uint ReadUInt32()
    {
        const int size = sizeof(uint);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadUInt32LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public long ReadInt64()
    {
        const int size = sizeof(long);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadInt64LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public ulong ReadUInt64()
    {
        const int size = sizeof(ulong);
        EnsureReadable(Position, size);

        var value = _bigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadUInt64LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return value;
    }

    public float ReadFloat()
    {
        const int size = sizeof(float);
        EnsureReadable(Position, size);

        int bits = _bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_memory.Span.Slice((int)Position, size))
            : BinaryPrimitives.ReadInt32LittleEndian(_memory.Span.Slice((int)Position, size));

        Position += size;
        return BitConverter.Int32BitsToSingle(bits);
    }

    public double ReadDouble()
    {
        long bits = ReadInt64();
        return BitConverter.Int64BitsToDouble(bits);
    }

    public int Align(int alignment)
    {
        if (alignment < 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        if (alignment == 0)
            return (int)Position;

        if (Position > int.MaxValue)
            throw new InvalidOperationException($"[{_name}] seek position exceeds int range: {Position}.");

        int aligned = ((int)Position + alignment) & ~alignment;
        if ((long)aligned > Length)
            throw new InvalidOperationException(
                $"[{_name}] alignment to 0x{aligned:X} exceeds source length 0x{Length:X}.");

        Position = aligned;
        return (int)Position;
    }

    private void EnsureReadable(long offset, int count)
    {
        if (offset < 0 || count < 0)
            throw new ArgumentOutOfRangeException($"[{_name}] invalid read params.");

        if (offset + count > Length)
            throw new InvalidOperationException(
                $"[{_name}] read {count} at 0x{offset:X} exceeds available length 0x{Length:X}.");
    }
}
