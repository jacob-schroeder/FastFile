namespace FastFile.ModelsOLD.Zone.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class XFieldAttribute : Attribute
{
    public required int Offset { get; init; }
    public int Count { get; init; }
}
