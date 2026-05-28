namespace UI.Models;

public sealed class KeyValueListItem(string key, string value)
{
    public string Key { get; } = key;

    public string Value { get; } = value;
}