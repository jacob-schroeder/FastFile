using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace UI.Components.Menu;

public sealed class MenuMaterialReferenceDisplayItem(string key, string value, MaterialAsset? material = null)
{
    public string Key { get; } = key;

    public string Value { get; } = value;

    public MaterialAsset? Material { get; } = material;

    public bool CanOpenMaterial => Material is not null;
}
