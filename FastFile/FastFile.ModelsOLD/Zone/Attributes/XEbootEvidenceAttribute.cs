namespace FastFile.ModelsOLD.Zone.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, AllowMultiple = true)]
public sealed class XEbootEvidenceAttribute(string address, string trace) : Attribute
{
    public string Address { get; } = address;

    public string Trace { get; } = trace;

    public string? Detail { get; init; }
}
