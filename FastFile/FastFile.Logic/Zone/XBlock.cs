using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public class XBlock
{
    public readonly XFILE_BLOCK BlockType;
    private readonly MemoryStream _stream;
    
    public int Position => (int)_stream.Position;
    public ReadOnlySpan<byte> WrittenSpan => _stream.GetBuffer().AsSpan(0, (int)_stream.Length);
    
    public XBlockAddress Address => new XBlockAddress(BlockType, Position);
    
    public XBlock(XFILE_BLOCK blockType, int  capacity)
    {
        BlockType = blockType;

        if(capacity < 0)
            throw new InvalidDataException($"Invalid negative XFILE block size {capacity} for block {blockType}.");
        
        _stream = new MemoryStream(capacity);
    }
    
    public void PatchInt32(int offset, int value)
    {
        long oldPosition = _stream.Position;

        _stream.Position = offset;
        WriteInt32(value);

        _stream.Position = oldPosition;
    }

    public void WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        _stream.Write(buffer);
    }

    public void WriteCString(string value)
    {
        int byteCount = Encoding.ASCII.GetByteCount(value);

        Span<byte> buffer = byteCount <= 256 ? stackalloc byte[byteCount + 1] : new byte[byteCount + 1];
        Encoding.ASCII.GetBytes(value, buffer);
        buffer[byteCount] = 0;

        _stream.Write(buffer);
    }

    public void Write(byte[] value)
    {
        _stream.Write(value, 0, value.Length);
    }

    ~XBlock()
    {
        _stream.Dispose();
    }
}