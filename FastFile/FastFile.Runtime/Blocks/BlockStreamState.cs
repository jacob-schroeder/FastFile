using FastFile.Models.Zone;
using FastFile.Runtime.Coverage;
using FastFile.Runtime.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FastFile.Runtime.Blocks;

public sealed class BlockStreamState
{
    private readonly Stack<StreamBlockFrame> _stack = new();
    private int[] _positions = [];
    private int[] _materializedLengths = [];
    private MemoryStream[] _streams = [];

    public SourceCoverageRecorder? SourceCoverage { get; set; }
    public XFileBlockType CurrentBlock { get; private set; } = XFileBlockType.TEMP;
    public int[] BlockSizes { get; private set; } = [];
    public XBlockAddress CurrentAddress => GetAddress(CurrentBlock);

    public void Initialize(XFile xfile)
    {
        BlockSizes = xfile.BlockSize;
        _positions = new int[BlockSizes.Length];
        _materializedLengths = new int[BlockSizes.Length];
        _streams = new MemoryStream[BlockSizes.Length];
        for (int i = 0; i < _streams.Length; i++)
            _streams[i] = new MemoryStream(Math.Max(0, BlockSizes[i]));

        CurrentBlock = XFileBlockType.TEMP;
        _stack.Clear();
    }

    public void Push(XFileBlockType block)
    {
        _stack.Push(new StreamBlockFrame(CurrentBlock, GetPosition(block)));
        CurrentBlock = block;
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException("DB stream block stack underflow.");

        StreamBlockFrame frame = _stack.Pop();

        if (CurrentBlock == XFileBlockType.TEMP)
            _positions[(int)XFileBlockType.TEMP] = frame.PushedBlockPosition;

        CurrentBlock = frame.PreviousBlock;
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
        _positions[index] = checked(_positions[index] + byteCount);
        EnsureLength(index, _positions[index]);
    }

    public XBlockAddress AllocateCurrent(int alignment)
    {
        AlignCurrent(alignment);
        return CurrentAddress;
    }

    public XBlockAddress AllocateInsertPointerCell()
    {
        int index = GetBlockIndex(XFileBlockType.LARGE);
        int position = _positions[index];
        int alignedPosition = checked((position + sizeof(int) - 1) / sizeof(int) * sizeof(int));
        _positions[index] = alignedPosition;
        EnsureLength(index, alignedPosition);

        var address = new XBlockAddress(XFileBlockType.LARGE, alignedPosition);
        WriteInt32(address, 0);
        _positions[index] = checked(alignedPosition + sizeof(int));
        EnsureLength(index, _positions[index]);
        return address;
    }

    public void AlignCurrent(int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        int index = (int)CurrentBlock;
        int position = _positions[index];
        _positions[index] = checked((position + alignment - 1) / alignment * alignment);
        EnsureLength(index, _positions[index]);
    }

    public byte[] Load(
        FastFileCursor cursor,
        int byteCount,
        string? memberName = null,
        [CallerMemberName] string callerName = "")
    {
        int sourceStart = cursor.Offset;
        XBlockAddress destinationAddress = CurrentAddress;
        byte[] bytes = cursor.ReadBytes(byteCount);
        Write(bytes);
        SourceCoverage?.RecordLoadStream(
            sourceStart,
            byteCount,
            destinationAddress.BlockType,
            destinationAddress.Offset,
            memberName ?? callerName,
            callerName);
        return bytes;
    }

    public byte[] Load(
        FastFileCursor cursor,
        int byteCount,
        out XBlockAddress address,
        string? memberName = null,
        [CallerMemberName] string callerName = "")
    {
        address = CurrentAddress;
        return Load(cursor, byteCount, memberName, callerName);
    }

    public string LoadCString(
        FastFileCursor cursor,
        string? memberName = null,
        [CallerMemberName] string callerName = "")
    {
        int sourceStart = cursor.Offset;
        XBlockAddress destinationAddress = CurrentAddress;
        var bytes = new List<byte>();
        byte value;

        do
        {
            value = cursor.ReadByte();
            bytes.Add(value);
        }
        while (value != 0);

        Write(CollectionsMarshal.AsSpan(bytes));
        SourceCoverage?.RecordCString(
            sourceStart,
            bytes.Count,
            destinationAddress.BlockType,
            destinationAddress.Offset,
            memberName ?? callerName,
            callerName);
        return Encoding.Latin1.GetString(CollectionsMarshal.AsSpan(bytes)[..^1]);
    }

    public void RecordSourcePadding(
        FastFileCursor cursor,
        int byteCount,
        string? memberName = null,
        [CallerMemberName] string callerName = "")
    {
        SourceCoverage?.RecordSourcePadding(
            cursor.Offset,
            byteCount,
            memberName ?? callerName,
            callerName);
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        int index = (int)CurrentBlock;
        int end = checked(_positions[index] + bytes.Length);
        EnsureLength(index, end);

        MemoryStream stream = _streams[index];
        stream.Position = _positions[index];
        stream.Write(bytes);
        _positions[index] = end;
    }

    public int ReadInt32(XBlockAddress address)
    {
        int index = GetBlockIndex(address.BlockType);
        int offset = address.Offset;
        int writtenLength = _materializedLengths[index];
        if (offset < 0 || offset > writtenLength - sizeof(int))
            throw new InvalidDataException($"Cannot read int32 at {address}; block {address.BlockType} has 0x{writtenLength:X} materialized byte(s).");

        MemoryStream stream = _streams[index];
        if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            throw new InvalidOperationException($"Unable to inspect block {address.BlockType} bytes.");

        return BinaryPrimitives.ReadInt32BigEndian(segment.AsSpan(offset, sizeof(int)));
    }

    public byte ReadByte(XBlockAddress address)
    {
        int index = GetBlockIndex(address.BlockType);
        int offset = address.Offset;
        int writtenLength = _materializedLengths[index];
        if (offset < 0 || offset >= writtenLength)
            throw new InvalidDataException($"Cannot read byte at {address}; block {address.BlockType} has 0x{writtenLength:X} materialized byte(s).");

        MemoryStream stream = _streams[index];
        if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            throw new InvalidOperationException($"Unable to inspect block {address.BlockType} bytes.");

        return segment[offset];
    }

    public string FormatByteWindow(XBlockAddress address, int bytesBefore, int bytesAfter)
    {
        if (bytesBefore < 0)
            throw new ArgumentOutOfRangeException(nameof(bytesBefore));
        if (bytesAfter < 0)
            throw new ArgumentOutOfRangeException(nameof(bytesAfter));

        int index = GetBlockIndex(address.BlockType);
        int writtenLength = _materializedLengths[index];
        int offset = address.Offset;
        if (offset < 0 || offset >= writtenLength)
            return $"{address} outside materialized length 0x{writtenLength:X}";

        MemoryStream stream = _streams[index];
        if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            throw new InvalidOperationException($"Unable to inspect block {address.BlockType} bytes.");

        int start = Math.Max(0, offset - bytesBefore);
        int end = Math.Min(writtenLength, offset + bytesAfter);
        ReadOnlySpan<byte> bytes = segment.AsSpan(start, end - start);

        var parts = new List<string>();
        for (int i = 0; i < bytes.Length; i += sizeof(int))
        {
            int currentOffset = start + i;
            int count = Math.Min(sizeof(int), bytes.Length - i);
            string marker = currentOffset <= offset && offset < currentOffset + count ? "*" : string.Empty;
            parts.Add($"{marker}{currentOffset:X8}:{Convert.ToHexString(bytes.Slice(i, count))}");
        }

        return $"{address.BlockType}:0x{offset:X} len=0x{writtenLength:X} {string.Join(' ', parts)}";
    }

    public void WriteInt32(XBlockAddress address, int value)
    {
        int index = GetBlockIndex(address.BlockType);
        int offset = address.Offset;
        int end = checked(offset + sizeof(int));
        EnsureLength(index, end);

        MemoryStream stream = _streams[index];
        stream.Position = offset;
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    public byte[] GetBytes(XFileBlockType block)
    {
        int index = (int)block;
        if (index < 0 || index >= _streams.Length)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Invalid XFile block.");

        return GetWrittenBytes(index);
    }

    public string DescribePositions()
    {
        if (_positions.Length == 0)
            return "<uninitialized>";

        var parts = new List<string>();
        int count = Math.Min(_positions.Length, (int)XFileBlockType.COUNT);
        for (int i = 0; i < count; i++)
        {
            int declared = i < BlockSizes.Length ? BlockSizes[i] : 0;
            parts.Add($"{(XFileBlockType)i}=0x{_positions[i]:X}/0x{declared:X}");
        }

        return string.Join(", ", parts);
    }

    public void ValidateMaterializedRange(
        XBlockAddress address,
        int byteCount,
        string targetName,
        int rawPointer)
    {
        if (byteCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));

        int index = GetBlockIndex(address.BlockType);
        int offset = address.Offset;
        int writtenLength = _materializedLengths[index];
        long requiredEnd = (long)offset + byteCount;

        if (offset < 0 || offset > writtenLength - byteCount)
        {
            throw new InvalidDataException(
                $"Offset pointer 0x{rawPointer:X8} to {targetName} targets {address}, " +
                $"but block {address.BlockType} only has 0x{writtenLength:X} materialized byte(s); " +
                $"required range is 0x{offset:X}..0x{requiredEnd:X}.");
        }
    }

    public void ValidateMaterializedCString(
        XBlockAddress address,
        string targetName,
        int rawPointer)
    {
        int index = GetBlockIndex(address.BlockType);
        int offset = address.Offset;
        int writtenLength = _materializedLengths[index];

        if (offset < 0 || offset >= writtenLength)
        {
            throw new InvalidDataException(
                $"Offset pointer 0x{rawPointer:X8} to {targetName} targets {address}, " +
                $"but block {address.BlockType} only has 0x{writtenLength:X} materialized byte(s).");
        }

        MemoryStream stream = _streams[index];
        if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            throw new InvalidOperationException($"Unable to inspect block {address.BlockType} bytes for pointer validation.");

        ReadOnlySpan<byte> bytes = segment.AsSpan(0, writtenLength);
        if (bytes[offset..].IndexOf((byte)0) < 0)
        {
            throw new InvalidDataException(
                $"Offset pointer 0x{rawPointer:X8} to {targetName} targets {address}, " +
                $"but no null terminator exists before the end of materialized block {address.BlockType} data at 0x{writtenLength:X}.");
        }
    }

    public IReadOnlyDictionary<XFileBlockType, byte[]> Snapshot()
    {
        var blocks = new Dictionary<XFileBlockType, byte[]>();
        for (int i = 0; i < _streams.Length && i < (int)XFileBlockType.COUNT; i++)
            blocks[(XFileBlockType)i] = GetWrittenBytes(i);

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
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        int declaredLength = BlockSizes[index];
        if (length > declaredLength)
        {
            throw new InvalidOperationException(
                $"Block stream {(XFileBlockType)index} exceeded declared XFile block size: " +
                $"requested length 0x{length:X}, declared length 0x{declaredLength:X}.");
        }

        MemoryStream stream = _streams[index];
        if (stream.Length >= length)
        {
            _materializedLengths[index] = Math.Max(_materializedLengths[index], length);
            return;
        }

        stream.Position = stream.Length;
        stream.Write(new byte[length - stream.Length]);
        _materializedLengths[index] = Math.Max(_materializedLengths[index], length);
    }

    private int GetBlockIndex(XFileBlockType block)
    {
        int index = (int)block;
        if (index < 0 || index >= _positions.Length)
            throw new ArgumentOutOfRangeException(nameof(block), block, "Invalid XFile block.");

        return index;
    }

    private int GetPosition(XFileBlockType block)
    {
        return _positions[GetBlockIndex(block)];
    }

    private byte[] GetWrittenBytes(int index)
    {
        int length = _materializedLengths[index];
        MemoryStream stream = _streams[index];
        if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            return stream.ToArray().AsSpan(0, length).ToArray();

        return segment.AsSpan(0, length).ToArray();
    }

    private readonly record struct StreamBlockFrame(
        XFileBlockType PreviousBlock,
        int PushedBlockPosition);
}
