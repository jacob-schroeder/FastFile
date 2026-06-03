using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class StringTableWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        var table = (StringTable)asset;
        GenericWriter.WriteStringPointer(context, table.NamePtr);
        context.WriteInt32(table.ColumnCount);
        context.WriteInt32(table.RowCount);
        context.WritePointer(table.StringsPtr, WriteCells);
    }

    private static void WriteCells(
        ZoneWriterContext context,
        ZonePointer<StringTableCell[]> pointer)
    {
        foreach (var cell in pointer.Result ?? [])
            WriteCell(context, cell);
    }

    private static void WriteCell(ZoneWriterContext context, StringTableCell cell)
    {
        GenericWriter.WriteStringPointer(context, cell.StringPtr);
        context.WriteInt32(cell.Hash);
    }
}
