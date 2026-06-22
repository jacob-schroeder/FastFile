using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;

namespace FastFile.Models.Pointers;

public readonly record struct PointerCell(
    XBlockAddress Address,
    int Raw)
{
    public PointerType Type => XPointerCodec.GetType(Raw);
}
