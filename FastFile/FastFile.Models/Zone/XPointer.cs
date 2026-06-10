using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public sealed class XPointer<T>
{
    public int Raw { get; init; }

    public PointerKind Kind { get; init; }

    public XBlockAddress? Address { get; set; }
    
    public XBlockAddress? PatchAddress { get; set; }

    public T? Value { get; set; }

    public bool IsResolved => Address is not null || Kind == PointerKind.Null;
}