namespace UI.Models;

public class DisplayItem
{
    public int Id { get; set; }

    public string Display { get; set; } = string.Empty;

    public bool IsEditing { get; set; }

    public bool IsReadOnly => !IsEditing;
}
