using FastFile.Models.Zone;

namespace FastFile.Runtime.Pointers;

public sealed class PointerResolutionTable
{
    private readonly Dictionary<int, XBlockAddress> _resolvedPointers = new();

    public void Register(int rawPointerValue, XBlockAddress address)
    {
        _resolvedPointers[rawPointerValue] = address;
    }

    public bool TryGet(int rawPointerValue, out XBlockAddress address)
    {
        return _resolvedPointers.TryGetValue(rawPointerValue, out address);
    }
}
