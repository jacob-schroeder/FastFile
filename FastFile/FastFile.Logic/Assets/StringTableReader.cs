using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class StringTableReader
{
    public static StringTable Read(ref ZoneReadContext context)
    {
        var asset = new StringTable
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            ColumnCount = context.ReadInt32(),
            RowCount = context.ReadInt32(),
        };

        asset.StringsPtr = context.ReadPointer<StringTableCell[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<StringTableCell[]> pointer) =>
            {
                var valueCount = asset.ColumnCount * asset.RowCount;
                var cells = ReadCells(ref pointerContext, valueCount);
                pointer.SetResult(cells);
            });

        return asset;
    }

    private static StringTableCell[] ReadCells(ref ZoneReadContext context, int count)
    {
        var cells = new StringTableCell[count];

        for (var i = 0; i < count; i++)
            cells[i] = ReadCell(ref context);

        return cells;
    }

    private static StringTableCell ReadCell(ref ZoneReadContext context)
    {
        return new StringTableCell
        {
            StringPtr = GenericReader.ReadStringPointer(ref context),
            Hash = context.ReadInt32(),
        };
    }
}
