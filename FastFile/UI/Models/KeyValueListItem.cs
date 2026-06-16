namespace UI.Models;

public sealed class KeyValueListItem(
    string key,
    string value,
    BlockStreamNavigationTarget? navigationTarget = null)
{
    public string Key { get; } = key;

    public string Value { get; } = value;

    public BlockStreamNavigationTarget? NavigationTarget { get; } = navigationTarget;

    public string NavigationValue => NavigationTarget?.ReplaceOffsetLabel(Value) ?? Value;

    public bool HasNavigationTarget => NavigationTarget is not null;

    public bool HasNoNavigationTarget => NavigationTarget is null;
}
