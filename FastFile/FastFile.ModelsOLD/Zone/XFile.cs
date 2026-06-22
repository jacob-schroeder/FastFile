namespace FastFile.ModelsOLD.Zone;

public class XFile
{
    public int Size { get; init; }
    public int ExternalSize { get; init; }
    public int[] BlockSize { get; init; } = [];
}