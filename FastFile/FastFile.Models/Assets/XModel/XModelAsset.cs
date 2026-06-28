using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.XModel;

public sealed class XModelAsset : BaseAsset
{
    public const int SerializedSize = 0x120;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public byte NumBones { get; init; }
    public byte NumRootBones { get; init; }
    public byte NumSurfs { get; init; }
}

public sealed class XModelSurfsAsset : BaseAsset
{
    public const int SerializedSize = 0x24;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public ushort NumSurfs { get; init; }
}

public sealed class PhysPresetAsset : BaseAsset
{
    public const int SerializedSize = 0x2c;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<string> SndAliasPrefixPointer { get; init; }
    public string? SndAliasPrefix { get; init; }
}

public sealed class PhysCollmapAsset : BaseAsset
{
    public const int SerializedSize = 0x48;
}
