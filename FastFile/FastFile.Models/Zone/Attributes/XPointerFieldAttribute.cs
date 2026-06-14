using FastFile.Models.Data;

namespace FastFile.Models.Zone.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class XPointerFieldAttribute : Attribute
{
    public required PointerResolutionKind ResolutionKind { get; init; }
    public required XPointerTarget Target { get; init; }
    public PointerResolutionKind ElementResolutionKind { get; init; } = PointerResolutionKind.Unknown;
    public XPointerTarget ElementTarget { get; init; } = XPointerTarget.None;
    public XFILE_BLOCK PayloadBlock { get; init; } = XFILE_BLOCK.LARGE;
    public bool OffsetIsAliasCell { get; init; }
    public bool UseCurrentStream { get; init; }
    public int Alignment { get; init; } = 4;
    public string? CountMember { get; init; }
}
