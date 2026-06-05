using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Dvar;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class MenuReader
{
    public static MenuDef Read(ref XFileReadContext context)
    {
        return Read(ref context, windowDynamicFlagCount: null);
    }

    public static MenuDef Read(ref XFileReadContext context, int? windowDynamicFlagCount)
    {
        var asset = new MenuDef
        {
            Offset = context.Position,
            Window = ReadWindow(ref context, resolvePointers: false, windowDynamicFlagCount),
            FontPtr = ReadStringPointer(ref context, resolve: false),
            Fullscreen = context.ReadInt32(),
            ItemCount = context.ReadInt32(),
            FontIndex = context.ReadInt32(),
        };

        ReadInt32Array(ref context, asset.CursorItems);

        asset.FadeCycle = context.ReadInt32();
        asset.FadeClamp = context.ReadFloat();
        asset.FadeAmount = context.ReadFloat();
        asset.FadeInAmount = context.ReadFloat();
        asset.BlurRadius = context.ReadFloat();

        asset.OnOpen = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnRequestClose = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnClose = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnEsc = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.ExecKeys = ReadItemKeyHandlerPointer(ref context, resolve: false);
        asset.VisibleExp = ReadStatementPointer(ref context, resolve: false);
        asset.AllowedBinding = ReadStringPointer(ref context, resolve: false);
        asset.SoundName = ReadStringPointer(ref context, resolve: false);
        asset.ImageTrack = context.ReadInt32();

        asset.FocusColor = context.ReadVec4();
        asset.RectXExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectYExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectHExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectWExp = ReadStatementPointer(ref context, resolve: false);
#if PC
        asset.OpenSoundExp = ReadStatementPointer(ref context, resolve: false);
        asset.CloseSoundExp = ReadStatementPointer(ref context, resolve: false);
#endif
        asset.Items = context.ReadDirectPointer<ZonePointer<ItemDef>[]>("MenuDef.Items");

#if !PC
        asset.ScaleTransition = ReadMenuTransitions(ref context);
        asset.AlphaTransition = ReadMenuTransitions(ref context);
        asset.XTransition = ReadMenuTransitions(ref context);
        asset.YTransition = ReadMenuTransitions(ref context);
#else
        asset.Unknown = context.ReadBytes(112);
#endif
        asset.ExpressionData = ReadExpressionSupportingDataPointer(ref context, resolve: false);

        ResolveMenuDefPointers(ref context, asset);

        return asset;
    }

    private static Window ReadWindow(
        ref XFileReadContext context,
        bool resolvePointers = true,
        int? dynamicFlagCount = null)
    {
        var window = new Window
        {
            NamePtr = ReadStringPointer(ref context, resolvePointers),
            Rect = ReadRectangle(ref context),
            RectClient = ReadRectangle(ref context),
            GroupPtr = ReadStringPointer(ref context, resolvePointers),
            Style = context.ReadInt32(),
            Border = context.ReadInt32(),
            OwnerDraw = context.ReadInt32(),
            OwnerDrawFlags = context.ReadInt32(),
            BorderSize = context.ReadFloat(),
            StaticFlags = context.ReadInt32(),
        };

        ReadInt32Array(ref context, window.DynamicFlags, dynamicFlagCount ?? window.DynamicFlags.Length);

        window.NextTime = context.ReadInt32();
        window.ForeColor = context.ReadVec4();
        window.BackColor = context.ReadVec4();
        window.BorderColor = context.ReadVec4();
        window.OutlineColor = context.ReadVec4();
        window.DisableColor = context.ReadVec4();
        window.Background = context.ReadAliasPointer<FastFile.Models.Assets.Material.Material>("Window.Background");
        if (resolvePointers)
            ResolveMaterialAssetReference(ref context, window.Background);

        return window;
    }

    private static void ResolveWindowPointers(ref XFileReadContext context, Window window)
    {
        context.ResolveInlinePointer(window.NamePtr, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(window.GroupPtr, GenericReader.ReadStringPointerValue);
        ResolveMaterialAssetReference(ref context, window.Background);
    }

    private static void ResolveMaterialAssetReference(
        ref XFileReadContext context,
        ZonePointer<FastFile.Models.Assets.Material.Material> pointer)
    {
        context.ResolvePointerInBlock(
            pointer,
            FastFile.Models.Zone.XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<FastFile.Models.Assets.Material.Material> materialPointer) =>
            {
                materialPointer.SetResult(pointerContext.ReadPointerValue(materialPointer, MaterialReader.Read));
            });
    }

    private static void ResolveMenuDefPointers(ref XFileReadContext context, MenuDef asset)
    {
        context.ResolveInlinePointer(asset.ExpressionData, ReadExpressionSupportingDataPointerValue);
        ResolveWindowPointers(ref context, asset.Window);
        context.ResolveInlinePointer(asset.FontPtr, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(asset.OnOpen, ReadMenuEventHandlerSetPointerValue);
        context.ResolveInlinePointer(asset.OnRequestClose, ReadMenuEventHandlerSetPointerValue);
        context.ResolveInlinePointer(asset.OnClose, ReadMenuEventHandlerSetPointerValue);
        context.ResolveInlinePointer(asset.OnEsc, ReadMenuEventHandlerSetPointerValue);
        context.ResolveInlinePointer(asset.ExecKeys, ReadItemKeyHandlerPointerValue);
        context.ResolveInlinePointer(asset.VisibleExp, ReadStatementPointerValue);
        context.ResolveInlinePointer(asset.AllowedBinding, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(asset.SoundName, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(asset.RectXExp, ReadStatementPointerValue);
        context.ResolveInlinePointer(asset.RectYExp, ReadStatementPointerValue);
        context.ResolveInlinePointer(asset.RectHExp, ReadStatementPointerValue);
        context.ResolveInlinePointer(asset.RectWExp, ReadStatementPointerValue);
#if PC
        context.ResolveInlinePointer(asset.OpenSoundExp, ReadStatementPointerValue);
        context.ResolveInlinePointer(asset.CloseSoundExp, ReadStatementPointerValue);
#endif
        context.ResolveInlinePointerDeferred(
            asset.Items,
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<ItemDef>[]> pointer) =>
            {
                var items = ReadPointerArray<ItemDef>(ref pointerContext, asset.ItemCount);
                pointer.SetResult(items);

                foreach (var itemPointer in items)
                    pointerContext.ResolveInlinePointer(itemPointer, ReadItemDefPointerValue);
            });
    }

    private static RectangleDef ReadRectangle(ref XFileReadContext context)
    {
        var rectangle = new RectangleDef
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            W = context.ReadFloat(),
            H = context.ReadFloat(),
            HorzAlign = context.ReadByte(),
            VertAlign = context.ReadByte(),
        };

        rectangle.AlignmentPadding = context.ReadUInt16();
        return rectangle;
    }

    private static ItemDef ReadItemDef(ref XFileReadContext context)
    {
        var item = new ItemDef
        {
            Window = ReadWindow(ref context),
        };

        for (var i = 0; i < item.TextRect.Length; i++)
            item.TextRect[i] = ReadRectangle(ref context);

        item.Type = context.ReadInt32();
        item.DataType = context.ReadInt32();
        item.Align = context.ReadInt32();
        item.FontEnum = context.ReadInt32();
        item.TextAlignMode = context.ReadInt32();
        item.TextAlignX = context.ReadFloat();
        item.TextAlignY = context.ReadFloat();
        item.TextScale = context.ReadFloat();
        item.TextStyle = context.ReadInt32();
        item.GameMsgWindowIndex = context.ReadInt32();
        item.GameMsgWindowMode = context.ReadInt32();
        item.Text = ReadStringPointer(ref context);
        item.TextSaveGameInfo = context.ReadInt32();
        item.Parent = context.CreatePointer<MenuDef>(context.ReadInt32(), register: false);
        item.MouseEnterText = ReadMenuEventHandlerSetPointer(ref context);
        item.MouseExitText = ReadMenuEventHandlerSetPointer(ref context);
        item.MouseEnter = ReadMenuEventHandlerSetPointer(ref context);
        item.MouseExit = ReadMenuEventHandlerSetPointer(ref context);
        item.Action = ReadMenuEventHandlerSetPointer(ref context);
        item.Accept = ReadMenuEventHandlerSetPointer(ref context);
        item.OnFocus = ReadMenuEventHandlerSetPointer(ref context);
        item.LeaveFocus = ReadMenuEventHandlerSetPointer(ref context);
        item.Dvar = ReadStringPointer(ref context);
        item.DvarTest = ReadStringPointer(ref context);
        item.OnKey = ReadItemKeyHandlerPointer(ref context);
        item.EnableDvar = ReadStringPointer(ref context);
        item.DvarFlags = context.ReadInt32();
        item.FocusSound = context.ReadAliasPointer<FastFile.Models.Assets.SoundAliasList.SndAliasList>("ItemDef.FocusSound");
        item.Special = context.ReadFloat();
        ReadInt32Array(ref context, item.CursorPos);
        item.TypeData = ReadItemDefData(ref context, item.Type);
        item.ImageTrack = context.ReadInt32();
        item.FloatExpressionCount = context.ReadInt32();
        item.FloatExpressions = context.ReadInlinePointer<ItemFloatExpression[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ItemFloatExpression[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, item.FloatExpressionCount, ReadItemFloatExpression);
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "ItemDef.FloatExpressions");
        item.VisibleExp = ReadStatementPointer(ref context);
        item.DisabledExp = ReadStatementPointer(ref context);
        item.TextExp = ReadStatementPointer(ref context);
        item.MaterialExp = ReadStatementPointer(ref context);
        item.GlowColor = context.ReadVec4();
        item.DecayActive = context.ReadBool();
        item.DecayActivePadding0 = context.ReadByte();
        item.DecayActivePadding1 = context.ReadByte();
        item.DecayActivePadding2 = context.ReadByte();
        item.FxBirthTime = context.ReadInt32();
        item.FxLetterTime = context.ReadInt32();
        item.FxDecayStartTime = context.ReadInt32();
        item.FxDecayDuration = context.ReadInt32();
        item.LastSoundPlayedTime = context.ReadInt32();

        return item;
    }

    private static ItemDefData ReadItemDefData(ref XFileReadContext context, int itemType)
    {
        var raw = context.ReadInt32();
        var data = new ItemDefData
        {
            Raw = raw,
            ListBox = context.CreatePointer<ListBoxDef>(raw, register: false),
            EditField = context.CreatePointer<EditFieldDef>(raw, register: false),
            Multi = context.CreatePointer<MultiDef>(raw, register: false),
            EnumDvarName = context.CreatePointer<string>(raw, register: false),
            NewsTicker = context.CreatePointer<NewsTickerDef>(raw, register: false),
            TextScroll = context.CreatePointer<TextScrollDef>(raw, register: false),
            Data = context.CreatePointer<ItemDefRawData>(raw, register: false),
        };

        switch (itemType)
        {
            case 0:
                context.RegisterPointer(data.EditField, PointerResolutionKind.Direct, "ItemDef.TypeData.EditField");
                context.ResolveInlinePointer(data.EditField, ReadEditFieldDefPointerValue);
                break;
            case 4:
            case 9:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                context.RegisterPointer(data.EditField, PointerResolutionKind.Direct, "ItemDef.TypeData.EditField");
                context.ResolveInlinePointer(data.EditField, ReadEditFieldDefPointerValue);
                break;
            case 6:
                context.RegisterPointer(data.ListBox, PointerResolutionKind.Direct, "ItemDef.TypeData.ListBox");
                context.ResolveInlinePointer(data.ListBox, ReadListBoxDefPointerValue);
                break;
            case 10:
            case 12:
                context.RegisterPointer(data.Multi, PointerResolutionKind.Direct, "ItemDef.TypeData.Multi");
                context.ResolveInlinePointer(data.Multi, ReadMultiDefPointerValue);
                break;
            case 13:
                context.RegisterPointer(data.EnumDvarName, PointerResolutionKind.Direct, "ItemDef.TypeData.EnumDvarName");
                context.ResolveInlinePointer(data.EnumDvarName, GenericReader.ReadStringPointerValue);
                break;
            case 20:
                context.RegisterPointer(data.NewsTicker, PointerResolutionKind.Direct, "ItemDef.TypeData.NewsTicker");
                context.ResolveInlinePointer(data.NewsTicker, ReadNewsTickerDefPointerValue);
                break;
            case 21:
                context.RegisterPointer(data.TextScroll, PointerResolutionKind.Direct, "ItemDef.TypeData.TextScroll");
                context.ResolveInlinePointer(data.TextScroll, ReadTextScrollDefPointerValue);
                break;
        }

        return data;
    }

    private static void ReadRawItemDataPointerValue(ref XFileReadContext context, ZonePointer<ItemDefRawData> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, (ref XFileReadContext valueContext) =>
        {
            var data = new ItemDefRawData();
            ReadInt32Array(ref valueContext, data.Words);
            return data;
        }));
    }

    private static Statement ReadStatement(ref XFileReadContext context)
    {
        var statement = new Statement
        {
            NumEntries = context.ReadInt32(),
        };

        statement.Entries = context.ReadInlinePointer<ExpressionEntry[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ExpressionEntry[]> pointer) =>
            {
                var entries = ReadArray(ref pointerContext, statement.NumEntries, ReadExpressionEntry);
                pointer.SetResult(entries);
        },
        PointerResolutionKind.Direct,
        "Statement.Entries");
        statement.SupportingData = ReadExpressionSupportingDataPointer(ref context, resolve: false);
        statement.LastExecuteTime = context.ReadInt32();
        statement.LastResult = ReadOperand(ref context, resolveUnion: false);

        return statement;
    }

    private static ExpressionEntry ReadExpressionEntry(ref XFileReadContext context)
    {
        var entry = new ExpressionEntry
        {
            Type = context.ReadInt32(),
            Data = new EntryInternalData(),
        };

        var first = context.ReadInt32();
        entry.Data.Op = (OperationEnum)first;

        if (entry.Type != 1)
        {
            var second = context.ReadInt32();
            entry.Data.Operand = new Operand
            {
                DataType = (ExpDataType)first,
                Internals = new OperandInternalData
                {
                    IntVal = second,
                    FloatVal = BitConverter.Int32BitsToSingle(second),
                    StringVal = context.CreatePointer<string>(second, register: false),
                    Function = context.CreatePointer<Statement>(second, register: false),
                }
            };
            return entry;
        }

        entry.Data.Operand = ReadOperandData(ref context, (ExpDataType)first);

        return entry;
    }

    private static Operand ReadOperand(ref XFileReadContext context, bool resolveUnion = true)
    {
        var dataType = (ExpDataType)context.ReadInt32();
        return ReadOperandData(ref context, dataType, resolveUnion);
    }

    private static Operand ReadOperandData(ref XFileReadContext context, ExpDataType dataType, bool resolveUnion = true)
    {
        var raw = context.ReadInt32();
        var operand = new Operand
        {
            DataType = dataType,
            Internals = new OperandInternalData
            {
                IntVal = raw,
                FloatVal = BitConverter.Int32BitsToSingle(raw),
                StringVal = context.CreatePointer<string>(raw, register: false),
                Function = context.CreatePointer<Statement>(raw, register: false),
            }
        };

        if (!resolveUnion)
            return operand;

        switch (operand.DataType)
        {
            case ExpDataType.VAL_STRING:
                context.RegisterPointer(operand.Internals.StringVal, PointerResolutionKind.Direct, "Operand.StringVal");
                context.ResolveInlinePointer(operand.Internals.StringVal, GenericReader.ReadStringPointerValue);
                break;
            case ExpDataType.VAL_FUNCTION:
                context.RegisterPointer(operand.Internals.Function, PointerResolutionKind.Direct, "Operand.Function");
                context.ResolveInlinePointer(operand.Internals.Function, ReadStatementPointerValue);
                break;
        }

        return operand;
    }

    private static MenuEventHandlerSet ReadMenuEventHandlerSet(ref XFileReadContext context)
    {
        var set = new MenuEventHandlerSet
        {
            EventHandlerCount = context.ReadInt32(),
        };

        set.EventHandlers = context.ReadInlinePointer<ZonePointer<MenuEventHandler>[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<MenuEventHandler>[]> pointer) =>
            {
                var eventHandlers = ReadPointerArray<MenuEventHandler>(ref pointerContext, set.EventHandlerCount);
                pointer.SetResult(eventHandlers);

                foreach (var eventHandlerPointer in eventHandlers)
                    pointerContext.ResolveInlinePointer(eventHandlerPointer, ReadMenuEventHandlerPointerValue);
            },
            PointerResolutionKind.Direct,
            "MenuEventHandlerSet.EventHandlers");

        return set;
    }

    private static MenuEventHandler ReadMenuEventHandler(ref XFileReadContext context)
    {
        var raw = context.ReadInt32();
        var handler = new MenuEventHandler
        {
            EventData = new EventData
            {
                Raw = raw,
                UnconditionalScript = context.CreatePointer<string>(raw, register: false),
                ConditionalScript = context.CreatePointer<ConditionalScript>(raw, register: false),
                ElseScript = context.CreatePointer<MenuEventHandlerSet>(raw, register: false),
                SetLocalVarData = context.CreatePointer<SetLocalVarData>(raw, register: false),
            },
            EventType = context.ReadByte(),
        };
        handler.EventTypePadding0 = context.ReadByte();
        handler.EventTypePadding1 = context.ReadByte();
        handler.EventTypePadding2 = context.ReadByte();

        switch (handler.EventType)
        {
            case 0:
                context.RegisterPointer(handler.EventData.UnconditionalScript, PointerResolutionKind.Direct, "MenuEventHandler.UnconditionalScript");
                context.ResolveInlinePointer(handler.EventData.UnconditionalScript, GenericReader.ReadStringPointerValue);
                break;
            case 1:
                context.RegisterPointer(handler.EventData.ConditionalScript, PointerResolutionKind.Direct, "MenuEventHandler.ConditionalScript");
                context.ResolveInlinePointer(handler.EventData.ConditionalScript, ReadConditionalScriptPointerValue);
                break;
            case 2:
                context.RegisterPointer(handler.EventData.ElseScript, PointerResolutionKind.Direct, "MenuEventHandler.ElseScript");
                context.ResolveInlinePointer(handler.EventData.ElseScript, ReadMenuEventHandlerSetPointerValue);
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                context.RegisterPointer(handler.EventData.SetLocalVarData, PointerResolutionKind.Direct, "MenuEventHandler.SetLocalVarData");
                context.ResolveInlinePointer(handler.EventData.SetLocalVarData, ReadSetLocalVarDataPointerValue);
                break;
        }

        return handler;
    }

    private static ConditionalScript ReadConditionalScript(ref XFileReadContext context)
    {
        var script = new ConditionalScript
        {
            EventHandlerSet = ReadMenuEventHandlerSetPointer(ref context, resolve: false),
            EventExpression = ReadStatementPointer(ref context, resolve: false),
        };

        context.ResolveInlinePointer(script.EventExpression, ReadStatementPointerValue);
        context.ResolveInlinePointer(script.EventHandlerSet, ReadMenuEventHandlerSetPointerValue);

        return script;
    }

    private static SetLocalVarData ReadSetLocalVarData(ref XFileReadContext context)
    {
        return new SetLocalVarData
        {
            LocalVarName = ReadStringPointer(ref context),
            Expression = ReadStatementPointer(ref context),
        };
    }

    private static ItemKeyHandler ReadItemKeyHandler(ref XFileReadContext context)
    {
        return new ItemKeyHandler
        {
            Key = context.ReadInt32(),
            Action = ReadMenuEventHandlerSetPointer(ref context),
            Next = ReadItemKeyHandlerPointer(ref context),
        };
    }

    private static ItemFloatExpression ReadItemFloatExpression(ref XFileReadContext context)
    {
        return new ItemFloatExpression
        {
            Target = (ItemFloatExpressionTarget)context.ReadInt32(),
            Expression = ReadStatementPointer(ref context),
        };
    }

    private static ListBoxDef ReadListBoxDef(ref XFileReadContext context)
    {
        var listBox = new ListBoxDef();
#if !PC
        ReadInt32Array(ref context, listBox.StartPos);
        ReadInt32Array(ref context, listBox.EndPos);
        listBox.DrawPadding = context.ReadInt32();
#else
        ReadInt32Array(ref context, listBox.Unknown);
#endif
        listBox.ElementWidth = context.ReadFloat();
        listBox.ElementHeight = context.ReadFloat();
        listBox.ElementStyle = context.ReadInt32();
        listBox.NumColumns = context.ReadInt32();
        for (var i = 0; i < listBox.ColumnInfo.Length; i++)
            listBox.ColumnInfo[i] = ReadColumnInfo(ref context);
        listBox.DoubleClick = ReadMenuEventHandlerSetPointer(ref context);
        listBox.NotSelectable = context.ReadInt32();
        listBox.NoScrollbars = context.ReadInt32();
        listBox.UsePaging = context.ReadInt32();
        listBox.SelectBorder = context.ReadVec4();
        listBox.SelectIcon = MaterialReader.ReadMaterialPointer(ref context);

        return listBox;
    }

    private static ColumnInfo ReadColumnInfo(ref XFileReadContext context)
    {
        return new ColumnInfo
        {
            Pos = context.ReadInt32(),
            Width = context.ReadInt32(),
            MaxChars = context.ReadInt32(),
            Alignment = context.ReadInt32(),
        };
    }

    private static EditFieldDef ReadEditFieldDef(ref XFileReadContext context)
    {
        return new EditFieldDef
        {
            MinVal = context.ReadFloat(),
            MaxVal = context.ReadFloat(),
            DefVal = context.ReadFloat(),
            Range = context.ReadFloat(),
            MaxChars = context.ReadInt32(),
            MaxCharsGotoNext = context.ReadInt32(),
            MaxPaintChars = context.ReadInt32(),
            PaintOffset = context.ReadInt32(),
        };
    }

    private static MultiDef ReadMultiDef(ref XFileReadContext context)
    {
        var multi = new MultiDef();

        for (var i = 0; i < multi.DvarList.Length; i++)
            multi.DvarList[i] = ReadStringPointer(ref context);
        for (var i = 0; i < multi.DvarStr.Length; i++)
            multi.DvarStr[i] = ReadStringPointer(ref context);
        for (var i = 0; i < multi.DvarValue.Length; i++)
            multi.DvarValue[i] = context.ReadFloat();

        multi.Count = context.ReadInt32();
        multi.StrDef = context.ReadInt32();

        return multi;
    }

    private static NewsTickerDef ReadNewsTickerDef(ref XFileReadContext context)
    {
        return new NewsTickerDef
        {
            FeedId = context.ReadInt32(),
            Speed = context.ReadInt32(),
            Spacing = context.ReadInt32(),
            LastTime = context.ReadInt32(),
            Start = context.ReadInt32(),
            End = context.ReadInt32(),
            X = context.ReadFloat(),
        };
    }

    private static TextScrollDef ReadTextScrollDef(ref XFileReadContext context)
    {
        return new TextScrollDef
        {
            StartTime = context.ReadInt32(),
        };
    }

    private static ExpressionSupportingData ReadExpressionSupportingData(ref XFileReadContext context)
    {
        return new ExpressionSupportingData
        {
            UiFunctions = ReadUIFunctionList(ref context),
            StaticDvarList = ReadStaticDvarList(ref context),
            UiStrings = ReadStringList(ref context),
        };
    }

    private static UIFunctionList ReadUIFunctionList(ref XFileReadContext context)
    {
        var list = new UIFunctionList
        {
            TotalFunctions = context.ReadInt32(),
        };

        list.Functions = context.ReadInlinePointer<ZonePointer<Statement>[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<Statement>[]> pointer) =>
            {
                var functions = ReadPointerArray<Statement>(ref pointerContext, list.TotalFunctions);
                pointer.SetResult(functions);
                foreach (var functionPointer in functions)
                    pointerContext.ResolveInlinePointer(functionPointer, ReadStatementPointerValue);
            },
            PointerResolutionKind.Direct,
            "UIFunctionList.Functions");

        return list;
    }

    private static StaticDvarList ReadStaticDvarList(ref XFileReadContext context)
    {
        var list = new StaticDvarList
        {
            NumStaticDvars = context.ReadInt32(),
        };

        list.StaticDvars = context.ReadInlinePointer<ZonePointer<StaticDvar>[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<StaticDvar>[]> pointer) =>
            {
                var staticDvars = ReadPointerArray<StaticDvar>(ref pointerContext, list.NumStaticDvars);
                pointer.SetResult(staticDvars);
                foreach (var staticDvarPointer in staticDvars)
                    pointerContext.ResolveInlinePointer(staticDvarPointer, ReadStaticDvarPointerValue);
            },
            PointerResolutionKind.Direct,
            "StaticDvarList.StaticDvars");

        return list;
    }

    private static StaticDvar ReadStaticDvar(ref XFileReadContext context)
    {
        return new StaticDvar
        {
            Dvar = context.CreatePointer<Dvar>(context.ReadInt32(), register: false),
            DvarName = ReadStringPointer(ref context),
        };
    }

    private static StringList ReadStringList(ref XFileReadContext context)
    {
        var list = new StringList
        {
            TotalStrings = context.ReadInt32(),
        };

        list.Strings = context.ReadInlinePointer<ZonePointer<string>[]>(
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<string>[]> pointer) =>
            {
                var strings = ReadPointerArray<string>(ref pointerContext, list.TotalStrings, "StringList.Strings");
                pointer.SetResult(strings);
                foreach (var stringPointer in strings)
                    pointerContext.ResolveInlinePointer(stringPointer, GenericReader.ReadStringPointerValue);
            },
            PointerResolutionKind.Direct,
            "StringList.Strings");

        return list;
    }

    private static MenuTransition[] ReadMenuTransitions(ref XFileReadContext context)
    {
        return ReadArray(ref context, 4, ReadMenuTransition);
    }

    private static MenuTransition ReadMenuTransition(ref XFileReadContext context)
    {
        return new MenuTransition
        {
            TransitionType = (TransitionType)context.ReadInt32(),
            TargetField = context.ReadInt32(),
            StartTime = context.ReadInt32(),
            StartVal = context.ReadFloat(),
            EndVal = context.ReadFloat(),
            Time = context.ReadFloat(),
            EndTriggerType = (TriggerType)context.ReadInt32(),
        };
    }

    private static ZonePointer<string> ReadStringPointer(ref XFileReadContext context, bool resolve = true)
    {
        return GenericReader.ReadStringPointer(ref context, resolve);
    }

    private static ZonePointer<Statement> ReadStatementPointer(ref XFileReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<Statement>(
                ReadStatementPointerValue,
                PointerResolutionKind.Direct,
                "Statement")
            : context.ReadDirectPointer<Statement>("Statement");
    }

    private static ZonePointer<MenuEventHandlerSet> ReadMenuEventHandlerSetPointer(ref XFileReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<MenuEventHandlerSet>(
                ReadMenuEventHandlerSetPointerValue,
                PointerResolutionKind.Direct,
                "MenuEventHandlerSet")
            : context.ReadDirectPointer<MenuEventHandlerSet>("MenuEventHandlerSet");
    }

    private static ZonePointer<ItemKeyHandler> ReadItemKeyHandlerPointer(ref XFileReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<ItemKeyHandler>(
                ReadItemKeyHandlerPointerValue,
                PointerResolutionKind.Direct,
                "ItemKeyHandler")
            : context.ReadDirectPointer<ItemKeyHandler>("ItemKeyHandler");
    }

    private static ZonePointer<ExpressionSupportingData> ReadExpressionSupportingDataPointer(ref XFileReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<ExpressionSupportingData>(
                ReadExpressionSupportingDataPointerValue,
                PointerResolutionKind.Direct,
                "ExpressionSupportingData")
            : context.ReadDirectPointer<ExpressionSupportingData>("ExpressionSupportingData");
    }

    private static void ReadStatementPointerValue(ref XFileReadContext context, ZonePointer<Statement> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadStatement));
    }

    private static void ReadExpressionSupportingDataPointerValue(ref XFileReadContext context, ZonePointer<ExpressionSupportingData> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadExpressionSupportingData));
    }

    private static void ReadMenuEventHandlerSetPointerValue(ref XFileReadContext context, ZonePointer<MenuEventHandlerSet> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMenuEventHandlerSet));
    }

    private static void ReadMenuEventHandlerPointerValue(ref XFileReadContext context, ZonePointer<MenuEventHandler> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMenuEventHandler));
    }

    private static void ReadConditionalScriptPointerValue(ref XFileReadContext context, ZonePointer<ConditionalScript> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadConditionalScript));
    }

    private static void ReadSetLocalVarDataPointerValue(ref XFileReadContext context, ZonePointer<SetLocalVarData> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadSetLocalVarData));
    }

    private static void ReadItemKeyHandlerPointerValue(ref XFileReadContext context, ZonePointer<ItemKeyHandler> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadItemKeyHandler));
    }

    private static void ReadItemDefPointerValue(ref XFileReadContext context, ZonePointer<ItemDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadItemDef));
    }

    private static void ReadListBoxDefPointerValue(ref XFileReadContext context, ZonePointer<ListBoxDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadListBoxDef));
    }

    private static void ReadEditFieldDefPointerValue(ref XFileReadContext context, ZonePointer<EditFieldDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadEditFieldDef));
    }

    private static void ReadMultiDefPointerValue(ref XFileReadContext context, ZonePointer<MultiDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMultiDef));
    }

    private static void ReadNewsTickerDefPointerValue(ref XFileReadContext context, ZonePointer<NewsTickerDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadNewsTickerDef));
    }

    private static void ReadTextScrollDefPointerValue(ref XFileReadContext context, ZonePointer<TextScrollDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadTextScrollDef));
    }

    private static void ReadStaticDvarPointerValue(ref XFileReadContext context, ZonePointer<StaticDvar> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadStaticDvar));
    }

    private static string ReadAlignedCString(ref XFileReadContext context)
    {
        return context.ReadAlignedCString();
    }

    private static T[] ReadArray<T>(
        ref XFileReadContext context,
        int count,
        XFileValueReader<T> reader)
    {
        if (count is < 0 or > 100_000)
        {
            throw new InvalidDataException(
                $"Invalid array count {count:N0} for {typeof(T).Name} at zone offset 0x{context.Position:X8} ({context.Position:N0}).");
        }

        var values = new T[count];
        for (var i = 0; i < count; i++)
            values[i] = reader(ref context);

        return values;
    }

    private static ZonePointer<T>[] ReadPointerArray<T>(
        ref XFileReadContext context,
        int count,
        string? fieldPath = null)
    {
        if (count is < 0 or > 100_000)
        {
            throw new InvalidDataException(
                $"Invalid pointer array count {count:N0} for {typeof(T).Name} at zone offset 0x{context.Position:X8} ({context.Position:N0}).");
        }

        var pointers = new ZonePointer<T>[count];
        for (var i = 0; i < count; i++)
            pointers[i] = context.ReadDirectPointer<T>(fieldPath is null ? $"{typeof(T).Name}[][{i}]" : $"{fieldPath}[{i}]");

        return pointers;
    }

    private static void ReadInt32Array(ref XFileReadContext context, int[] values)
    {
        ReadInt32Array(ref context, values, values.Length);
    }

    private static void ReadInt32Array(ref XFileReadContext context, int[] values, int count)
    {
        if (count < 0 || count > values.Length)
        {
            throw new InvalidDataException(
                $"Invalid Int32 array count {count:N0} for array length {values.Length:N0} at zone offset 0x{context.Position:X8} ({context.Position:N0}).");
        }

        for (var i = 0; i < count; i++)
            values[i] = context.ReadInt32();
    }
}
