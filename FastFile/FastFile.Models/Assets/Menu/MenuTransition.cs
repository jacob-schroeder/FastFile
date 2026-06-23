namespace FastFile.Models.Assets.Menu;

public sealed class MenuTransition
{
    public const int SerializedSize = 0x1c;

    public MenuTransitionType TransitionType { get; init; }

    /// <summary>
    /// Xbox PDB names this field targetField. PS3 transition helpers inspected so far select
    /// scale/alpha/x/y by containing array, not by this field.
    /// </summary>
    public int TargetField { get; init; }
    public int StartTime { get; init; }
    public float StartValue { get; init; }
    public float EndValue { get; init; }
    public float Time { get; init; }
    public MenuTransitionEndTrigger EndTriggerType { get; init; }
}

public enum MenuTransitionType : int
{
    TRANS_INACTIVE = 0,
    TRANS_LERP = 1
}

public enum MenuTransitionEndTrigger : int
{
    TRIGGER_NONE = 0,
    TRIGGER_CLOSEMENU = 1
}
