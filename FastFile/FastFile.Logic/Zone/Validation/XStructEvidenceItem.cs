using FastFile.Models.Zone;

namespace FastFile.Logic.Zone.Validation;

public sealed record XStructEvidenceItem(
    string TypeName,
    XFILE_BLOCK Block,
    int Size,
    bool IsVerified,
    IReadOnlyList<string> Evidence)
{
    public string SizeHex => $"0x{Size:X}";
}
