namespace FastFile.Logic.Streams;

public interface IReadCursor
{
    long Position { get; set; }
    long Length { get; }

    T ReadStruct<T>() where T : struct;

    byte ReadByte();
    sbyte ReadSByte();
    ushort ReadUInt16();
    int ReadInt32();
    uint ReadUInt32();
    short ReadInt16();
    long ReadInt64();
    ulong ReadUInt64();
    float ReadFloat();
    double ReadDouble();
    int Align(int alignment);
}
