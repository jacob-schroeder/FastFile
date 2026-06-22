namespace UI.Models;

using FastFile.ModelsOLD.Assets;
using FastFile.ModelsOLD.Zone;

public class DisplayItem
{
    public int Id { get; set; }

    public string Display { get; set; } = string.Empty;

    public BaseAsset? Asset { get; set; }

    public XAssetType? AssetType { get; set; }

    public bool IsEditing { get; set; }

    public bool IsReadOnly => !IsEditing;
}
