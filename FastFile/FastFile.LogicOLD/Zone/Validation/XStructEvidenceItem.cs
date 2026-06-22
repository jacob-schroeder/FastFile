using FastFile.ModelsOLD.Zone;

namespace FastFile.LogicOLD.Zone.Validation;

public sealed record XStructEvidenceItem(
    string TypeName,
    XFILE_BLOCK Block,
    int Size,
    bool IsVerified,
    IReadOnlyList<string> Evidence)
{
    public string SizeHex => $"0x{Size:X}";
}
