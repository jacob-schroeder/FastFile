namespace FastFile.Models.Assets.Menu;

public sealed class MenuTransition
{
    public int TransitionType { get; init; }
    public int TargetField { get; init; }
    public int StartTime { get; init; }
    public float StartValue { get; init; }
    public float EndValue { get; init; }
    public float Time { get; init; }
    public int EndTriggerType { get; init; }
}
