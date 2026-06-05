using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriterContext
{
    private readonly MemoryStream _stream = new();
    private readonly int[] _blockPositions;
    private readonly Stack<XFileStreamBlockStackEntry> _blockStack = new();
    private readonly Dictionary<XFileBlockAddress, int> _serializedOffsets = new();
    private readonly List<WrittenPointerValueSpan> _writtenPointerValues = new();
    private readonly Dictionary<XFileBlockAddress, XFileBlockAddress> _writtenPointerValuesBySourceStreamAddress = new();
    private readonly Dictionary<XFileBlockAddress, XFileBlockAddress> _writtenAliasCellsBySource = new();
    private readonly List<PendingOffsetPointerField> _pendingOffsetPointerFields = new();
    private readonly XFileInlineWriteQueue _deferredInlineWrites = new();
    private int _activeBlockIndex;
    private bool _deferInlineWrites;

    public XFileWriterContext(XFile header)
    {
        var blockCount = Math.Max((int)XFILE_BLOCK.MAX_XFILE_COUNT, header.BlockSize.Length);
        _blockPositions = new int[blockCount];
        _blockPositions[(int)XFILE_BLOCK.TEMP] = GetBlockSize(header, XFILE_BLOCK.TEMP);
        _blockPositions[(int)XFILE_BLOCK.LARGE] = XFileWriteRules.Ps3LargeBlockInitialOffset;
        _activeBlockIndex = (int)XFILE_BLOCK.TEMP;
    }

    public int ActiveBlockIndex => _activeBlockIndex;
    public int Position => _blockPositions[_activeBlockIndex];
    public int SerializedPosition => checked((int)_stream.Position);
    private XFileBlockAddress ActiveAddress => new(ActiveBlockIndex, Position);

    public bool PushInlineWriteDeferral(bool deferInlineWrites = true)
    {
        var previous = _deferInlineWrites;
        _deferInlineWrites = deferInlineWrites;
        return previous;
    }

    public void RestoreInlineWriteDeferral(bool deferInlineWrites)
    {
        _deferInlineWrites = deferInlineWrites;
    }

    public bool TryDeferInlineWrite(Action writer)
    {
        if (!_deferInlineWrites)
            return false;

        _deferredInlineWrites.Add(writer);
        return true;
    }

    public void ResolveDeferredInlineWrites()
    {
        var previous = _deferInlineWrites;
        _deferInlineWrites = false;
        try
        {
            _deferredInlineWrites.Resolve();
        }
        finally
        {
            _deferInlineWrites = previous;
        }
    }

    public void PushStreamBlock(XFILE_BLOCK block)
    {
        PushStreamBlock((int)block);
    }

    public void PushStreamBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= _blockPositions.Length)
            throw new InvalidDataException($"Invalid XFile stream block index {blockIndex:N0}.");

        var previousBlockIndex = _activeBlockIndex;
        _activeBlockIndex = blockIndex;
        _blockStack.Push(new XFileStreamBlockStackEntry(previousBlockIndex, _blockPositions[_activeBlockIndex]));
    }

    public void PopStreamBlock()
    {
        if (_blockStack.Count == 0)
            throw new InvalidDataException("Cannot pop an XFile stream block because the stream block stack is empty.");

        var entry = _blockStack.Pop();
        if (_activeBlockIndex == (int)XFILE_BLOCK.TEMP)
            _blockPositions[_activeBlockIndex] = entry.SavedActivePosition;

        _activeBlockIndex = entry.PreviousBlockIndex;
    }

    public T WithStreamBlock<T>(XFILE_BLOCK block, Func<T> write)
    {
        PushStreamBlock(block);
        try
        {
            return write();
        }
        finally
        {
            PopStreamBlock();
        }
    }

    public void WithStreamBlock(XFILE_BLOCK block, Action write)
    {
        PushStreamBlock(block);
        try
        {
            write();
        }
        finally
        {
            PopStreamBlock();
        }
    }

    public XFileBlockAddress Align(XFileStreamAlignment alignment)
    {
        var byteAlignment = (int)alignment;
        if (byteAlignment <= 0)
            throw new InvalidDataException($"Cannot align XFile block {ActiveBlockIndex:N0} with invalid alignment {byteAlignment:N0}.");

        var remainder = Position % byteAlignment;
        if (remainder != 0)
            WriteZeroes(byteAlignment - remainder);

        return ActiveAddress;
    }

    public XFileBlockAddress Allocate(
        XFILE_BLOCK block,
        XFileStreamAlignment alignment,
        Action write)
    {
        return WithStreamBlock(block, () =>
        {
            Align(alignment);
            var address = ActiveAddress;
            write();
            return address;
        });
    }

    public XFileBlockAddress Reserve(
        XFILE_BLOCK block,
        XFileStreamAlignment alignment,
        int byteCount)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Cannot reserve a negative byte count: {byteCount:N0}.");

        return Allocate(block, alignment, () => WriteZeroes(byteCount));
    }

    public XFileBlockAddress AllocateRoot(Action write)
    {
        return Allocate(XFileWriteRules.RootBlock, XFileWriteRules.StructAlignment, write);
    }

    public XFileBlockAddress AllocateAssetData(Action write)
    {
        return Allocate(XFileWriteRules.AssetDataBlock, XFileWriteRules.StructAlignment, write);
    }

    public XFileBlockAddress AllocateAssetData(XFileStreamAlignment alignment, Action write)
    {
        return Allocate(XFileWriteRules.AssetDataBlock, alignment, write);
    }

    public XFileBlockAddress AllocateCString(string? value)
    {
        return AllocateAssetData(XFileWriteRules.StringAlignment, () => WriteCString(value));
    }

    public XFileBlockAddress AllocateRawFileBuffer(byte[]? value)
    {
        return Allocate(
            XFileWriteRules.RawBulkDataBlock,
            XFileWriteRules.RawFileBufferAlignment,
            () => WriteBytes(value));
    }

    public XFileBlockAddress ReserveInt32()
    {
        var address = ActiveAddress;
        RegisterPatchAddress(address);
        WriteInt32(0);
        return address;
    }

    public XFileBlockAddress ReserveInsertSlot()
    {
        return WithStreamBlock(XFileWriteRules.AssetDataBlock, () =>
        {
            AlignStreamOnly(XFileWriteRules.InsertSlotAlignment);
            var address = ActiveAddress;
            AdvanceActiveBlock(XFileWriteRules.PointerSize);
            return address;
        });
    }

    public void WriteNullPointer()
    {
        WriteInt32(0);
    }

    public void WriteInlinePointerMarker()
    {
        WriteInt32(-1);
    }

    public void WritePointer(XFileBlockAddress address)
    {
        WriteInt32(address.Raw);
    }

    public void WritePointerAt(XFileBlockAddress pointerField, XFileBlockAddress target)
    {
        WriteInt32At(pointerField, target.Raw);
    }

    public void WritePointer<T>(
        ZonePointer<T>? pointer,
        XFILE_BLOCK targetBlock,
        XFileStreamAlignment alignment,
        XFilePointerWriter<T> writer)
    {
        if (pointer is null || pointer.Result is null)
        {
            WriteNullPointer();
            return;
        }

        var target = Allocate(targetBlock, alignment, () =>
        {
            RegisterMaterializedPointerValue(pointer);
            writer(this, pointer);
        });
        WritePointer(target);
    }

    public void WritePointerRaw<T>(
        ZonePointer<T>? pointer,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        if (pointer is null)
        {
            WriteNullPointer();
            return;
        }

        if (pointer.Kind == PointerKind.Null)
        {
            WriteNullPointer();
            return;
        }

        var proofedKind = EbootPointerRules.Resolve(
            pointer,
            resolutionKind,
            fieldPath,
            out var normalizedFieldPath);
        pointer.SetResolutionKind(proofedKind, string.IsNullOrEmpty(normalizedFieldPath) ? fieldPath : normalizedFieldPath);

        if (pointer.Kind == PointerKind.Offset)
        {
            WriteOffsetPointer(pointer);
            return;
        }

        if (pointer.Result is not null)
        {
            WriteInt32(pointer.Raw);
            return;
        }

        WriteNullPointer();
    }

    public void WritePointerRaw(
        Pointer? pointer,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        if (pointer is null)
        {
            WriteNullPointer();
            return;
        }

        if (pointer.Kind == PointerKind.Null)
        {
            WriteNullPointer();
            return;
        }

        var proofedKind = EbootPointerRules.Resolve(
            pointer,
            resolutionKind,
            fieldPath,
            out var normalizedFieldPath);
        pointer.SetResolutionKind(proofedKind, string.IsNullOrEmpty(normalizedFieldPath) ? fieldPath : normalizedFieldPath);

        if (pointer.Kind == PointerKind.Offset)
            WriteOffsetPointer(pointer);
        else
            WriteInt32(pointer.Raw);
    }

    public void WriteInsertPointer<T>(
        ZonePointer<T>? pointer,
        XFILE_BLOCK targetBlock,
        XFileStreamAlignment alignment,
        XFilePointerWriter<T> writer)
    {
        if (pointer is null || pointer.Result is null)
        {
            WriteNullPointer();
            return;
        }

        WriteInt32(-2);
        ReserveInsertSlot();
        Allocate(targetBlock, alignment, () =>
        {
            RegisterMaterializedPointerValue(pointer);
            writer(this, pointer);
        });
    }

    public void WriteByte(byte value)
    {
        _stream.WriteByte(value);
        AdvanceActiveBlock(1);
    }

    public void WriteBool(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    public void WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        WriteBytes(buffer);
    }

    public void WriteVec4(Vec4 value)
    {
        WriteFloat(value.A);
        WriteFloat(value.R);
        WriteFloat(value.G);
        WriteFloat(value.B);
    }

    public void WriteVec3(Vec3 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
        WriteFloat(value.Z);
    }

    public void WriteBounds(Bounds value)
    {
        WriteVec3(value.MidPoint);
        WriteVec3(value.HalfSize);
    }

    public void WriteBytes(byte[]? value)
    {
        if (value is not null)
            WriteBytes(value.AsSpan());
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _stream.Write(value);
        AdvanceActiveBlock(value.Length);
    }

    public void WriteCString(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            WriteBytes(Encoding.Latin1.GetBytes(value));

        WriteByte(0);
    }

    public void WriteZeroes(int count)
    {
        if (count == 0)
            return;

        WriteBytes(new byte[count]);
    }

    public void WriteInt32At(XFileBlockAddress address, int value)
    {
        if (!_serializedOffsets.TryGetValue(address, out var serializedOffset))
        {
            throw new InvalidDataException(
                $"Cannot patch Int32 for XFile block {address.BlockIndex:N0} offset 0x{address.Offset:X}; no serialized write location was registered.");
        }

        WriteInt32AtSerializedOffset(serializedOffset, value);
    }

    public int[] GetBlockSizes()
    {
        return _blockPositions.ToArray();
    }

    public IReadOnlyList<byte[]> GetBlockBytes(int[] blockSizes)
    {
        var bytes = new byte[_blockPositions.Length][];

        for (var i = 0; i < _blockPositions.Length; i++)
        {
            var size = i < blockSizes.Length ? blockSizes[i] : _blockPositions[i];
            bytes[i] = new byte[Math.Max(size, _blockPositions[i])];
        }

        return bytes;
    }

    public byte[] ToArray()
    {
        return _stream.ToArray();
    }

    private void RegisterPatchAddress(XFileBlockAddress address)
    {
        _serializedOffsets[address] = SerializedPosition;
    }

    private void AdvanceActiveBlock(int byteCount)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Cannot advance XFile block by a negative byte count: {byteCount:N0}.");

        _blockPositions[_activeBlockIndex] += byteCount;
    }

    public void AlignStreamOnly(XFileStreamAlignment alignment)
    {
        var byteAlignment = (int)alignment;
        if (byteAlignment <= 0)
            throw new InvalidDataException($"Cannot align XFile block {ActiveBlockIndex:N0} with invalid alignment {byteAlignment:N0}.");

        var remainder = Position % byteAlignment;
        if (remainder != 0)
            AdvanceActiveBlock(byteAlignment - remainder);
    }

    private void WriteInt32AtSerializedOffset(int offset, int value)
    {
        if (offset < 0 || offset + 4 > _stream.Length)
            throw new InvalidDataException($"Cannot patch Int32 at serialized XFile stream offset 0x{offset:X}.");

        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        var previous = _stream.Position;
        _stream.Position = offset;
        _stream.Write(buffer);
        _stream.Position = previous;
    }

    private static int GetBlockSize(XFile header, XFILE_BLOCK block)
    {
        var index = (int)block;
        return index >= 0 && index < header.BlockSize.Length
            ? header.BlockSize[index]
            : 0;
    }

}
