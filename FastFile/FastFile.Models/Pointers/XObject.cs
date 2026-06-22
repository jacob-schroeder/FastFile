using FastFile.Models.Zone;

namespace FastFile.Models.Pointers;

public readonly record struct XObject<T>(
    T Value,
    XBlockAddress Address);