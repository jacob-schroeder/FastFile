using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Runtime.IO;

namespace FastFile.Runtime.Pointers;

public sealed class XFilePointerReader
{
    public XPointerReference ReadCell(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode = XPointerOffsetMode.None)
    {
        return XPointerReference.FromRaw(cursor.ReadInt32(), offsetMode);
    }

    public XPointerReference FromRaw(
        int raw,
        XPointerOffsetMode offsetMode = XPointerOffsetMode.None)
    {
        return XPointerReference.FromRaw(raw, offsetMode);
    }

    public XPointer<T> FromRaw<T>(
        int raw,
        XPointerResolutionMode resolutionMode = XPointerResolutionMode.None)
    {
        return new XPointer<T>(raw, resolutionMode);
    }

    public XPointer<T> FromRaw<T>(
        int raw,
        XPointerOffsetMode resolutionMode)
    {
        return new XPointer<T>(raw, resolutionMode);
    }

    public bool HasInlinePayload(XPointerReference pointer)
    {
        return pointer.Type is PointerType.Inline or PointerType.Insert;
    }

    public T? ReadNullableInline<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        Func<T> readPayload,
        int alignment = 0)
        where T : class
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (!HasInlinePayload(pointer))
            return null;

        AlignIfNeeded(cursor, alignment);
        return readPayload();
    }

    public T ReadRequiredInline<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        Func<T> readPayload,
        string ownerName,
        int alignment = 0)
    {
        if (!HasInlinePayload(pointer))
            throw new InvalidDataException($"{ownerName} pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        AlignIfNeeded(cursor, alignment);
        return readPayload();
    }

    public string? ReadCString(FastFileCursor cursor, XPointerReference pointer)
    {
        return ReadNullableInline(cursor, pointer, cursor.ReadCString);
    }

    public byte[]? ReadBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment = 0)
    {
        return ReadNullableInline(cursor, pointer, () => cursor.ReadBytes(byteCount), alignment);
    }

    public void ReadInlinePayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        Action readPayload,
        int alignment = 0)
    {
        if (!HasInlinePayload(pointer))
            return;

        AlignIfNeeded(cursor, alignment);
        readPayload();
    }

    private static void AlignIfNeeded(FastFileCursor cursor, int alignment)
    {
        if (alignment < 0)
            throw new ArgumentOutOfRangeException(nameof(alignment));

        if (alignment > 0)
            cursor.Align(alignment);
    }
}
