using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Weapon;

public sealed class XModelAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class FxEffectDefAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class GfxImageAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class TracerDefAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class PhysPresetAsset : BaseAsset
{
    public const int SerializedSize = 0x2c;
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class PhysCollmapAsset : BaseAsset
{
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}

public sealed class XModelSurfsAsset : BaseAsset
{
    public const int SerializedSize = 0x24;
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
}
