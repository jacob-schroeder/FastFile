using FastFile.ModelsOLD.Assets.Effects;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.ImpactFx;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class FxImpactTable() : BaseAsset(XAssetType.ImpactFx)
{
    public const int RootSize = 0x08;
    public const int EntryCount = 15;

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?> NamePtr { get; set; } = null!;
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(EntryCount))]
    public XPointer<FxImpactEntry[]> Entries { get; set; } = null!;

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"ImpactFx 0x{Offset:X8}"
        : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class FxImpactEntry
{
    public const int RootSize = 0x8C;
    public const int EffectDefs0Count = 31;
    public const int EffectDefs7CCount = 4;

    public int Offset { get; set; }

    [XField(Offset = 0x00, Count = EffectDefs0Count)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<FxEffectDef>[] EffectDefs0 { get; set; } = new XPointer<FxEffectDef>[EffectDefs0Count];

    [XField(Offset = 0x7C, Count = EffectDefs7CCount)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<FxEffectDef>[] EffectDefs7C { get; set; } = new XPointer<FxEffectDef>[EffectDefs7CCount];
}
