namespace FastFile.Logic.Streams;

public interface IWriteCursor : IReadCursor
{
    void WriteByte(byte value);
    void WriteSByte(sbyte value);
    void WriteUInt16(ushort value);
    void WriteInt16(short value);
    void WriteInt32(int value);
    void WriteUInt32(uint value);
    void WriteInt64(long value);
    void WriteUInt64(ulong value);
    void WriteFloat(float value);
    void WriteDouble(double value);
    void WriteBytes(ReadOnlySpan<byte> data);
}
