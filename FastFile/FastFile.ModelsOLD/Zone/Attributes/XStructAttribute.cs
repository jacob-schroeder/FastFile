namespace FastFile.ModelsOLD.Zone.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class XStructAttribute : Attribute
{
    public required XFILE_BLOCK Block { get; init; }
    public required int Size { get; init; }
}