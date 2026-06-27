using FastFile.Models.Assets.Localize;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.Localize;

public sealed class LocalizeLoader
{
    public LocalizeAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level Localize pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            AlignStream(context, 4);
            return ReadLocalize(cursor, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static LocalizeAsset ReadLocalize(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, LocalizeAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> valuePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != LocalizeAsset.SerializedSize)
            throw new InvalidDataException($"Localize consumed 0x{rootCursor.Offset:X} bytes instead of 0x{LocalizeAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  Localize root source=0x{offset:X} value=0x{valuePointer.Raw:X8} name=0x{namePointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        string? value;
        string? name;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            value = context.PointerReader.LoadXString(cursor, valuePointer);
            name = context.PointerReader.LoadXString(cursor, namePointer);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new LocalizeAsset
        {
            Offset = offset,
            ValuePointer = valuePointer,
            Value = value,
            NamePointer = namePointer,
            Name = name
        };
    }

    private static void AlignStream(
        FastFileLoadContext context,
        int alignment)
    {
        context.Blocks.AlignCurrent(alignment);
    }
}
