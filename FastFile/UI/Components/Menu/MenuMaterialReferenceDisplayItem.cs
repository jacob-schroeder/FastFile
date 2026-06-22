using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;
using UI.Models;

namespace UI.Components.Menu;

public sealed class MenuMaterialReferenceDisplayItem(
    string key,
    string value,
    MaterialAsset? material = null,
    BlockStreamNavigationTarget? navigationTarget = null)
{
    public string Key { get; } = key;

    public string Value { get; } = value;

    public BlockStreamNavigationTarget? NavigationTarget { get; } = navigationTarget;

    public string NavigationValue => NavigationTarget?.ReplaceOffsetLabel(Value) ?? Value;

    public bool HasNavigationTarget => NavigationTarget is not null;

    public bool HasNoNavigationTarget => NavigationTarget is null;

    public MaterialAsset? Material { get; } = material;

    public bool CanOpenMaterial => Material is not null;
}
