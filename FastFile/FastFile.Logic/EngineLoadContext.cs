using FastFile.Models;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Logic.Streams;
using FastFile.Models.Zone;

namespace FastFile.Logic;

public sealed class EngineLoadContext(
    MirroredReadCursor reader,
    XFileBlockStack blocks,
    IReadOnlyDictionary<XFileBlockType, IBlockCursor> blockCursors)
{
    public MirroredReadCursor Reader { get; } = reader;
    public XFileBlockStack Blocks { get; } = blocks;

    public T ReadStruct<T>() where T : struct
    {
        return Reader.ReadStruct<T>();
    }

    public PointerCell TakePointerCell()
    {
        return Reader.TakePointerCell();
    }

    public PointerCell[] Load_PointerArray<T>(int count)
    {
        var cells = new PointerCell[count];

        for (int i = 0; i < count; i++)
        {
            XBlockAddress address = Blocks.ActiveAddress;
            int raw = Reader.ReadInt32();
            cells[i] = new PointerCell(address, raw);
        }

        return cells;
    }

    public void Load_XString(PointerCell value)
    {
        if (!TryPatchPointerToCurrentBlock(value, out _))
            return;

        Load_CString();
    }

    public void Load_CString()
    {
        byte value;

        do
        {
            value = Reader.ReadByte();
        }
        while (value != 0);
    }

    public bool TryPatchPointerToCurrentBlock(
        PointerCell cell,
        out int patchedPointer)
    {
        patchedPointer = 0;
        PointerType type = cell.Type;

        if (type is PointerType.Null or PointerType.Offset)
            return false;

        if (type == PointerType.Insert)
            throw new NotImplementedException("Insert pointer patching needs a proven loader path first.");

        XBlockAddress target = Blocks.ActiveAddress;
        patchedPointer = XPointerCodec.Encode(target);
        blockCursors[cell.Address.BlockType].PatchInt32(cell.Address.Offset, patchedPointer);
        return true;
    }
}
