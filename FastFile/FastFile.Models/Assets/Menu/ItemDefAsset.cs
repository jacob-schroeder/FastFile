using FastFile.Models.Math;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ItemDefAsset : BaseAsset
{
    public const int SerializedSize = 0x1cc;

    public WindowDef Window { get; init; } = new();
    public IReadOnlyList<RectangleDef> TextRect { get; init; } = [];
    public ItemDefType Type { get; init; }
    public int DataType { get; init; }
    public int Align { get; init; }
    public int FontEnum { get; init; }
    public int TextAlignMode { get; init; }
    public float TextAlignX { get; init; }
    public float TextAlignY { get; init; }
    public float TextScale { get; init; }
    public int TextStyle { get; init; }
    public int GameMsgWindowIndex { get; init; }
    public int GameMsgWindowMode { get; init; }
    public XString Text { get; init; }
    public string? TextString { get; set; }
    public int TextSaveGameInfo { get; init; }
    // 0x134: runtime parent menu pointer slot. Load_ItemDef copies this dword in
    // the 0x1cc root, but does not resolve it as an XFile pointer during DB load.
    // Runtime code initializes/consumes this field after load.
    public int RuntimeParentPointer { get; init; }
    public XPointer<MenuEventHandlerSet> MouseEnterText { get; init; }
    public MenuEventHandlerSet? MouseEnterTextSet { get; set; }
    public XPointer<MenuEventHandlerSet> MouseExitText { get; init; }
    public MenuEventHandlerSet? MouseExitTextSet { get; set; }
    public XPointer<MenuEventHandlerSet> MouseEnter { get; init; }
    public MenuEventHandlerSet? MouseEnterSet { get; set; }
    public XPointer<MenuEventHandlerSet> MouseExit { get; init; }
    public MenuEventHandlerSet? MouseExitSet { get; set; }
    public XPointer<MenuEventHandlerSet> Action { get; init; }
    public MenuEventHandlerSet? ActionSet { get; set; }
    public XPointer<MenuEventHandlerSet> Accept { get; init; }
    public MenuEventHandlerSet? AcceptSet { get; set; }
    public XPointer<MenuEventHandlerSet> OnFocus { get; init; }
    public MenuEventHandlerSet? OnFocusSet { get; set; }
    public XPointer<MenuEventHandlerSet> LeaveFocus { get; init; }
    public MenuEventHandlerSet? LeaveFocusSet { get; set; }
    public XString Dvar { get; init; }
    public string? DvarString { get; set; }
    public XString DvarTest { get; init; }
    public string? DvarTestString { get; set; }
    public XPointer<ItemKeyHandler> OnKey { get; init; }
    public ItemKeyHandler? OnKeyHandler { get; set; }
    public XString EnableDvar { get; init; }
    public string? EnableDvarString { get; set; }
    public int DvarFlags { get; init; }
    public XPointer<SoundAliasListAsset> FocusSound { get; init; }
    public float Special { get; init; }
    public IReadOnlyList<int> CursorPos { get; init; } = [];
    public ItemDefData TypeData { get; init; } = new();
    public EditFieldDef? EditField { get; set; }
    public ListBoxDef? ListBox { get; set; }
    public MultiDef? Multi { get; set; }
    public string? DvarEnumName { get; set; }
    public NewsTickerDef? NewsTicker { get; set; }
    public TextScrollDef? TextScroll { get; set; }
    public int ImageTrack { get; init; }
    public int FloatExpressionCount { get; init; }
    public XPointer<ItemFloatExpression[]> FloatExpressions { get; init; }
    public XPointer<Statement> VisibleExpression { get; init; }
    public Statement? VisibleStatement { get; set; }
    public XPointer<Statement> DisabledExpression { get; init; }
    public Statement? DisabledStatement { get; set; }
    public XPointer<Statement> TextExpression { get; init; }
    public Statement? TextStatement { get; set; }
    public XPointer<Statement> MaterialExpression { get; init; }
    public Statement? MaterialStatement { get; set; }
    public Vec4 GlowColor { get; init; } = new();
    public byte DecayActive { get; init; }
    public byte DecayActivePad0 { get; init; }
    public byte DecayActivePad1 { get; init; }
    public byte DecayActivePad2 { get; init; }
    public int FxBirthTime { get; init; }
    public int FxLetterTime { get; init; }
    public int FxDecayStartTime { get; init; }
    public int FxDecayDuration { get; init; }
    public int LastSoundPlayedTime { get; init; }

    public IReadOnlyList<ItemFloatExpression> LoadedFloatExpressions { get; set; } = [];
}

public sealed record ItemDefReference(
    int Index,
    XPointer<ItemDefAsset> Pointer,
    ItemDefAsset? Item);

public sealed class SoundAliasListAsset;
