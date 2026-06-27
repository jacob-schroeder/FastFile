using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class MultiDef
{
    public const int SerializedSize = 0x188;
    public const int EntryCapacity = 32;

    public IReadOnlyList<XPointer<string>> DvarList { get; init; } = [];
    public IReadOnlyList<XPointer<string>> DvarStr { get; init; } = [];
    public IReadOnlyList<float> DvarValue { get; init; } = [];
    public int Count { get; init; }
    public int StrDef { get; init; }
}
