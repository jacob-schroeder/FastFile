using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Menu.Elements;

public class RectangleDef
{
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; }
    public float H { get; set; }
    public byte HorzAlign { get; set; }
    public byte VertAlign { get; set; }
    public ushort AlignmentPadding { get; set; }
}

public class Window
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public RectangleDef Rect { get; set; }
    public RectangleDef RectClient { get; set; }
    public XPointer<string> GroupPtr { get; set; } // Direct
    public string Group => GroupPtr is { IsResolved: true } ? GroupPtr.Value ?? string.Empty : string.Empty;
    public int Style { get; set; }
    public int Border { get; set; }
    public int OwnerDraw { get; set; }
    public int OwnerDrawFlags { get; set; }
    public float BorderSize { get; set; }
    public int StaticFlags { get; set; }
#if !PC
    public int[] DynamicFlags { get; set; } = new int[4];
#else
    public int[] DynamicFlags { get; set; } = new int[1];
#endif
    public int NextTime { get; set; }
    public Vec4 ForeColor { get; set; }
    public Vec4 BackColor { get; set; }
    public Vec4 BorderColor { get; set; }
    public Vec4 OutlineColor { get; set; }
    public Vec4 DisableColor { get; set; }
    public XPointer<MaterialAsset> Background { get; set; } // Alias
}

public class ExpressionString
{
    public XPointer<string> StringPtr { get; set; } // Direct
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Value ?? string.Empty : string.Empty;
}

public class OperandInternalData
{
    public int IntVal { get; set; }
    public float FloatVal { get; set; }
    public XPointer<string> StringVal { get; set; } // Direct
    public XPointer<Statement> Function { get; set; } // ??
}

public class Operand
{
    public ExpDataType DataType { get; set; }
    public OperandInternalData Internals { get; set; }
}

public class EntryInternalData
{
    public OperationEnum Op { get; set; }
    public Operand Operand { get; set; }
}

public class ExpressionEntry
{
    public int Type { get; set; }
    public EntryInternalData Data { get; set; }
}

public class SetLocalVarData
{
    public XPointer<string> LocalVarName { get; set; } // Direct
    public string LocalVar => LocalVarName is { IsResolved: true } ? LocalVarName.Value ?? string.Empty : string.Empty;
    public XPointer<Statement> Expression { get; set; } // ??
}

public class ConditionalScript
{
    public XPointer<MenuEventHandlerSet> EventHandlerSet { get; set; } // ?
    public XPointer<Statement> EventExpression { get; set; } // ?
}

public class Statement
{
    public int NumEntries { get; set; }
    public XPointer<ExpressionEntry[]> Entries { get; set; } // ?
    public XPointer<ExpressionSupportingData> SupportingData { get; set; } // ?
    public int LastExecuteTime { get; set; }
    public Operand LastResult { get; set; }
}

public class ItemFloatExpression
{
    public ItemFloatExpressionTarget Target { get; set; }
    public XPointer<Statement> Expression { get; set; } // ?
}

public class EventData
{
    public int Raw { get; set; }
    public XPointer<string> UnconditionalScript { get; set; } // Direct
    public XPointer<ConditionalScript> ConditionalScript { get; set; } // ?
    public XPointer<MenuEventHandlerSet> ElseScript { get; set; } // ?
    public XPointer<SetLocalVarData> SetLocalVarData { get; set; } // ?
}

public class MenuEventHandler
{
    public EventData EventData { get; set; }
    public byte EventType { get; set; }
    public byte EventTypePadding0 { get; set; }
    public byte EventTypePadding1 { get; set; }
    public byte EventTypePadding2 { get; set; }
}

public class MenuEventHandlerSet
{
    public int EventHandlerCount { get; set; }
    public XPointer<XPointer<MenuEventHandler>[]> EventHandlers { get; set; } // ?
}

public class ItemKeyHandler
{
    public int Key { get; set; }
    public XPointer<MenuEventHandlerSet> Action { get; set; } // ?
    public XPointer<ItemKeyHandler> Next { get; set; } // ?
}

public class NewsTickerDef
{
    public int FeedId { get; set; }
    public int Speed { get; set; }
    public int Spacing { get; set; }
    public int LastTime { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public float X { get; set; }
}

public class ColumnInfo
{
    public int Pos { get; set; }
    public int Width { get; set; }
    public int MaxChars { get; set; }
    public int Alignment { get; set; }
}

public class ListBoxDef
{
#if !PC
    public int[] StartPos { get; set; } = new int[4];
    public int[] EndPos { get; set; } = new int[4];
    public int DrawPadding { get; set; }
#else
    public int[] Unknown { get; set; } = new int[4];
#endif
    public float ElementWidth { get; set; }
    public float ElementHeight { get; set; }
    public int ElementStyle { get; set; }
    public int NumColumns { get; set; }
    public ColumnInfo[] ColumnInfo { get; set; } = new ColumnInfo[16];
    public XPointer<MenuEventHandlerSet> DoubleClick { get; set; } // ?
    public int NotSelectable { get; set; }
    public int NoScrollbars { get; set; }
    public int UsePaging { get; set; }
    public Vec4 SelectBorder { get; set; }
    public XPointer<MaterialAsset> SelectIcon { get; set; } // Alias
}

public class EditFieldDef
{
    public float MinVal { get; set; }
    public float MaxVal { get; set; }
    public float DefVal { get; set; }
    public float Range { get; set; }
    public int MaxChars { get; set; }
    public int MaxCharsGotoNext { get; set; }
    public int MaxPaintChars { get; set; }
    public int PaintOffset { get; set; }
}

public class MultiDef
{
    public XPointer<string>[] DvarList { get; set; } = new XPointer<string>[32]; // ?
    public XPointer<string>[] DvarStr { get; set; } = new XPointer<string>[32]; // ?
    public float[] DvarValue { get; set; } = new float[32];
    public int Count { get; set; }
    public int StrDef { get; set; }
}

public class TextScrollDef
{
    public int StartTime { get; set; }
}

public class ItemDefData
{
    public int Raw { get; set; }
    public XPointer<ListBoxDef> ListBox { get; set; } // ?
    public XPointer<EditFieldDef> EditField { get; set; } // ?
    public XPointer<MultiDef> Multi { get; set; } // ?
    public XPointer<string> EnumDvarName { get; set; } // Direct
    public XPointer<NewsTickerDef> NewsTicker { get; set; } // ?
    public XPointer<TextScrollDef> TextScroll { get; set; } // ?
    public XPointer<ItemDefRawData> Data { get; set; } // ?
}

public class ItemDefRawData
{
    public int[] Words { get; set; } = new int[8];
}

public class ItemDef
{
    public Window Window { get; set; }
#if !PC
    public RectangleDef[] TextRect { get; set; } = new RectangleDef[4];
#else
    public RectangleDef[] TextRect { get; set; } = new RectangleDef[1];
#endif
    public int Type { get; set; }
    public int DataType { get; set; }
    public int Align { get; set; }
    public int FontEnum { get; set; }
    public int TextAlignMode { get; set; }
    public float TextAlignX { get; set; }
    public float TextAlignY { get; set; }
    public float TextScale { get; set; }
    public int TextStyle { get; set; }
    public int GameMsgWindowIndex { get; set; }
    public int GameMsgWindowMode { get; set; }
    public XPointer<string> Text { get; set; } // Direct
    public int TextSaveGameInfo { get; set; }
    public XPointer<MenuDef> Parent { get; set; } // ?
    public XPointer<MenuEventHandlerSet> MouseEnterText { get; set; } // ?
    public XPointer<MenuEventHandlerSet> MouseExitText { get; set; } // ?
    public XPointer<MenuEventHandlerSet> MouseEnter { get; set; } // ?
    public XPointer<MenuEventHandlerSet> MouseExit { get; set; } // ?
    public XPointer<MenuEventHandlerSet> Action { get; set; } // ?
    public XPointer<MenuEventHandlerSet> Accept { get; set; } // ?
    public XPointer<MenuEventHandlerSet> OnFocus { get; set; } // ?
    public XPointer<MenuEventHandlerSet> LeaveFocus { get; set; } // ?
    public XPointer<string> Dvar { get; set; }
    public XPointer<string> DvarTest { get; set; }
    public XPointer<ItemKeyHandler> OnKey { get; set; } // ?
    public XPointer<string> EnableDvar { get; set; }
    public int DvarFlags { get; set; }
    public XPointer<SndAliasList> FocusSound { get; set; } // ?
    public float Special { get; set; }
    public int[] CursorPos { get; set; } = new int[4];
    public ItemDefData TypeData { get; set; }
    public int ImageTrack { get; set; }
    public int FloatExpressionCount { get; set; }
    public XPointer<ItemFloatExpression[]> FloatExpressions { get; set; } // ?
    public XPointer<Statement> VisibleExp { get; set; } // ?
    public XPointer<Statement> DisabledExp { get; set; } // ?
    public XPointer<Statement> TextExp { get; set; } // ?
    public XPointer<Statement> MaterialExp { get; set; } // ?
    public Vec4 GlowColor { get; set; }
    public bool DecayActive { get; set; }
    public byte DecayActivePadding0 { get; set; }
    public byte DecayActivePadding1 { get; set; }
    public byte DecayActivePadding2 { get; set; }
    public int FxBirthTime { get; set; }
    public int FxLetterTime { get; set; }
    public int FxDecayStartTime { get; set; }
    public int FxDecayDuration { get; set; }
    public int LastSoundPlayedTime { get; set; }
}

public class StaticDvar
{
    public XPointer<Dvar.Dvar> Dvar { get; set; } // ?
    public XPointer<string> DvarName { get; set; } // Direct
}

public class StaticDvarList
{
    public int NumStaticDvars { get; set; }
    public XPointer<XPointer<StaticDvar>[]> StaticDvars { get; set; } // ? -> ?
}

public class UIFunctionList
{
    public int TotalFunctions { get; set; }
    public XPointer<XPointer<Statement>[]> Functions { get; set; } // ? -> ?
}

public class StringList
{
    public int TotalStrings { get; set; }
    public XPointer<XPointer<string>[]> Strings { get; set; } // ? -> ?
}

public class ExpressionSupportingData
{
    public UIFunctionList UiFunctions { get; set; }
    public StaticDvarList StaticDvarList { get; set; }
    public StringList UiStrings { get; set; }
}

public class MenuTransition
{
    public TransitionType TransitionType { get; set; }
    public int TargetField { get; set; }
    public int StartTime { get; set; }
    public float StartVal { get; set; }
    public float EndVal { get; set; }
    public float Time { get; set; }
    public TriggerType EndTriggerType { get; set; }
}
