using FastFile.Models.Zone;

namespace FastFile.Models.Data;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class XFilePointerAttribute(PointerResolutionKind resolutionKind) : Attribute
{
    public PointerResolutionKind ResolutionKind { get; } = resolutionKind;
    public XFILE_BLOCK Block { get; init; } = XFILE_BLOCK.MAX_XFILE_COUNT;
    public int Alignment { get; init; }
    public string? CountMember { get; init; }
    public bool SerializedPayloadInBlock { get; init; }
}
