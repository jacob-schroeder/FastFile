using FastFile.Models.Assets.RawFile;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.RawFile;

public sealed class RawFileLoader
{
    public RawFileAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level RawFile pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            AlignStream(cursor, context, 4);
            return ReadRawFile(cursor, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static RawFileAsset ReadRawFile(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, RawFileAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        int compressedLen = rootCursor.ReadInt32();
        int len = rootCursor.ReadInt32();
        XPointer<byte[]> bufferPointer = context.PointerReader.ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != RawFileAsset.SerializedSize)
            throw new InvalidDataException($"RawFile consumed 0x{rootCursor.Offset:X} bytes instead of 0x{RawFileAsset.SerializedSize:X}.");

        int bufferLength = compressedLen != 0 ? compressedLen : checked(len + 1);
        context.Diagnostics.Trace(
            $"  RawFile root source=0x{offset:X} name=0x{namePointer.Raw:X8} compressedLen={compressedLen} len={len} " +
            $"buffer=0x{bufferPointer.Raw:X8} bufferLength={bufferLength} blocks={context.Blocks.DescribePositions()}");

        string? name;
        byte[]? buffer;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            buffer = context.PointerReader.LoadBytes(cursor, bufferPointer.Untyped, bufferLength);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new RawFileAsset
        {
            Offset = offset,
            NamePointer = namePointer,
            Name = name,
            CompressedLen = compressedLen,
            Len = len,
            BufferPointer = bufferPointer,
            Buffer = buffer
        };
    }

    private static void AlignStream(
        FastFileCursor cursor,
        FastFileLoadContext context,
        int alignment)
    {
        context.Blocks.AlignCurrent(alignment);
    }
}
