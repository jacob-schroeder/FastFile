using FastFile.Models.Zone;
using FastFile.Runtime.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FastFile.Runtime.Blocks;

public sealed class BlockStreamState
{
    private readonly Stack<XFileBlockType> _stack = new();
    private int[] _positions = [];
    private MemoryStream[] _streams = [];

    public XFileBlockType CurrentBlock { get; private set; } = XFileBlockType.TEMP;
    public int[] BlockSizes { get; private set; } = [];
    public XBlockAddress CurrentAddress => GetAddress(CurrentBlock);

    public void Initialize(XFile xfile)
    {
        BlockSizes = xfile.BlockSize;
        _positions = new int[BlockSizes.Length];
        _streams = new MemoryStream[BlockSizes.Length];
        for (int i = 0; i < _streams.Length; i++)
            _streams[i] = new MemoryStream(Math.Max(0, BlockSizes[i]));

        CurrentBlock = XFileBlockType.TEMP;
        _stack.Clear();
    }

    public void Push(XFileBlockType block)
    {
        _stack.Push(CurrentBlock);
        CurrentBlock = block;
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException("DB stream block stack underflow.");

        CurrentBlock = _stack.Pop();
    }

    public XBlockAddress GetAddress(XFileBlockType block)
    {
        int index = (int)block;
        if (index < 0 || index >= _positions.Length)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Invalid XFile block.");

        return new XBlockAddress(block, _positions[index]);
    }

    public void Advance(int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        int index = (int)CurrentBlock;
        _positions[index] += byteCount;
        EnsureLength(index, _positions[index]);
    }

    public XBlockAddress AllocateCurrent(int alignment)
    {
        AlignCurrent(alignment);
        return CurrentAddress;
    }

    public void AlignCurrent(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        int index = (int)CurrentBlock;
        int position = _positions[index];
        _positions[index] = (position + alignment - 1) / alignment * alignment;
        EnsureLength(index, _positions[index]);
    }

    public byte[] Load(FastFileCursor cursor, int byteCount)
    {
        byte[] bytes = cursor.ReadBytes(byteCount);
        Write(bytes);
        return bytes;
    }

    public string LoadCString(FastFileCursor cursor)
    {
        var bytes = new List<byte>();
        byte value;

        do
        {
            value = cursor.ReadByte();
            bytes.Add(value);
        }
        while (value != 0);

        Write(CollectionsMarshal.AsSpan(bytes));
        return Encoding.Latin1.GetString(CollectionsMarshal.AsSpan(bytes)[..^1]);
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        int index = (int)CurrentBlock;
        EnsureLength(index, _positions[index]);

        MemoryStream stream = _streams[index];
        stream.Position = _positions[index];
        stream.Write(bytes);
        _positions[index] += bytes.Length;
    }

    public byte[] GetBytes(XFileBlockType block)
    {
        int index = (int)block;
        if (index < 0 || index >= _streams.Length)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Invalid XFile block.");

        return _streams[index].ToArray();
    }

    public IReadOnlyDictionary<XFileBlockType, byte[]> Snapshot()
    {
        var blocks = new Dictionary<XFileBlockType, byte[]>();
        for (int i = 0; i < _streams.Length && i < (int)XFileBlockType.COUNT; i++)
            blocks[(XFileBlockType)i] = _streams[i].ToArray();

        return blocks;
    }

    public void DumpToDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach ((XFileBlockType block, byte[] bytes) in Snapshot())
        {
            string path = Path.Combine(directory, $"{(int)block}_{block}.bin");
            File.WriteAllBytes(path, bytes);
        }
    }

    private void EnsureLength(int index, int length)
    {
        MemoryStream stream = _streams[index];
        if (stream.Length >= length)
            return;

        stream.Position = stream.Length;
        stream.Write(new byte[length - stream.Length]);
    }
}
