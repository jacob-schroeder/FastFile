using FastFile.Models;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Logic.Streams;

public sealed class MirroredReadCursor(IReadCursor source, XFileBlockStack blocks) : IReadCursor, IPointerCellRecorder
{
    private readonly Queue<PointerCell> _pointerCells = new();

    public long Position
    {
        get => source.Position;
        set => source.Position = value;
    }

    public long Length => source.Length;
    public XBlockAddress CurrentWriteAddress => blocks.ActiveAddress;

    public void RecordPointerCell(XBlockAddress address, int raw)
    {
        _pointerCells.Enqueue(new PointerCell(address, raw));
    }

    public PointerCell TakePointerCell()
    {
        if (!_pointerCells.TryDequeue(out PointerCell cell))
            throw new InvalidOperationException("No pointer cell was recorded.");

        return cell;
    }

    public T ReadStruct<T>() where T : struct
    {
        return StructReaderRegistry.Read<T>(this);
    }

    public byte ReadByte()
    {
        byte value = source.ReadByte();
        blocks.ActiveCursor.WriteByte(value);
        return value;
    }

    public sbyte ReadSByte()
    {
        sbyte value = source.ReadSByte();
        blocks.ActiveCursor.WriteSByte(value);
        return value;
    }

    public ushort ReadUInt16()
    {
        ushort value = source.ReadUInt16();
        blocks.ActiveCursor.WriteUInt16(value);
        return value;
    }

    public short ReadInt16()
    {
        short value = source.ReadInt16();
        blocks.ActiveCursor.WriteInt16(value);
        return value;
    }

    public int ReadInt32()
    {
        int value = source.ReadInt32();
        blocks.ActiveCursor.WriteInt32(value);
        return value;
    }

    public uint ReadUInt32()
    {
        uint value = source.ReadUInt32();
        blocks.ActiveCursor.WriteUInt32(value);
        return value;
    }

    public long ReadInt64()
    {
        long value = source.ReadInt64();
        blocks.ActiveCursor.WriteInt64(value);
        return value;
    }

    public ulong ReadUInt64()
    {
        ulong value = source.ReadUInt64();
        blocks.ActiveCursor.WriteUInt64(value);
        return value;
    }

    public float ReadFloat()
    {
        float value = source.ReadFloat();
        blocks.ActiveCursor.WriteFloat(value);
        return value;
    }

    public double ReadDouble()
    {
        double value = source.ReadDouble();
        blocks.ActiveCursor.WriteDouble(value);
        return value;
    }

    public int Align(int alignment)
    {
        int position = source.Align(alignment);
        blocks.ActiveCursor.Align(alignment);
        return position;
    }
}
