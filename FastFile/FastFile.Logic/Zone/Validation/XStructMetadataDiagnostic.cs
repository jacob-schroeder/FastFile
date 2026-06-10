namespace FastFile.Logic.Zone.Validation;

public enum XStructMetadataSeverity
{
    Warning,
    Error
}

public sealed record XStructMetadataDiagnostic(
    XStructMetadataSeverity Severity,
    string TypeName,
    string? MemberName,
    string Message)
{
    public override string ToString()
    {
        var member = string.IsNullOrWhiteSpace(MemberName)
            ? TypeName
            : $"{TypeName}.{MemberName}";

        return $"{Severity}: {member}: {Message}";
    }
}
