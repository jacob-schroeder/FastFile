using FastFile.ModelsOLD.Data;

namespace FastFile.ModelsOLD.Zone;

public static class XPointer
{
    //possible duplicate.. 
    public static int EncodeOffset(int blockIndex, int offset)
    {
        return (blockIndex << 28) | (offset + 1);
    }
}

public sealed class XPointer<T> : Pointer
{
    public T? Value { get; set; }
}

public class Pointer
{
    public required int Raw { get; init; }

    public required PointerKind Kind { get; init; }

    public required PointerResolutionKind ResolutionKind { get; init; }

    // Where the pointer field lives in the emitted block stream.
    public XBlockAddress? PatchAddress { get; init; }

    // Where the pointed-to data actually lands.
    public XBlockAddress? Address { get; set; }

    public bool IsNull => Kind == PointerKind.Null;
    public bool IsResolved => IsNull || Address is not null;
}