namespace FastFile.LogicOLD.Zone.Validation;

public sealed class XStructEvidenceReport(IReadOnlyList<XStructEvidenceItem> structs)
{
    public IReadOnlyList<XStructEvidenceItem> Structs { get; } = structs;

    public IReadOnlyList<XStructEvidenceItem> Verified { get; } =
        [..structs.Where(item => item.IsVerified)];

    public IReadOnlyList<XStructEvidenceItem> Unverified { get; } =
        [..structs.Where(item => !item.IsVerified)];

    public string ToMarkdown()
    {
        var lines = new List<string>
        {
            "# XStruct EBOOT Evidence Report",
            string.Empty,
            $"Verified: {Verified.Count}",
            $"Unverified: {Unverified.Count}",
            string.Empty,
            "## Unverified",
            string.Empty
        };

        foreach (var item in Unverified.OrderBy(item => item.TypeName, StringComparer.Ordinal))
            lines.Add($"- `{item.TypeName}` ({item.Block}, {item.SizeHex})");

        lines.Add(string.Empty);
        lines.Add("## Verified");
        lines.Add(string.Empty);

        foreach (var item in Verified.OrderBy(item => item.TypeName, StringComparer.Ordinal))
        {
            lines.Add($"- `{item.TypeName}` ({item.Block}, {item.SizeHex})");
            foreach (var evidence in item.Evidence)
                lines.Add($"  - {evidence}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
