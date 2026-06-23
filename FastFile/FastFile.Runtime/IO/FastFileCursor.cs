using System.Buffers.Binary;
using System.Text;

namespace FastFile.Runtime.IO;

public sealed class FastFileCursor
{
    private readonly ReadOnlyMemory<byte> _memory;

    public FastFileCursor(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    public int Offset { get; private set; }
    public int Length => _memory.Length;
    public int Remaining => Length - Offset;

    private ReadOnlySpan<byte> Span => _memory.Span;

    public byte ReadByte()
    {
        EnsureAvailable(sizeof(byte));
        return Span[Offset++];
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(sizeof(ushort));
        ushort value = BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(Offset, sizeof(ushort)));
        Offset += sizeof(ushort);
        return value;
    }

    public ushort PeekUInt16()
    {
        EnsureAvailable(sizeof(ushort));
        return BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(Offset, sizeof(ushort)));
    }

    public int ReadInt32()
    {
        EnsureAvailable(sizeof(int));
        int value = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(Offset, sizeof(int)));
        Offset += sizeof(int);
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(sizeof(uint));
        uint value = BinaryPrimitives.ReadUInt32BigEndian(Span.Slice(Offset, sizeof(uint)));
        Offset += sizeof(uint);
        return value;
    }

    public ulong ReadUInt64()
    {
        EnsureAvailable(sizeof(ulong));
        ulong value = BinaryPrimitives.ReadUInt64BigEndian(Span.Slice(Offset, sizeof(ulong)));
        Offset += sizeof(ulong);
        return value;
    }

    public string ReadFixedString(int length)
    {
        EnsureAvailable(length);
        string value = Encoding.Latin1.GetString(Span.Slice(Offset, length));
        Offset += length;
        return value;
    }

    public string ReadCString()
    {
        int start = Offset;

        while (Offset < Length && Span[Offset] != 0)
            Offset++;

        EnsureAvailable(sizeof(byte));
        string value = Encoding.Latin1.GetString(Span.Slice(start, Offset - start));
        Offset++;
        return value;
    }

    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length);
        byte[] value = Span.Slice(Offset, length).ToArray();
        Offset += length;
        return value;
    }

    public void Skip(int length)
    {
        EnsureAvailable(length);
        Offset += length;
    }

    public void Align(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        int aligned = (Offset + alignment - 1) / alignment * alignment;
        Skip(aligned - Offset);
    }

    private void EnsureAvailable(int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        if (Offset + byteCount > Length)
            throw new EndOfStreamException($"Tried to read 0x{byteCount:X} byte(s) at 0x{Offset:X}, beyond buffer length 0x{Length:X}.");
    }
}
