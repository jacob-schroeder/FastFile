using FastFile.Models.Math;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class MenuDefAsset : BaseAsset
{
    public const int SerializedSize = 0x2f0;

    public WindowDef Window { get; init; } = new();
    public XString FontPointer { get; init; }
    public int Fullscreen { get; init; }
    public int ItemCount { get; init; }
    public int FontIndex { get; init; }
    public IReadOnlyList<int> CursorItems { get; init; } = [];
    public int FadeCycle { get; init; }
    public float FadeClamp { get; init; }
    public float FadeAmount { get; init; }
    public float FadeInAmount { get; init; }
    public float BlurRadius { get; init; }
    public XPointer<MenuEventHandlerSet> OnOpen { get; init; }
    public XPointer<MenuEventHandlerSet> OnCloseRequest { get; init; }
    public XPointer<MenuEventHandlerSet> OnClose { get; init; }
    public XPointer<MenuEventHandlerSet> OnEsc { get; init; }
    public XPointer<ItemKeyHandler> ExecKeys { get; init; }
    public XPointer<Statement> VisibleExpression { get; init; }
    public XString AllowedBinding { get; init; }
    public XString SoundName { get; init; }
    public int ImageTrack { get; init; }
    public Vec4 FocusColor { get; init; } = new();
    public XPointer<Statement> RectXExpression { get; init; }
    public XPointer<Statement> RectYExpression { get; init; }
    public XPointer<Statement> RectWExpression { get; init; }
    public XPointer<Statement> RectHExpression { get; init; }
    public XPointer<XPointer<ItemDefAsset>[]> ItemsPointer { get; init; }
    public IReadOnlyList<MenuTransition> ScaleTransitions { get; init; } = [];
    public IReadOnlyList<MenuTransition> AlphaTransitions { get; init; } = [];
    public IReadOnlyList<MenuTransition> XTransitions { get; init; } = [];
    public IReadOnlyList<MenuTransition> YTransitions { get; init; } = [];
    public XPointer<ExpressionSupportingData> ExpressionData { get; init; }
}
