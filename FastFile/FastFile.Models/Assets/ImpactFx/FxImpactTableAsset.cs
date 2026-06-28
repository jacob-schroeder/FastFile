using FastFile.Models.Assets.Fx;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.ImpactFx;

public sealed class FxImpactTableAsset : BaseAsset
{
    public const int SerializedSize = 0x08;
    public const int EntryCount = 15;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<FxImpactEntry[]> EntriesPointer { get; init; }
    public IReadOnlyList<FxImpactEntry> Entries { get; init; } = [];
}

public sealed class FxImpactEntry
{
    public const int SerializedSize = 0x8C;
    public const int SurfaceEffectCount = 31;
    public const int FleshEffectCount = 4;

    public int Offset { get; init; }
    public IReadOnlyList<XPointer<FxEffectDefAsset>> SurfaceEffectPointers { get; init; } = [];
    public IReadOnlyList<FxEffectDefAsset?> SurfaceEffects { get; init; } = [];
    public IReadOnlyList<XPointer<FxEffectDefAsset>> FleshEffectPointers { get; init; } = [];
    public IReadOnlyList<FxEffectDefAsset?> FleshEffects { get; init; } = [];
}
