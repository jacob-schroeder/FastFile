namespace UI.Components.Menu;

public sealed class MenuInsightDisplayItem(string name, string summary, string detail = "")
{
    public string Name { get; } = name;

    public string Summary { get; } = summary;

    public string Detail { get; } = detail;

    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

    public override string ToString()
    {
        return Name;
    }
}
