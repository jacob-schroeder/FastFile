using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Localize;

public class LocalizeEntry() : BaseAsset(XAssetType.Localize)
{
    public DirectPointer<string> ValuePtr { get; set; }
    public string Value => ValuePtr is { IsResolved: true } ? ValuePtr.Result ?? string.Empty : string.Empty;

    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public override string? GetDisplayName => Name;
}
