using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Models.Assets.Menu.Elements;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x14)]
public class RectangleDef
{
    [XField(Offset = 0x00)]
    public float X { get; set; }
    [XField(Offset = 0x04)]
    public float Y { get; set; }
    [XField(Offset = 0x08)]
    public float W { get; set; }
    [XField(Offset = 0x0C)]
    public float H { get; set; }
    [XField(Offset = 0x10)]
    public byte HorzAlign { get; set; }
    [XField(Offset = 0x11)]
    public byte VertAlign { get; set; }
    [XField(Offset = 0x12)]
    public ushort AlignmentPadding { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0xB0)]
public class Window
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public RectangleDef Rect { get; set; }

    [XField(Offset = 0x18)]
    public RectangleDef RectClient { get; set; }

    [XField(Offset = 0x2C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> GroupPtr { get; set; } // Direct
    public string Group => GroupPtr is { IsResolved: true } ? GroupPtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x30)]
    public int Style { get; set; }

    [XField(Offset = 0x34)]
    public int Border { get; set; }

    [XField(Offset = 0x38)]
    public int OwnerDraw { get; set; }

    [XField(Offset = 0x3C)]
    public int OwnerDrawFlags { get; set; }

    [XField(Offset = 0x40)]
    public float BorderSize { get; set; }

    [XField(Offset = 0x44)]
    public int StaticFlags { get; set; }
#if !PC
    [XField(Offset = 0x48, Count = 4)]
    public int[] DynamicFlags { get; set; } = new int[4];
#else
    [XField(Offset = 0x48, Count = 1)]
    public int[] DynamicFlags { get; set; } = new int[1];
#endif
    [XField(Offset = 0x58)]
    public int NextTime { get; set; }

    [XField(Offset = 0x5C)]
    public Vec4 ForeColor { get; set; }

    [XField(Offset = 0x6C)]
    public Vec4 BackColor { get; set; }

    [XField(Offset = 0x7C)]
    public Vec4 BorderColor { get; set; }

    [XField(Offset = 0x8C)]
    public Vec4 OutlineColor { get; set; }

    [XField(Offset = 0x9C)]
    public Vec4 DisableColor { get; set; }

    [XField(Offset = 0xAC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<MaterialAsset> Background { get; set; } // Alias
}

public class ExpressionString
{
    public XPointer<string> StringPtr { get; set; } // Direct
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Value ?? string.Empty : string.Empty;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class OperandInternalData
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<object> DataPtr { get; set; }

    public int Raw => DataPtr?.Raw ?? 0;

    public int IntVal => Raw;
    public float FloatVal => BitConverter.Int32BitsToSingle(Raw);
    public XPointer<string?>? StringVal { get; set; } // Direct
    public XPointer<Statement>? Function { get; set; } // ??
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class Operand
{
    [XField(Offset = 0x00)]
    public ExpDataType DataType { get; set; }

    [XField(Offset = 0x04)]
    public OperandInternalData Internals { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class EntryInternalData
{
    public OperationEnum Op => Operand is null
        ? default
        : (OperationEnum)Operand.DataType;

    [XField(Offset = 0x00)]
    public Operand Operand { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public class ExpressionEntry
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    public int Type { get; set; }

    [XField(Offset = 0x04)]
    public EntryInternalData Data { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class SetLocalVarData
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> LocalVarName { get; set; } // Direct
    public string LocalVar => LocalVarName is { IsResolved: true } ? LocalVarName.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> Expression { get; set; } // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class ConditionalScript
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> EventHandlerSet { get; set; } // ?

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> EventExpression { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public class Statement
{
    [XField(Offset = 0x00)]
    public int NumEntries { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(NumEntries))]
    public XPointer<ExpressionEntry[]> Entries { get; set; } // ?

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<ExpressionSupportingData> SupportingData { get; set; } // ?

    [XField(Offset = 0x0C)]
    public int LastExecuteTime { get; set; }

    [XField(Offset = 0x10)]
    public Operand LastResult { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class ItemFloatExpression
{
    [XField(Offset = 0x00)]
    public ItemFloatExpressionTarget Target { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> Expression { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class EventData
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.None)]
    public XPointer<object> DataPtr { get; set; }

    public int Raw => DataPtr?.Raw ?? 0;
    public XPointer<string?> UnconditionalScript { get; set; } // Direct
    public XPointer<ConditionalScript> ConditionalScript { get; set; } // ?
    public XPointer<MenuEventHandlerSet> ElseScript { get; set; } // ?
    public XPointer<SetLocalVarData> SetLocalVarData { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class MenuEventHandler
{
    [XField(Offset = 0x00)]
    public EventData EventData { get; set; }

    [XField(Offset = 0x04)]
    public byte EventType { get; set; }

    [XField(Offset = 0x05)]
    public byte EventTypePadding0 { get; set; }

    [XField(Offset = 0x06)]
    public byte EventTypePadding1 { get; set; }

    [XField(Offset = 0x07)]
    public byte EventTypePadding2 { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class MenuEventHandlerSet
{
    [XField(Offset = 0x00)]
    public int EventHandlerCount { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.PointerArray,
        CountMember = nameof(EventHandlerCount),
        ElementResolutionKind = PointerResolutionKind.CurrentStream,
        ElementTarget = XPointerTarget.Object)]
    public XPointer<XPointer<MenuEventHandler>[]> EventHandlers { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public class ItemKeyHandler
{
    [XField(Offset = 0x00)]
    public int Key { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> Action { get; set; } // ?

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<ItemKeyHandler> Next { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1C)]
public class NewsTickerDef
{
    [XField(Offset = 0x00)]
    public int FeedId { get; set; }

    [XField(Offset = 0x04)]
    public int Speed { get; set; }

    [XField(Offset = 0x08)]
    public int Spacing { get; set; }

    [XField(Offset = 0x0C)]
    public int LastTime { get; set; }

    [XField(Offset = 0x10)]
    public int Start { get; set; }

    [XField(Offset = 0x14)]
    public int End { get; set; }

    [XField(Offset = 0x18)]
    public float X { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public class ColumnInfo
{
    [XField(Offset = 0x00)]
    public int Pos { get; set; }

    [XField(Offset = 0x04)]
    public int Width { get; set; }

    [XField(Offset = 0x08)]
    public int MaxChars { get; set; }

    [XField(Offset = 0x0C)]
    public int Alignment { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x158)]
public class ListBoxDef
{
#if !PC
    [XField(Offset = 0x00, Count = 4)]
    public int[] StartPos { get; set; } = new int[4];

    [XField(Offset = 0x10, Count = 4)]
    public int[] EndPos { get; set; } = new int[4];

    [XField(Offset = 0x20)]
    public int DrawPadding { get; set; }
#else
    [XField(Offset = 0x00, Count = 4)]
    public int[] Unknown { get; set; } = new int[4];
#endif
    [XField(Offset = 0x24)]
    public float ElementWidth { get; set; }

    [XField(Offset = 0x28)]
    public float ElementHeight { get; set; }

    [XField(Offset = 0x2C)]
    public int ElementStyle { get; set; }

    [XField(Offset = 0x30)]
    public int NumColumns { get; set; }

    [XField(Offset = 0x34, Count = 16)]
    public ColumnInfo[] ColumnInfo { get; set; } = new ColumnInfo[16];

    [XField(Offset = 0x134)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> DoubleClick { get; set; } // ?

    [XField(Offset = 0x138)]
    public int NotSelectable { get; set; }

    [XField(Offset = 0x13C)]
    public int NoScrollbars { get; set; }

    [XField(Offset = 0x140)]
    public int UsePaging { get; set; }

    [XField(Offset = 0x144)]
    public Vec4 SelectBorder { get; set; }

    [XField(Offset = 0x154)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<MaterialAsset> SelectIcon { get; set; } // Alias
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x20)]
public class EditFieldDef
{
    [XField(Offset = 0x00)]
    public float MinVal { get; set; }

    [XField(Offset = 0x04)]
    public float MaxVal { get; set; }

    [XField(Offset = 0x08)]
    public float DefVal { get; set; }

    [XField(Offset = 0x0C)]
    public float Range { get; set; }

    [XField(Offset = 0x10)]
    public int MaxChars { get; set; }

    [XField(Offset = 0x14)]
    public int MaxCharsGotoNext { get; set; }

    [XField(Offset = 0x18)]
    public int MaxPaintChars { get; set; }

    [XField(Offset = 0x1C)]
    public int PaintOffset { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x188)]
public class MultiDef
{
    [XField(Offset = 0x00, Count = 32)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?>[] DvarList { get; set; } = new XPointer<string?>[32]; // ?

    [XField(Offset = 0x80, Count = 32)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?>[] DvarStr { get; set; } = new XPointer<string?>[32]; // ?

    [XField(Offset = 0x100, Count = 32)]
    public float[] DvarValue { get; set; } = new float[32];

    [XField(Offset = 0x180)]
    public int Count { get; set; }

    [XField(Offset = 0x184)]
    public int StrDef { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class TextScrollDef
{
    [XField(Offset = 0x00)]
    public int StartTime { get; set; }
}

public class ItemDefData
{
    public int Raw { get; set; }
    public XPointer<ListBoxDef>? ListBox { get; set; } // ?
    public XPointer<EditFieldDef>? EditField { get; set; } // ?
    public XPointer<MultiDef>? Multi { get; set; } // ?
    public XPointer<string?>? EnumDvarName { get; set; } // Direct
    public XPointer<NewsTickerDef>? NewsTicker { get; set; } // ?
    public XPointer<TextScrollDef>? TextScroll { get; set; } // ?
    public XPointer<ItemDefRawData>? Data { get; set; } // ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x20)]
public class ItemDefRawData
{
    [XField(Offset = 0x00, Count = 8)]
    public int[] Words { get; set; } = new int[8];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1CC)]
public class ItemDef
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    public Window Window { get; set; }
#if !PC
    [XField(Offset = 0xB0, Count = 4)]
    public RectangleDef[] TextRect { get; set; } = new RectangleDef[4];
#else
    [XField(Offset = 0xB0, Count = 1)]
    public RectangleDef[] TextRect { get; set; } = new RectangleDef[1];
#endif
    [XField(Offset = 0x100)]
    public int Type { get; set; }
    [XField(Offset = 0x104)]
    public int DataType { get; set; }
    [XField(Offset = 0x108)]
    public int Align { get; set; }
    [XField(Offset = 0x10C)]
    public int FontEnum { get; set; }
    [XField(Offset = 0x110)]
    public int TextAlignMode { get; set; }
    [XField(Offset = 0x114)]
    public float TextAlignX { get; set; }
    [XField(Offset = 0x118)]
    public float TextAlignY { get; set; }
    [XField(Offset = 0x11C)]
    public float TextScale { get; set; }
    [XField(Offset = 0x120)]
    public int TextStyle { get; set; }
    [XField(Offset = 0x124)]
    public int GameMsgWindowIndex { get; set; }
    [XField(Offset = 0x128)]
    public int GameMsgWindowMode { get; set; }

    [XField(Offset = 0x12C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> Text { get; set; } // Direct

    [XField(Offset = 0x130)]
    public int TextSaveGameInfo { get; set; }

    [XField(Offset = 0x134)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.None)]
    public XPointer<MenuDef> Parent { get; set; } // ?

    [XField(Offset = 0x138)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> MouseEnterText { get; set; } // ?

    [XField(Offset = 0x13C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> MouseExitText { get; set; } // ?

    [XField(Offset = 0x140)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> MouseEnter { get; set; } // ?

    [XField(Offset = 0x144)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> MouseExit { get; set; } // ?

    [XField(Offset = 0x148)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> Action { get; set; } // ?

    [XField(Offset = 0x14C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> Accept { get; set; } // ?

    [XField(Offset = 0x150)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> OnFocus { get; set; } // ?

    [XField(Offset = 0x154)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> LeaveFocus { get; set; } // ?

    [XField(Offset = 0x158)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> Dvar { get; set; }

    [XField(Offset = 0x15C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> DvarTest { get; set; }

    [XField(Offset = 0x160)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<ItemKeyHandler> OnKey { get; set; } // ?

    [XField(Offset = 0x164)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> EnableDvar { get; set; }

    [XField(Offset = 0x168)]
    public int DvarFlags { get; set; }

    [XField(Offset = 0x16C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<SndAliasList> FocusSound { get; set; } // ?

    [XField(Offset = 0x170)]
    public float Special { get; set; }

    [XField(Offset = 0x174, Count = 4)]
    public int[] CursorPos { get; set; } = new int[4];

    [XField(Offset = 0x184)]
    public ItemDefData TypeData { get; set; }

    [XField(Offset = 0x188)]
    public int ImageTrack { get; set; }

    [XField(Offset = 0x18C)]
    public int FloatExpressionCount { get; set; }

    [XField(Offset = 0x190)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(FloatExpressionCount))]
    public XPointer<ItemFloatExpression[]> FloatExpressions { get; set; } // ?

    [XField(Offset = 0x194)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> VisibleExp { get; set; } // ?

    [XField(Offset = 0x198)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> DisabledExp { get; set; } // ?

    [XField(Offset = 0x19C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> TextExp { get; set; } // ?

    [XField(Offset = 0x1A0)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> MaterialExp { get; set; } // ?

    [XField(Offset = 0x1A4)]
    public Vec4 GlowColor { get; set; }

    [XField(Offset = 0x1B4)]
    public byte DecayActive { get; set; }

    [XField(Offset = 0x1B5)]
    public byte DecayActivePadding0 { get; set; }

    [XField(Offset = 0x1B6)]
    public byte DecayActivePadding1 { get; set; }

    [XField(Offset = 0x1B7)]
    public byte DecayActivePadding2 { get; set; }

    [XField(Offset = 0x1B8)]
    public int FxBirthTime { get; set; }

    [XField(Offset = 0x1BC)]
    public int FxLetterTime { get; set; }

    [XField(Offset = 0x1C0)]
    public int FxDecayStartTime { get; set; }

    [XField(Offset = 0x1C4)]
    public int FxDecayDuration { get; set; }

    [XField(Offset = 0x1C8)]
    public int LastSoundPlayedTime { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class StaticDvar
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> Dvar { get; set; } // Direct

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> DvarName { get; set; } // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class StaticDvarList
{
    [XField(Offset = 0x00)]
    public int NumStaticDvars { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.PointerArray,
        CountMember = nameof(NumStaticDvars),
        ElementResolutionKind = PointerResolutionKind.CurrentStream,
        ElementTarget = XPointerTarget.Object)]
    public XPointer<XPointer<StaticDvar>[]> StaticDvars { get; set; } // ? -> ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class UIFunctionList
{
    [XField(Offset = 0x00)]
    public int TotalFunctions { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.PointerArray,
        CountMember = nameof(TotalFunctions),
        ElementResolutionKind = PointerResolutionKind.CurrentStream,
        ElementTarget = XPointerTarget.Object)]
    public XPointer<XPointer<Statement>[]> Functions { get; set; } // ? -> ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class StringList
{
    [XField(Offset = 0x00)]
    public int TotalStrings { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.PointerArray,
        CountMember = nameof(TotalStrings),
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString)]
    public XPointer<XPointer<string>[]> Strings { get; set; } // ? -> ?
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public class ExpressionSupportingData
{
    [XField(Offset = 0x00)]
    public UIFunctionList UiFunctions { get; set; }

    [XField(Offset = 0x08)]
    public StaticDvarList StaticDvarList { get; set; }

    [XField(Offset = 0x10)]
    public StringList UiStrings { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1C)]
public class MenuTransition
{
    [XField(Offset = 0x00)]
    public TransitionType TransitionType { get; set; }

    [XField(Offset = 0x04)]
    public int TargetField { get; set; }

    [XField(Offset = 0x08)]
    public int StartTime { get; set; }

    [XField(Offset = 0x0C)]
    public float StartVal { get; set; }

    [XField(Offset = 0x10)]
    public float EndVal { get; set; }

    [XField(Offset = 0x14)]
    public float Time { get; set; }

    [XField(Offset = 0x18)]
    public TriggerType EndTriggerType { get; set; }
}
