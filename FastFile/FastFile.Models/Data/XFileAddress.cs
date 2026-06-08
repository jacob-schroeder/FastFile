namespace FastFile.Models.Data;

public readonly record struct XFileAddress(int BlockIndex, int Offset)
{
    public static readonly XFileAddress Null = new(-1, -1);

    public bool IsNull => BlockIndex < 0 || Offset < 0;

    public int Raw => IsNull ? 0 : Pointer.EncodeOffset(BlockIndex, Offset);
}
