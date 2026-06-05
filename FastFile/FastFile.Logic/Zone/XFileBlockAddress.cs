using FastFile.Models.Data;

namespace FastFile.Logic.Zone;

public readonly record struct XFileBlockAddress(int BlockIndex, int Offset)
{
    public int Raw => Pointer.EncodeOffset(BlockIndex, Offset);
}
