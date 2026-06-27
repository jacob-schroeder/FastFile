using FastFile.Models.Assets.StringTable;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.StringTable;

public sealed class StringTableLoader
{
    public StringTableAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level StringTable pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            AlignStream(cursor, context, 4);
            return ReadStringTable(cursor, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static StringTableAsset ReadStringTable(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, StringTableAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        int columnCount = rootCursor.ReadInt32();
        int rowCount = rootCursor.ReadInt32();
        XPointer<StringTableCell[]> cellsPointer = ReadPointer<StringTableCell[]>(rootCursor, context);

        if (rootCursor.Offset != StringTableAsset.SerializedSize)
            throw new InvalidDataException($"StringTable consumed 0x{rootCursor.Offset:X} bytes instead of 0x{StringTableAsset.SerializedSize:X}.");

        if (columnCount < 0 || rowCount < 0 || (long)columnCount * rowCount > 0x100000)
        {
            throw new InvalidDataException(
                $"StringTable at source 0x{offset:X} has invalid dimensions {columnCount}x{rowCount}; " +
                $"name=0x{namePointer.Raw:X8}, cells=0x{cellsPointer.Raw:X8}.");
        }

        context.Diagnostics.Trace(
            $"  StringTable root source=0x{offset:X} name=0x{namePointer.Raw:X8} columns={columnCount} rows={rowCount} " +
            $"cells=0x{cellsPointer.Raw:X8} cellCount={checked(columnCount * rowCount)} blocks={context.Blocks.DescribePositions()}");

        string? name;
        IReadOnlyList<StringTableCell> cells;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            cells = ReadCells(cursor, cellsPointer.Untyped, checked(columnCount * rowCount), context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new StringTableAsset
        {
            Offset = offset,
            NamePointer = namePointer,
            Name = name,
            ColumnCount = columnCount,
            RowCount = rowCount,
            CellsPointer = cellsPointer,
            Cells = cells
        };
    }

    private static IReadOnlyList<StringTableCell> ReadCells(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative StringTable cell count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * StringTableCell.SerializedSize), "StringTableCell[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        context.Diagnostics.Trace(
            $"    StringTable.cells table source=0x{cursor.Offset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] cellBytes = context.Blocks.Load(cursor, checked(count * StringTableCell.SerializedSize), out XBlockAddress cellsAddress);
        var cellCursor = new FastFileCursor(cellBytes, cellsAddress);
        var cells = new StringTableCell[count];

        for (int i = 0; i < cells.Length; i++)
        {
            int rowStart = cellCursor.Offset;
            XPointer<string> stringPointer = ReadXStringPointer(cellCursor, context);
            int hash = cellCursor.ReadInt32();

            if (cellCursor.Offset - rowStart != StringTableCell.SerializedSize)
                throw new InvalidDataException($"StringTableCell consumed 0x{cellCursor.Offset - rowStart:X} bytes instead of 0x{StringTableCell.SerializedSize:X}.");

            string? value = context.PointerReader.LoadXString(cursor, stringPointer);
            cells[i] = new StringTableCell
            {
                StringPointer = stringPointer,
                String = value,
                Hash = hash
            };
        }

        return cells;
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return context.PointerReader.ReadPointer<T>(cursor, XPointerResolutionMode.Direct);
    }

    private static XPointer<string> ReadXStringPointer(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return context.PointerReader.ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static void AlignStream(
        FastFileCursor cursor,
        FastFileLoadContext context,
        int alignment)
    {
        context.Blocks.AlignCurrent(alignment);
    }
}
