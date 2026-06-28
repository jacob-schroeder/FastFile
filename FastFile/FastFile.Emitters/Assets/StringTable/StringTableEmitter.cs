using FastFile.Models.Assets.StringTable;
using FastFile.Models.Codecs;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.StringTable;

public sealed class StringTableEmitter : IXAssetEmitter<StringTableAsset>
{
    public IXAssetCodecContract Contract => StringTableCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, StringTableAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        string name = asset.Name ?? throw new InvalidDataException("StringTable name is required for inline PS3 emission.");
        int cellCount = asset.CellCount;
        if (asset.ColumnCount < 0 || asset.RowCount < 0 || (long)asset.ColumnCount * asset.RowCount > 0x100000)
            throw new InvalidDataException($"StringTable dimensions {asset.ColumnCount}x{asset.RowCount} are outside the loader-validated range.");

        if (asset.Cells.Count != cellCount)
            throw new InvalidDataException($"StringTable has {asset.Cells.Count} cell(s), but dimensions require {cellCount}.");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress rootAddress = context.Blocks.AllocateCurrent(StringTableAsset.SerializedSize);

            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(asset.ColumnCount);
            context.Source.WriteInt32(asset.RowCount);
            context.Source.WriteInt32(-1);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitInlineXString(context, rootAddress.Add(0x00), name);
                EmitCells(context, rootAddress.Add(0x0C), asset.Cells);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"StringTable emitted source=0x{sourceOffset:X} name='{name}' columns={asset.ColumnCount} rows={asset.RowCount} " +
                $"cells={cellCount} blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitCells(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        IReadOnlyList<StringTableCell> cells)
    {
        context.Blocks.PatchInlinePointerCell(pointerCellAddress, alignment: 4);
        XBlockAddress cellsAddress = context.Blocks.AllocateCurrent(checked(cells.Count * StringTableCell.SerializedSize));

        for (int i = 0; i < cells.Count; i++)
        {
            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(cells[i].Hash);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            string value = cells[i].String ?? throw new InvalidDataException($"StringTable cell {i} string is required for inline PS3 emission.");
            EmitInlineXString(context, cellsAddress.Add(checked(i * StringTableCell.SerializedSize)), value);
        }
    }

    private static void EmitInlineXString(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        string value)
    {
        context.Blocks.PatchInlinePointerCell(pointerCellAddress);
        context.Blocks.AllocateCurrent(checked(System.Text.Encoding.Latin1.GetByteCount(value) + 1));
        context.Source.WriteCString(value);
    }
}
