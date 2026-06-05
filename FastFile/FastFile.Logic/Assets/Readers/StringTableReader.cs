using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class StringTableReader
{
    public static StringTable Read(ref XFileReadContext context)
    {
        var asset = new StringTable
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            ColumnCount = context.ReadInt32(),
            RowCount = context.ReadInt32(),
        };

        asset.StringsPtr = context.ReadPointer<StringTableCell[]>(
            (ref XFileReadContext pointerContext, ZonePointer<StringTableCell[]> pointer) =>
            {
                var valueCount = asset.ColumnCount * asset.RowCount;
                var cells = ReadCells(ref pointerContext, valueCount);
                pointer.SetResult(cells);
            },
            PointerResolutionKind.Direct,
            "StringTable.Cells");

        return asset;
    }

    private static StringTableCell[] ReadCells(ref XFileReadContext context, int count)
    {
        var cells = new StringTableCell[count];

        for (var i = 0; i < count; i++)
            cells[i] = ReadCell(ref context);

        return cells;
    }

    private static StringTableCell ReadCell(ref XFileReadContext context)
    {
        return new StringTableCell
        {
            StringPtr = GenericReader.ReadStringPointer(ref context),
            Hash = context.ReadInt32(),
        };
    }
}
