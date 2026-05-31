using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.Dvar;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class MenuReader
{
    public static MenuDef Read(ref ZoneReadContext context)
    {
        return Read(ref context, windowDynamicFlagCount: null);
    }

    public static MenuDef Read(ref ZoneReadContext context, int? windowDynamicFlagCount)
    {
        var start = context.Position;
        context.Trace?.Invoke("MenuDef.Start", start, start);
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
        context.Trace?.Invoke(
            $"MenuDef.Fields font=0x{asset.FontPtr.Raw:X8} fullscreen={asset.Fullscreen} itemCount={asset.ItemCount} fontIndex={asset.FontIndex} cursorItems=[{string.Join(", ", asset.CursorItems)}]",
            start,
            context.Position);

        var fadeStart = context.Position;
        asset.FadeCycle = context.ReadInt32();
        asset.FadeClamp = context.ReadFloat();
        asset.FadeAmount = context.ReadFloat();
        asset.FadeInAmount = context.ReadFloat();
        asset.BlurRadius = context.ReadFloat();
        context.Trace?.Invoke(
            $"MenuDef.Fade cycle={asset.FadeCycle} clamp={asset.FadeClamp} amount={asset.FadeAmount} in={asset.FadeInAmount} blur={asset.BlurRadius}",
            fadeStart,
            context.Position);

        var eventsStart = context.Position;
        asset.OnOpen = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnRequestClose = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnClose = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.OnEsc = ReadMenuEventHandlerSetPointer(ref context, resolve: false);
        asset.ExecKeys = ReadItemKeyHandlerPointer(ref context, resolve: false);
        asset.VisibleExp = ReadStatementPointer(ref context, resolve: false);
        asset.AllowedBinding = ReadStringPointer(ref context, resolve: false);
        asset.SoundName = ReadStringPointer(ref context, resolve: false);
        asset.ImageTrack = context.ReadInt32();
        context.Trace?.Invoke(
            $"MenuDef.EventPointers onOpen=0x{asset.OnOpen.Raw:X8} onRequestClose=0x{asset.OnRequestClose.Raw:X8} onClose=0x{asset.OnClose.Raw:X8} onEsc=0x{asset.OnEsc.Raw:X8} execKeys=0x{asset.ExecKeys.Raw:X8} visible=0x{asset.VisibleExp.Raw:X8} allowed=0x{asset.AllowedBinding.Raw:X8} sound=0x{asset.SoundName.Raw:X8} imageTrack={asset.ImageTrack}",
            eventsStart,
            context.Position);

        var focusColorOffset = context.Position;
        asset.FocusColor = context.ReadVec4();
        context.Trace?.Invoke(
            $"MenuDef.FocusColor ({asset.FocusColor.R}, {asset.FocusColor.G}, {asset.FocusColor.B}, {asset.FocusColor.A})",
            focusColorOffset,
            context.Position);
        asset.RectXExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectYExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectHExp = ReadStatementPointer(ref context, resolve: false);
        asset.RectWExp = ReadStatementPointer(ref context, resolve: false);
        context.Trace?.Invoke(
            $"MenuDef.RectExp x=0x{asset.RectXExp.Raw:X8} y=0x{asset.RectYExp.Raw:X8} h=0x{asset.RectHExp.Raw:X8} w=0x{asset.RectWExp.Raw:X8}",
            focusColorOffset,
            context.Position);
#if PC
        asset.OpenSoundExp = ReadStatementPointer(ref context, resolve: false);
        asset.CloseSoundExp = ReadStatementPointer(ref context, resolve: false);
#endif
        var itemsPointerOffset = context.Position;
        asset.Items = context.ReadPointer<ZonePointer<ItemDef>[]>();
        context.Trace?.Invoke($"MenuDef.Items raw=0x{asset.Items.Raw:X8} itemCount={asset.ItemCount}", itemsPointerOffset, context.Position);

        var transitionsStart = context.Position;
#if !PC
        asset.ScaleTransition = ReadMenuTransitions(ref context);
        asset.AlphaTransition = ReadMenuTransitions(ref context);
        asset.XTransition = ReadMenuTransitions(ref context);
        asset.YTransition = ReadMenuTransitions(ref context);
#else
        asset.Unknown = context.ReadBytes(112);
#endif
        context.Trace?.Invoke("MenuDef.Transitions", transitionsStart, context.Position);
        var expressionDataOffset = context.Position;
        asset.ExpressionData = ReadExpressionSupportingDataPointer(ref context, resolve: false);
        context.Trace?.Invoke($"MenuDef.ExpressionData raw=0x{asset.ExpressionData.Raw:X8}", expressionDataOffset, context.Position);

        ResolveMenuDefPointers(ref context, asset);
        context.Trace?.Invoke("MenuDef.Header", start, context.Position);

        return asset;
    }

    private static Window ReadWindow(
        ref ZoneReadContext context,
        bool resolvePointers = true,
        int? dynamicFlagCount = null)
    {
        var start = context.Position;
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
        window.Background = context.ReadPointer<FastFile.Models.Assets.Material.Material>();
        if (resolvePointers)
        {
            context.ResolveInlinePointer(
                window.Background,
                (ref ZoneReadContext pointerContext, ZonePointer<FastFile.Models.Assets.Material.Material> pointer) =>
                {
                    pointer.SetResult(pointerContext.ReadPointerValue(pointer, MaterialReader.Read));
                });
        }

        context.Trace?.Invoke(
            $"Window name=0x{window.NamePtr.Raw:X8} group=0x{window.GroupPtr.Raw:X8} rect=({window.Rect.X}, {window.Rect.Y}, {window.Rect.W}, {window.Rect.H}) client=({window.RectClient.X}, {window.RectClient.Y}, {window.RectClient.W}, {window.RectClient.H}) style={window.Style} border={window.Border} ownerDraw={window.OwnerDraw} ownerDrawFlags=0x{window.OwnerDrawFlags:X8} borderSize={window.BorderSize} staticFlags=0x{window.StaticFlags:X8} dynamicFlags=[{string.Join(", ", window.DynamicFlags)}] nextTime={window.NextTime} fore=({window.ForeColor.R}, {window.ForeColor.G}, {window.ForeColor.B}, {window.ForeColor.A}) back=({window.BackColor.R}, {window.BackColor.G}, {window.BackColor.B}, {window.BackColor.A}) background=0x{window.Background.Raw:X8}",
            start,
            context.Position);

        return window;
    }

    private static void ResolveWindowPointers(ref ZoneReadContext context, Window window)
    {
        context.ResolveInlinePointer(window.NamePtr, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(window.GroupPtr, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(
            window.Background,
            (ref ZoneReadContext pointerContext, ZonePointer<FastFile.Models.Assets.Material.Material> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, MaterialReader.Read));
            });
    }

    private static void ResolveMenuDefPointers(ref ZoneReadContext context, MenuDef asset)
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
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<ItemDef>[]> pointer) =>
            {
                var items = ReadPointerArray<ItemDef>(ref pointerContext, asset.ItemCount);
                pointer.SetResult(items);

                foreach (var itemPointer in items)
                    pointerContext.ResolveInlinePointer(itemPointer, ReadItemDefPointerValue);
            });
    }

    private static RectangleDef ReadRectangle(ref ZoneReadContext context)
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

        context.Position += 2;
        return rectangle;
    }

    private static ItemDef ReadItemDef(ref ZoneReadContext context)
    {
        var start = context.Position;
        var item = new ItemDef
        {
            Window = ReadWindow(ref context),
        };
        context.Trace?.Invoke(
            $"ItemDef.Window name=0x{item.Window.NamePtr.Raw:X8} group=0x{item.Window.GroupPtr.Raw:X8} rect=({item.Window.Rect.X}, {item.Window.Rect.Y}, {item.Window.Rect.W}, {item.Window.Rect.H}) client=({item.Window.RectClient.X}, {item.Window.RectClient.Y}, {item.Window.RectClient.W}, {item.Window.RectClient.H}) style={item.Window.Style} border={item.Window.Border} ownerDraw={item.Window.OwnerDraw} staticFlags=0x{item.Window.StaticFlags:X8} background=0x{item.Window.Background.Raw:X8}",
            start,
            context.Position);

        for (var i = 0; i < item.TextRect.Length; i++)
            item.TextRect[i] = ReadRectangle(ref context);
        var firstTextRect = item.TextRect.Length > 0 ? item.TextRect[0] : null;
        context.Trace?.Invoke(
            firstTextRect is null
                ? "ItemDef.TextRect none"
                : $"ItemDef.TextRect[0]=({firstTextRect.X}, {firstTextRect.Y}, {firstTextRect.W}, {firstTextRect.H}) align=({firstTextRect.HorzAlign}, {firstTextRect.VertAlign}) count={item.TextRect.Length}",
            start,
            context.Position);

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
        context.Trace?.Invoke(
            $"ItemDef.Basic type={item.Type} dataType={item.DataType} align={item.Align}",
            start,
            context.Position);
        item.Text = ReadStringPointer(ref context);
        item.TextSaveGameInfo = context.ReadInt32();
        item.Parent = context.ReadPointer<MenuDef>();
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
        context.Trace?.Invoke(
            $"ItemDef.Events text=0x{item.Text.Raw:X8} parent=0x{item.Parent.Raw:X8} mouseEnterText=0x{item.MouseEnterText.Raw:X8} mouseExitText=0x{item.MouseExitText.Raw:X8} mouseEnter=0x{item.MouseEnter.Raw:X8} mouseExit=0x{item.MouseExit.Raw:X8} action=0x{item.Action.Raw:X8} accept=0x{item.Accept.Raw:X8} onFocus=0x{item.OnFocus.Raw:X8} leaveFocus=0x{item.LeaveFocus.Raw:X8} dvar=0x{item.Dvar.Raw:X8} dvarTest=0x{item.DvarTest.Raw:X8} onKey=0x{item.OnKey.Raw:X8} enableDvar=0x{item.EnableDvar.Raw:X8} dvarFlags=0x{item.DvarFlags:X8}",
            start,
            context.Position);
        item.FocusSound = context.ReadPointer<FastFile.Models.Assets.SoundAliasList.SndAliasList>();
        item.Special = context.ReadFloat();
        ReadInt32Array(ref context, item.CursorPos);
        item.TypeData = ReadItemDefData(ref context, item.Type);
        item.ImageTrack = context.ReadInt32();
        item.FloatExpressionCount = context.ReadInt32();
        context.Trace?.Invoke(
            $"ItemDef.TypeData raw=0x{item.TypeData.Raw:X8} focusSound=0x{item.FocusSound.Raw:X8} floatExpressionCount={item.FloatExpressionCount}",
            start,
            context.Position);
        item.FloatExpressions = context.ReadInlinePointer<ItemFloatExpression[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ItemFloatExpression[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, item.FloatExpressionCount, ReadItemFloatExpression);
                pointer.SetResult(values);
            });
        item.VisibleExp = ReadStatementPointer(ref context);
        item.DisabledExp = ReadStatementPointer(ref context);
        item.TextExp = ReadStatementPointer(ref context);
        item.MaterialExp = ReadStatementPointer(ref context);
        var glowColorOffset = context.Position;
        item.GlowColor = context.ReadVec4();
        context.Trace?.Invoke(
            $"ItemDef.GlowColor floatExpressions=0x{item.FloatExpressions.Raw:X8} visible=0x{item.VisibleExp.Raw:X8} disabled=0x{item.DisabledExp.Raw:X8} textExp=0x{item.TextExp.Raw:X8} materialExp=0x{item.MaterialExp.Raw:X8}",
            glowColorOffset,
            context.Position);
        var decayActiveOffset = context.Position;
        item.DecayActive = context.ReadBool();
        context.Trace?.Invoke("ItemDef.DecayActive", decayActiveOffset, context.Position);
        context.Position += 3;
        item.FxBirthTime = context.ReadInt32();
        item.FxLetterTime = context.ReadInt32();
        item.FxDecayStartTime = context.ReadInt32();
        item.FxDecayDuration = context.ReadInt32();
        item.LastSoundPlayedTime = context.ReadInt32();
        context.Trace?.Invoke("ItemDef", start, context.Position);

        return item;
    }

    private static ItemDefData ReadItemDefData(ref ZoneReadContext context, int itemType)
    {
        var raw = context.ReadInt32();
        var data = new ItemDefData
        {
            Raw = raw,
            ListBox = new ZonePointer<ListBoxDef>(raw),
            EditField = new ZonePointer<EditFieldDef>(raw),
            Multi = new ZonePointer<MultiDef>(raw),
            EnumDvarName = new ZonePointer<string>(raw),
            NewsTicker = new ZonePointer<NewsTickerDef>(raw),
            TextScroll = new ZonePointer<TextScrollDef>(raw),
            Data = new ZonePointer<byte[]>(raw),
        };

        switch (itemType)
        {
            case 0:
                context.ResolveInlinePointer(data.Data, ReadUnknownItemDataPointerValue);
                break;
            case 4:
            case 9:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                context.ResolveInlinePointer(data.EditField, ReadEditFieldDefPointerValue);
                break;
            case 6:
                context.ResolveInlinePointer(data.ListBox, ReadListBoxDefPointerValue);
                break;
            case 10:
            case 12:
                context.ResolveInlinePointer(data.Multi, ReadMultiDefPointerValue);
                break;
            case 13:
                context.ResolveInlinePointer(data.EnumDvarName, GenericReader.ReadStringPointerValue);
                break;
            case 20:
                context.ResolveInlinePointer(data.NewsTicker, ReadNewsTickerDefPointerValue);
                break;
            case 21:
                context.ResolveInlinePointer(data.TextScroll, ReadTextScrollDefPointerValue);
                break;
        }

        return data;
    }

    private static void ReadUnknownItemDataPointerValue(ref ZoneReadContext context, ZonePointer<byte[]> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, (ref ZoneReadContext valueContext) => valueContext.ReadBytes(0x20)));
    }

    private static Statement ReadStatement(ref ZoneReadContext context)
    {
        var statement = new Statement
        {
            NumEntries = context.ReadInt32(),
        };

        statement.Entries = context.ReadInlinePointer<ExpressionEntry[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ExpressionEntry[]> pointer) =>
            {
                var entries = ReadArray(ref pointerContext, statement.NumEntries, ReadExpressionEntry);
                pointer.SetResult(entries);
            });
        statement.SupportingData = ReadExpressionSupportingDataPointer(ref context, resolve: false);
        statement.Unknown = context.ReadBytes(0xC);

        return statement;
    }

    private static ExpressionEntry ReadExpressionEntry(ref ZoneReadContext context)
    {
        var entry = new ExpressionEntry
        {
            Type = context.ReadInt32(),
            Data = new EntryInternalData(),
        };

        var first = context.ReadInt32();
        var second = context.ReadInt32();

        entry.Data.Op = (OperationEnum)first;
        entry.Data.Operand = new Operand
        {
            DataType = (ExpDataType)first,
            Internals = new OperandInternalData
            {
                IntVal = second,
                FloatVal = BitConverter.Int32BitsToSingle(second),
                StringVal = new ZonePointer<ExpressionString>(second),
                Function = new ZonePointer<Statement>(second),
            }
        };

        if (entry.Type != 1)
            return entry;

        switch (entry.Data.Operand.DataType)
        {
            case ExpDataType.VAL_STRING:
                if (entry.Data.Operand.Internals.StringVal.Kind == PointerKind.Inline)
                    context.ResolveInlinePointer(entry.Data.Operand.Internals.StringVal, ReadExpressionStringPointerValue);
                break;
            case ExpDataType.VAL_FUNCTION:
                if (entry.Data.Operand.Internals.Function.Kind == PointerKind.Inline)
                    context.ResolveInlinePointer(entry.Data.Operand.Internals.Function, ReadStatementPointerValue);
                break;
        }

        return entry;
    }

    private static ExpressionString ReadExpressionString(ref ZoneReadContext context)
    {
        return new ExpressionString
        {
            StringPtr = ReadStringPointer(ref context)
        };
    }

    private static MenuEventHandlerSet ReadMenuEventHandlerSet(ref ZoneReadContext context)
    {
        var start = context.Position;
        var set = new MenuEventHandlerSet
        {
            EventHandlerCount = context.ReadInt32(),
        };
        context.Trace?.Invoke(
            $"MenuEventHandlerSet count={set.EventHandlerCount}",
            start,
            context.Position);

        set.EventHandlers = context.ReadInlinePointer<ZonePointer<MenuEventHandler>[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<MenuEventHandler>[]> pointer) =>
            {
                var eventHandlers = ReadPointerArray<MenuEventHandler>(ref pointerContext, set.EventHandlerCount);
                pointer.SetResult(eventHandlers);

                foreach (var eventHandlerPointer in eventHandlers)
                    pointerContext.ResolveInlinePointer(eventHandlerPointer, ReadMenuEventHandlerPointerValue);
            });

        return set;
    }

    private static MenuEventHandler ReadMenuEventHandler(ref ZoneReadContext context)
    {
        var raw = context.ReadInt32();
        var handler = new MenuEventHandler
        {
            EventData = new EventData
            {
                Raw = raw,
                UnconditionalScript = new ZonePointer<string>(raw),
                ConditionalScript = new ZonePointer<ConditionalScript>(raw),
                ElseScript = new ZonePointer<MenuEventHandlerSet>(raw),
                SetLocalVarData = new ZonePointer<SetLocalVarData>(raw),
            },
            EventType = context.ReadByte(),
        };
        context.Position += 3;

        switch (handler.EventType)
        {
            case 0:
                context.ResolveInlinePointer(handler.EventData.UnconditionalScript, GenericReader.ReadStringPointerValue);
                break;
            case 1:
                context.ResolveInlinePointer(handler.EventData.ConditionalScript, ReadConditionalScriptPointerValue);
                break;
            case 2:
                context.ResolveInlinePointer(handler.EventData.ElseScript, ReadMenuEventHandlerSetPointerValue);
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                context.ResolveInlinePointer(handler.EventData.SetLocalVarData, ReadSetLocalVarDataPointerValue);
                break;
        }

        return handler;
    }

    private static ConditionalScript ReadConditionalScript(ref ZoneReadContext context)
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

    private static SetLocalVarData ReadSetLocalVarData(ref ZoneReadContext context)
    {
        return new SetLocalVarData
        {
            LocalVarName = ReadStringPointer(ref context),
            Expression = ReadStatementPointer(ref context),
        };
    }

    private static ItemKeyHandler ReadItemKeyHandler(ref ZoneReadContext context)
    {
        return new ItemKeyHandler
        {
            Key = context.ReadInt32(),
            Action = ReadMenuEventHandlerSetPointer(ref context),
            Next = ReadItemKeyHandlerPointer(ref context),
        };
    }

    private static ItemFloatExpression ReadItemFloatExpression(ref ZoneReadContext context)
    {
        return new ItemFloatExpression
        {
            Target = (ItemFloatExpressionTarget)context.ReadInt32(),
            Expression = ReadStatementPointer(ref context),
        };
    }

    private static ListBoxDef ReadListBoxDef(ref ZoneReadContext context)
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

    private static ColumnInfo ReadColumnInfo(ref ZoneReadContext context)
    {
        return new ColumnInfo
        {
            Pos = context.ReadInt32(),
            Width = context.ReadInt32(),
            MaxChars = context.ReadInt32(),
            Alignment = context.ReadInt32(),
        };
    }

    private static EditFieldDef ReadEditFieldDef(ref ZoneReadContext context)
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

    private static MultiDef ReadMultiDef(ref ZoneReadContext context)
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

    private static NewsTickerDef ReadNewsTickerDef(ref ZoneReadContext context)
    {
        return new NewsTickerDef
        {
            FeedId = context.ReadInt32(),
            Speed = context.ReadInt32(),
            Spacing = context.ReadInt32(),
            Unknown = context.ReadBytes(0x10),
        };
    }

    private static TextScrollDef ReadTextScrollDef(ref ZoneReadContext context)
    {
        return new TextScrollDef
        {
            StartTime = context.ReadInt32(),
        };
    }

    private static ExpressionSupportingData ReadExpressionSupportingData(ref ZoneReadContext context)
    {
        return new ExpressionSupportingData
        {
            UiFunctions = ReadUIFunctionList(ref context),
            StaticDvarList = ReadStaticDvarList(ref context),
            UiStrings = ReadStringList(ref context),
        };
    }

    private static UIFunctionList ReadUIFunctionList(ref ZoneReadContext context)
    {
        var list = new UIFunctionList
        {
            TotalFunctions = context.ReadInt32(),
        };

        list.Functions = context.ReadInlinePointer<ZonePointer<Statement>[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<Statement>[]> pointer) =>
            {
                var functions = ReadPointerArray<Statement>(ref pointerContext, list.TotalFunctions);
                pointer.SetResult(functions);
                foreach (var functionPointer in functions)
                    pointerContext.ResolveInlinePointer(functionPointer, ReadStatementPointerValue);
            });

        return list;
    }

    private static StaticDvarList ReadStaticDvarList(ref ZoneReadContext context)
    {
        var list = new StaticDvarList
        {
            NumStaticDvars = context.ReadInt32(),
        };

        list.StaticDvars = context.ReadInlinePointer<ZonePointer<StaticDvar>[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<StaticDvar>[]> pointer) =>
            {
                var staticDvars = ReadPointerArray<StaticDvar>(ref pointerContext, list.NumStaticDvars);
                pointer.SetResult(staticDvars);
                foreach (var staticDvarPointer in staticDvars)
                    pointerContext.ResolveInlinePointer(staticDvarPointer, ReadStaticDvarPointerValue);
            });

        return list;
    }

    private static StaticDvar ReadStaticDvar(ref ZoneReadContext context)
    {
        return new StaticDvar
        {
            Dvar = context.ReadPointer<Dvar>(),
            DvarName = ReadStringPointer(ref context),
        };
    }

    private static StringList ReadStringList(ref ZoneReadContext context)
    {
        var list = new StringList
        {
            TotalStrings = context.ReadInt32(),
        };

        list.Strings = context.ReadInlinePointer<ZonePointer<string>[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<string>[]> pointer) =>
            {
                var strings = ReadPointerArray<string>(ref pointerContext, list.TotalStrings);
                pointer.SetResult(strings);
                foreach (var stringPointer in strings)
                    pointerContext.ResolveInlinePointer(stringPointer, GenericReader.ReadStringPointerValue);
            });

        return list;
    }

    private static MenuTransition[] ReadMenuTransitions(ref ZoneReadContext context)
    {
        return ReadArray(ref context, 4, ReadMenuTransition);
    }

    private static MenuTransition ReadMenuTransition(ref ZoneReadContext context)
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

    private static ZonePointer<string> ReadStringPointer(ref ZoneReadContext context, bool resolve = true)
    {
        return GenericReader.ReadStringPointer(ref context, resolve);
    }

    private static ZonePointer<Statement> ReadStatementPointer(ref ZoneReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<Statement>(ReadStatementPointerValue)
            : context.ReadPointer<Statement>();
    }

    private static ZonePointer<MenuEventHandlerSet> ReadMenuEventHandlerSetPointer(ref ZoneReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<MenuEventHandlerSet>(ReadMenuEventHandlerSetPointerValue)
            : context.ReadPointer<MenuEventHandlerSet>();
    }

    private static ZonePointer<ItemKeyHandler> ReadItemKeyHandlerPointer(ref ZoneReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<ItemKeyHandler>(ReadItemKeyHandlerPointerValue)
            : context.ReadPointer<ItemKeyHandler>();
    }

    private static ZonePointer<ExpressionSupportingData> ReadExpressionSupportingDataPointer(ref ZoneReadContext context, bool resolve = true)
    {
        return resolve
            ? context.ReadInlinePointer<ExpressionSupportingData>(ReadExpressionSupportingDataPointerValue)
            : context.ReadPointer<ExpressionSupportingData>();
    }

    private static void ReadStatementPointerValue(ref ZoneReadContext context, ZonePointer<Statement> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadStatement));
    }

    private static void ReadExpressionStringPointerValue(ref ZoneReadContext context, ZonePointer<ExpressionString> pointer)
    {
        var value = context.ReadPointerValue(pointer, ReadDirectExpressionString);
        pointer.SetResult(value);
    }

    private static ExpressionString ReadDirectExpressionString(ref ZoneReadContext context)
    {
        var value = context.ReadCString();
        var stringPointer = new ZonePointer<string>(-1);
        stringPointer.SetResult(value);

        return new ExpressionString
        {
            StringPtr = stringPointer,
        };
    }

    private static void ReadExpressionSupportingDataPointerValue(ref ZoneReadContext context, ZonePointer<ExpressionSupportingData> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadExpressionSupportingData));
    }

    private static void ReadMenuEventHandlerSetPointerValue(ref ZoneReadContext context, ZonePointer<MenuEventHandlerSet> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMenuEventHandlerSet));
    }

    private static void ReadMenuEventHandlerPointerValue(ref ZoneReadContext context, ZonePointer<MenuEventHandler> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMenuEventHandler));
    }

    private static void ReadConditionalScriptPointerValue(ref ZoneReadContext context, ZonePointer<ConditionalScript> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadConditionalScript));
    }

    private static void ReadSetLocalVarDataPointerValue(ref ZoneReadContext context, ZonePointer<SetLocalVarData> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadSetLocalVarData));
    }

    private static void ReadItemKeyHandlerPointerValue(ref ZoneReadContext context, ZonePointer<ItemKeyHandler> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadItemKeyHandler));
    }

    private static void ReadItemDefPointerValue(ref ZoneReadContext context, ZonePointer<ItemDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadItemDef));
    }

    private static void ReadListBoxDefPointerValue(ref ZoneReadContext context, ZonePointer<ListBoxDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadListBoxDef));
    }

    private static void ReadEditFieldDefPointerValue(ref ZoneReadContext context, ZonePointer<EditFieldDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadEditFieldDef));
    }

    private static void ReadMultiDefPointerValue(ref ZoneReadContext context, ZonePointer<MultiDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadMultiDef));
    }

    private static void ReadNewsTickerDefPointerValue(ref ZoneReadContext context, ZonePointer<NewsTickerDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadNewsTickerDef));
    }

    private static void ReadTextScrollDefPointerValue(ref ZoneReadContext context, ZonePointer<TextScrollDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadTextScrollDef));
    }

    private static void ReadStaticDvarPointerValue(ref ZoneReadContext context, ZonePointer<StaticDvar> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadStaticDvar));
    }

    private static string ReadAlignedCString(ref ZoneReadContext context)
    {
        return context.ReadAlignedCString();
    }

    private static T[] ReadArray<T>(
        ref ZoneReadContext context,
        int count,
        ZoneValueReader<T> reader)
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
        ref ZoneReadContext context,
        int count)
    {
        if (count is < 0 or > 100_000)
        {
            throw new InvalidDataException(
                $"Invalid pointer array count {count:N0} for {typeof(T).Name} at zone offset 0x{context.Position:X8} ({context.Position:N0}).");
        }

        var pointers = new ZonePointer<T>[count];
        for (var i = 0; i < count; i++)
            pointers[i] = context.ReadPointer<T>();

        return pointers;
    }

    private static void ReadInt32Array(ref ZoneReadContext context, int[] values)
    {
        ReadInt32Array(ref context, values, values.Length);
    }

    private static void ReadInt32Array(ref ZoneReadContext context, int[] values, int count)
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
