namespace UI.Models;

using FastFile.Models.Zone;

public class DisplayItem
{
    public int Id { get; set; }

    public string Display { get; set; } = string.Empty;

    public XAssetType? AssetType { get; set; }

    public bool IsEditing { get; set; }

    public bool IsReadOnly => !IsEditing;
}
