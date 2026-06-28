using System.Buffers.Binary;
using System.Text;

namespace FastFile.Emitters;

public sealed class XSourceWriter
{
    private readonly MemoryStream _stream = new();

    public int Offset => checked((int)_stream.Position);

    public void WriteInt32(int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        _stream.Write(bytes);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        _stream.Write(bytes);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        _stream.Write(bytes);
    }

    public void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    public void WriteSingle(float value)
    {
        WriteInt32(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _stream.Write(bytes);
    }

    public void WriteCString(string value)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(value);
        _stream.Write(bytes);
        _stream.WriteByte(0);
    }

    public void Align(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        int padding = (alignment - Offset % alignment) % alignment;
        if (padding > 0)
            _stream.Write(new byte[padding]);
    }

    public byte[] ToArray()
    {
        return _stream.ToArray();
    }
}
