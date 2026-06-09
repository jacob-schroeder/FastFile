namespace UI.Models;

public sealed class TechsetTechniqueDisplayItem(
    string slotText,
    string name,
    string pointer,
    string flags,
    string passCount,
    string passesIndicator,
    TechsetPassDisplayItem[] passes)
{
    public string SlotText { get; } = slotText;

    public string Name { get; } = name;

    public string Pointer { get; } = pointer;

    public string Flags { get; } = flags;

    public string PassCount { get; } = passCount;

    public string PassesIndicator { get; } = passesIndicator;

    public TechsetPassDisplayItem[] Passes { get; } = passes;

    public bool HasPasses => Passes.Length > 0;

    public bool HasNoPasses => Passes.Length == 0;
}
