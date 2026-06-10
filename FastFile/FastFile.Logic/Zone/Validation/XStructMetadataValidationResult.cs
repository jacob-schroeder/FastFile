namespace FastFile.Logic.Zone.Validation;

public sealed class XStructMetadataValidationResult(IReadOnlyList<XStructMetadataDiagnostic> diagnostics)
{
    public IReadOnlyList<XStructMetadataDiagnostic> Diagnostics { get; } = diagnostics;

    public IReadOnlyList<XStructMetadataDiagnostic> Errors { get; } =
        [..diagnostics.Where(diagnostic => diagnostic.Severity == XStructMetadataSeverity.Error)];

    public IReadOnlyList<XStructMetadataDiagnostic> Warnings { get; } =
        [..diagnostics.Where(diagnostic => diagnostic.Severity == XStructMetadataSeverity.Warning)];

    public bool IsValid => Errors.Count == 0;
}
