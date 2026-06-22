namespace FastFile.ModelsOLD.Zone;

public sealed record XBlockStreamSnapshot(
    XFILE_BLOCK Block,
    int DeclaredSize,
    byte[] Data)
{
    public int Index => (int)Block;

    public int Length => Data.Length;
}
