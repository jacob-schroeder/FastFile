namespace FastFile.Models.Zone;

public sealed record XBlockStreamSnapshot(
    XFILE_BLOCK Block,
    int DeclaredSize,
    byte[] Data)
{
    public int Index => (int)Block;

    public int Length => Data.Length;
}
