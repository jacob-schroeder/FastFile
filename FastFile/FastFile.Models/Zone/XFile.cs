namespace FastFile.Models.Zone;

public class XFile
{
    public int Size { get; set; }
    public int ExternalSize { get; set; }
    public int[] BlockSize { get; set; } = [];
}