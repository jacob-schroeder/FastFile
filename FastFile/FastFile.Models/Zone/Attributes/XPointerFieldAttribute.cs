using FastFile.Models.Data;

namespace FastFile.Models.Zone.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class XPointerFieldAttribute : Attribute
{
    public required PointerResolutionKind ResolutionKind { get; init; }
    public required XPointerTarget Target { get; init; }
    public XFILE_BLOCK PayloadBlock { get; init; } = XFILE_BLOCK.LARGE;
    public string? CountMember { get; init; }
}

