using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Data;
using FastFile.Models.Utils;

namespace FastFile.Logic.Zone;

internal delegate void ZoneValueWriter<in T>(ZoneWriterContext context, T value);

internal delegate void ZonePointerWriter<T>(
    ZoneWriterContext context,
    ZonePointer<T> pointer);

internal interface IQueuedZonePointerWriter
{
    string Name { get; }
    void Write(ZoneWriterContext context);
}

internal sealed class QueuedZonePointerWriter<T>(
    ZonePointer<T> pointer,
    ZonePointerWriter<T> writer) : IQueuedZonePointerWriter
{
    public string Name => typeof(T).Name;

    public void Write(ZoneWriterContext context)
    {
        pointer.SetOffset(context.Position);
        writer(context, pointer);
    }
}

internal sealed class ZoneWriterContext
{
    private readonly MemoryStream _stream;
    private readonly List<IQueuedZonePointerWriter> _inlineWriters = new();
    private readonly List<IQueuedZonePointerWriter> _deferredWriters = new();
    private bool _deferInlinePointers;

    public ZoneWriterContext()
    {
        _stream = new MemoryStream();
    }

    public int Position => checked((int)_stream.Position);

    public bool PushInlinePointerDeferral(bool deferInlinePointers = true)
    {
        var previous = _deferInlinePointers;
        _deferInlinePointers = deferInlinePointers;
        return previous;
    }

    public void RestoreInlinePointerDeferral(bool deferInlinePointers)
    {
        _deferInlinePointers = deferInlinePointers;
    }

    public void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    public void WriteBool(bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    public void WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        _stream.Write(buffer);
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
            _stream.Write(value);
    }

    public void WriteBytes(ReadOnlySpan<byte> value)
    {
        _stream.Write(value);
    }

    public void WriteCString(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _stream.Write(Encoding.Latin1.GetBytes(value));

        _stream.WriteByte(0);
    }

    public void WriteAlignedCString(string? value, int alignment = 4)
    {
        WriteCString(value);
        AlignPosition(alignment);
    }

    public void WritePointer<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer)
    {
        WritePointerCore(pointer, writer, defer: false);
    }

    public void WriteInlinePointerDeferred<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer)
    {
        WritePointerCore(pointer, writer, defer: true);
    }

    public void WritePointer<T>(ZonePointer<T>? pointer)
    {
        WritePointerCore(pointer, (_, _) => { }, defer: false);
    }

    public void WritePointerRaw<T>(ZonePointer<T>? pointer)
    {
        if (pointer is null)
        {
            WriteInt32(0);
            return;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            WriteInt32(pointer.Raw);
            return;
        }

        if (pointer.Result is null)
        {
            WriteInt32(0);
            return;
        }

        WriteInt32(pointer.Raw);
    }

    public void QueuePointer<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer)
    {
        QueuePointerCore(pointer, writer, defer: false);
    }

    public void QueueInlinePointerDeferred<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer)
    {
        QueuePointerCore(pointer, writer, defer: true);
    }

    private void WritePointerCore<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer,
        bool defer)
    {
        if (pointer is null)
        {
            WriteInt32(0);
            return;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            WriteInt32(pointer.Raw);
            return;
        }

        if (pointer.Result is null)
        {
            WriteInt32(0);
            return;
        }

        WriteInt32(pointer.Raw);

        if (pointer.Kind == PointerKind.Inline)
            AddInlineWriter(pointer, writer, defer);
    }

    private void QueuePointerCore<T>(
        ZonePointer<T>? pointer,
        ZonePointerWriter<T> writer,
        bool defer)
    {
        if (pointer is null || pointer.Kind != PointerKind.Inline || pointer.Result is null)
            return;

        AddInlineWriter(pointer, writer, defer);
    }

    public void ResolveQueued()
    {
        var resolvedCount = 0;

        while (_inlineWriters.Count > 0 || _deferredWriters.Count > 0)
        {
            if (++resolvedCount > 1_000_000)
                throw new InvalidDataException($"Stopped writing inline zone pointers after {resolvedCount:N0} entries at zone offset 0x{Position:X8} ({Position:N0}).");

            if (_inlineWriters.Count == 0)
            {
                _inlineWriters.AddRange(_deferredWriters);
                _deferredWriters.Clear();
            }

            var writer = _inlineWriters[0];
            _inlineWriters.RemoveAt(0);

            var olderSiblingCount = _inlineWriters.Count;
            var olderDeferredCount = _deferredWriters.Count;
            writer.Write(this);

            var nestedCount = _inlineWriters.Count - olderSiblingCount;
            var nestedDeferredCount = _deferredWriters.Count - olderDeferredCount;
            var deferredInsertIndex = 0;
            if (nestedCount > 0 && olderSiblingCount > 0)
            {
                var nestedWriters = _inlineWriters.GetRange(olderSiblingCount, nestedCount);
                _inlineWriters.RemoveRange(olderSiblingCount, nestedCount);

                _inlineWriters.InsertRange(0, nestedWriters);
                deferredInsertIndex = nestedWriters.Count;
            }

            if (nestedDeferredCount > 0)
            {
                var nestedDeferredWriters = _deferredWriters.GetRange(olderDeferredCount, nestedDeferredCount);
                _deferredWriters.RemoveRange(olderDeferredCount, nestedDeferredCount);

                _inlineWriters.InsertRange(deferredInsertIndex, nestedDeferredWriters);
            }
        }
    }

    public void AlignPosition(int alignment)
    {
        if (alignment <= 0)
            throw new InvalidDataException($"Cannot align zone position with invalid alignment {alignment:N0}.");

        var remainder = Position % alignment;
        if (remainder == 0)
            return;

        WriteBytes(new byte[alignment - remainder]);
    }

    public byte[] ToArray()
    {
        return _stream.ToArray();
    }

    private void AddInlineWriter<T>(
        ZonePointer<T> pointer,
        ZonePointerWriter<T> writer,
        bool defer)
    {
        var queuedWriter = new QueuedZonePointerWriter<T>(pointer, writer);
        if (defer || _deferInlinePointers)
            _deferredWriters.Add(queuedWriter);
        else
            _inlineWriters.Add(queuedWriter);
    }
}
