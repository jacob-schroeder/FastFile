using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Dvar;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Writers;

internal static class MenufileWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        switch (asset)
        {
            case MenuList menuList:
                WriteMenuList(context, menuList);
                break;
            case MenuDef menuDef:
                WriteMenuDef(context, menuDef, windowDynamicFlagCount: 2);
                break;
            default:
                throw new InvalidDataException($"Unsupported menu file asset model {asset.GetType().Name}.");
        }
    }

    private static void WriteMenuList(ZoneWriterContext context, MenuList asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WriteInt32(asset.MenuCount);
        context.WritePointer(asset.Menus, (pointerContext, pointer) =>
        {
            foreach (var menuPointer in pointer.Result ?? [])
                pointerContext.WritePointer(menuPointer, (menuContext, p) =>
                {
                    if (p.Result is { } menu)
                        WriteMenuDef(menuContext, menu);
                });
        });
    }

    private static void WriteMenuDef(
        ZoneWriterContext context,
        MenuDef asset,
        int? windowDynamicFlagCount = null)
    {
        WriteWindow(context, asset.Window, resolvePointers: false, windowDynamicFlagCount);
        context.WritePointerRaw(asset.FontPtr);
        context.WriteInt32(asset.Fullscreen);
        context.WriteInt32(asset.ItemCount);
        context.WriteInt32(asset.FontIndex);
        WriteInt32Array(context, asset.CursorItems);
        context.WriteInt32(asset.FadeCycle);
        context.WriteFloat(asset.FadeClamp);
        context.WriteFloat(asset.FadeAmount);
        context.WriteFloat(asset.FadeInAmount);
        context.WriteFloat(asset.BlurRadius);
        context.WritePointerRaw(asset.OnOpen);
        context.WritePointerRaw(asset.OnRequestClose);
        context.WritePointerRaw(asset.OnClose);
        context.WritePointerRaw(asset.OnEsc);
        context.WritePointerRaw(asset.ExecKeys);
        context.WritePointerRaw(asset.VisibleExp);
        context.WritePointerRaw(asset.AllowedBinding);
        context.WritePointerRaw(asset.SoundName);
        context.WriteInt32(asset.ImageTrack);
        context.WriteVec4(asset.FocusColor);
        context.WritePointerRaw(asset.RectXExp);
        context.WritePointerRaw(asset.RectYExp);
        context.WritePointerRaw(asset.RectHExp);
        context.WritePointerRaw(asset.RectWExp);
#if PC
        context.WritePointerRaw(asset.OpenSoundExp);
        context.WritePointerRaw(asset.CloseSoundExp);
#endif
        context.WritePointerRaw(asset.Items);
#if !PC
        WriteMenuTransitions(context, asset.ScaleTransition);
        WriteMenuTransitions(context, asset.AlphaTransition);
        WriteMenuTransitions(context, asset.XTransition);
        WriteMenuTransitions(context, asset.YTransition);
#else
        context.WriteBytes(asset.Unknown);
#endif
        context.WritePointerRaw(asset.ExpressionData);

        QueueMenuDefPointers(context, asset);
    }

    private static void QueueMenuDefPointers(ZoneWriterContext context, MenuDef asset)
    {
        context.QueuePointer(asset.ExpressionData, WriteExpressionSupportingData);
        QueueWindowPointers(context, asset.Window);
        context.QueuePointer(asset.FontPtr, GenericWriter.WriteStringPointerValue);
        context.QueuePointer(asset.OnOpen, WriteMenuEventHandlerSetPointerValue);
        context.QueuePointer(asset.OnRequestClose, WriteMenuEventHandlerSetPointerValue);
        context.QueuePointer(asset.OnClose, WriteMenuEventHandlerSetPointerValue);
        context.QueuePointer(asset.OnEsc, WriteMenuEventHandlerSetPointerValue);
        context.QueuePointer(asset.ExecKeys, WriteItemKeyHandlerPointerValue);
        context.QueuePointer(asset.VisibleExp, WriteStatementPointerValue);
        context.QueuePointer(asset.AllowedBinding, GenericWriter.WriteStringPointerValue);
        context.QueuePointer(asset.SoundName, GenericWriter.WriteStringPointerValue);
        context.QueuePointer(asset.RectXExp, WriteStatementPointerValue);
        context.QueuePointer(asset.RectYExp, WriteStatementPointerValue);
        context.QueuePointer(asset.RectHExp, WriteStatementPointerValue);
        context.QueuePointer(asset.RectWExp, WriteStatementPointerValue);
#if PC
        context.QueuePointer(asset.OpenSoundExp, WriteStatementPointerValue);
        context.QueuePointer(asset.CloseSoundExp, WriteStatementPointerValue);
#endif
        context.QueueInlinePointerDeferred(asset.Items, (pointerContext, pointer) =>
        {
            foreach (var itemPointer in pointer.Result ?? [])
                pointerContext.WritePointer(itemPointer, WriteItemDefPointerValue);
        });
    }

    private static void WriteWindow(
        ZoneWriterContext context,
        Window window,
        bool resolvePointers = true,
        int? dynamicFlagCount = null)
    {
        if (resolvePointers)
            GenericWriter.WriteStringPointer(context, window.NamePtr);
        else
            context.WritePointerRaw(window.NamePtr);

        WriteRectangle(context, window.Rect);
        WriteRectangle(context, window.RectClient);

        if (resolvePointers)
            GenericWriter.WriteStringPointer(context, window.GroupPtr);
        else
            context.WritePointerRaw(window.GroupPtr);

        context.WriteInt32(window.Style);
        context.WriteInt32(window.Border);
        context.WriteInt32(window.OwnerDraw);
        context.WriteInt32(window.OwnerDrawFlags);
        context.WriteFloat(window.BorderSize);
        context.WriteInt32(window.StaticFlags);
        WriteInt32Array(context, window.DynamicFlags, dynamicFlagCount ?? window.DynamicFlags.Length);
        context.WriteInt32(window.NextTime);
        context.WriteVec4(window.ForeColor);
        context.WriteVec4(window.BackColor);
        context.WriteVec4(window.BorderColor);
        context.WriteVec4(window.OutlineColor);
        context.WriteVec4(window.DisableColor);

        if (resolvePointers)
            MaterialWriter.WriteMaterialPointer(context, window.Background);
        else
            context.WritePointerRaw(window.Background);
    }

    private static void QueueWindowPointers(ZoneWriterContext context, Window window)
    {
        context.QueuePointer(window.NamePtr, GenericWriter.WriteStringPointerValue);
        context.QueuePointer(window.GroupPtr, GenericWriter.WriteStringPointerValue);
        context.QueuePointer(window.Background, MaterialWriter.WriteMaterialPointerValue);
    }

    private static void WriteRectangle(ZoneWriterContext context, RectangleDef rectangle)
    {
        context.WriteFloat(rectangle.X);
        context.WriteFloat(rectangle.Y);
        context.WriteFloat(rectangle.W);
        context.WriteFloat(rectangle.H);
        context.WriteByte(rectangle.HorzAlign);
        context.WriteByte(rectangle.VertAlign);
        context.WriteUInt16(rectangle.AlignmentPadding);
    }

    private static void WriteItemDefPointerValue(ZoneWriterContext context, ZonePointer<ItemDef> pointer)
    {
        if (pointer.Result is { } item)
            WriteItemDef(context, item);
    }

    private static void WriteItemDef(ZoneWriterContext context, ItemDef item)
    {
        WriteWindow(context, item.Window);
        foreach (var rect in item.TextRect)
            WriteRectangle(context, rect);
        context.WriteInt32(item.Type);
        context.WriteInt32(item.DataType);
        context.WriteInt32(item.Align);
        context.WriteInt32(item.FontEnum);
        context.WriteInt32(item.TextAlignMode);
        context.WriteFloat(item.TextAlignX);
        context.WriteFloat(item.TextAlignY);
        context.WriteFloat(item.TextScale);
        context.WriteInt32(item.TextStyle);
        context.WriteInt32(item.GameMsgWindowIndex);
        context.WriteInt32(item.GameMsgWindowMode);
        GenericWriter.WriteStringPointer(context, item.Text);
        context.WriteInt32(item.TextSaveGameInfo);
        context.WritePointerRaw(item.Parent);
        WriteMenuEventHandlerSetPointer(context, item.MouseEnterText);
        WriteMenuEventHandlerSetPointer(context, item.MouseExitText);
        WriteMenuEventHandlerSetPointer(context, item.MouseEnter);
        WriteMenuEventHandlerSetPointer(context, item.MouseExit);
        WriteMenuEventHandlerSetPointer(context, item.Action);
        WriteMenuEventHandlerSetPointer(context, item.Accept);
        WriteMenuEventHandlerSetPointer(context, item.OnFocus);
        WriteMenuEventHandlerSetPointer(context, item.LeaveFocus);
        GenericWriter.WriteStringPointer(context, item.Dvar);
        GenericWriter.WriteStringPointer(context, item.DvarTest);
        WriteItemKeyHandlerPointer(context, item.OnKey);
        GenericWriter.WriteStringPointer(context, item.EnableDvar);
        context.WriteInt32(item.DvarFlags);
        WriteReferencePointer(context, item.FocusSound, "SndAliasList");
        context.WriteFloat(item.Special);
        WriteInt32Array(context, item.CursorPos);
        WriteItemDefData(context, item.TypeData, item.Type);
        context.WriteInt32(item.ImageTrack);
        context.WriteInt32(item.FloatExpressionCount);
        context.WritePointer(item.FloatExpressions, (pointerContext, pointer) =>
        {
            foreach (var expression in pointer.Result ?? [])
                WriteItemFloatExpression(pointerContext, expression);
        });
        WriteStatementPointer(context, item.VisibleExp);
        WriteStatementPointer(context, item.DisabledExp);
        WriteStatementPointer(context, item.TextExp);
        WriteStatementPointer(context, item.MaterialExp);
        context.WriteVec4(item.GlowColor);
        context.WriteBool(item.DecayActive);
        context.WriteByte(item.DecayActivePadding0);
        context.WriteByte(item.DecayActivePadding1);
        context.WriteByte(item.DecayActivePadding2);
        context.WriteInt32(item.FxBirthTime);
        context.WriteInt32(item.FxLetterTime);
        context.WriteInt32(item.FxDecayStartTime);
        context.WriteInt32(item.FxDecayDuration);
        context.WriteInt32(item.LastSoundPlayedTime);
    }

    private static void WriteItemDefData(
        ZoneWriterContext context,
        ItemDefData data,
        int itemType)
    {
        switch (itemType)
        {
            case 0:
                context.WritePointer(data.Data, (pointerContext, pointer) =>
                {
                    foreach (var value in pointer.Result?.Words ?? [])
                        pointerContext.WriteInt32(value);
                });
                break;
            case 4:
            case 9:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                context.WritePointer(data.EditField, WriteEditFieldDefPointerValue);
                break;
            case 6:
                context.WritePointer(data.ListBox, WriteListBoxDefPointerValue);
                break;
            case 10:
            case 12:
                context.WritePointer(data.Multi, WriteMultiDefPointerValue);
                break;
            case 13:
                GenericWriter.WriteStringPointer(context, data.EnumDvarName);
                break;
            case 20:
                context.WritePointer(data.NewsTicker, WriteNewsTickerDefPointerValue);
                break;
            case 21:
                context.WritePointer(data.TextScroll, WriteTextScrollDefPointerValue);
                break;
            default:
                context.WriteInt32(data.Raw);
                break;
        }
    }

    private static void WriteStatementPointer(ZoneWriterContext context, ZonePointer<Statement> pointer)
    {
        context.WritePointer(pointer, WriteStatementPointerValue);
    }

    private static void WriteStatementPointerValue(ZoneWriterContext context, ZonePointer<Statement> pointer)
    {
        if (pointer.Result is { } statement)
            WriteStatement(context, statement);
    }

    private static void WriteStatement(ZoneWriterContext context, Statement statement)
    {
        context.WriteInt32(statement.NumEntries);
        context.WritePointer(statement.Entries, (pointerContext, pointer) =>
        {
            foreach (var entry in pointer.Result ?? [])
                WriteExpressionEntry(pointerContext, entry);
        });
        context.WritePointerRaw(statement.SupportingData);
        context.WriteInt32(statement.LastExecuteTime);
        WriteOperand(context, statement.LastResult);
    }

    private static void WriteExpressionEntry(ZoneWriterContext context, ExpressionEntry entry)
    {
        context.WriteInt32(entry.Type);
        if (entry.Type != 1)
        {
            context.WriteInt32((int)entry.Data.Op);
            context.WriteInt32(entry.Data.Operand?.Internals?.IntVal ?? 0);
            return;
        }

        var operand = entry.Data.Operand;
        WriteOperand(context, operand);
    }

    private static void WriteOperand(ZoneWriterContext context, Operand operand)
    {
        context.WriteInt32((int)operand.DataType);
        switch (operand.DataType)
        {
            case ExpDataType.VAL_STRING:
                context.WritePointerRaw(operand.Internals.StringVal);
                context.QueuePointer(operand.Internals.StringVal, WriteExpressionStringPointerValue);
                break;
            case ExpDataType.VAL_FUNCTION:
                context.WritePointerRaw(operand.Internals.Function);
                context.QueuePointer(operand.Internals.Function, WriteStatementPointerValue);
                break;
            case ExpDataType.VAL_FLOAT:
                context.WriteInt32(BitConverter.SingleToInt32Bits(operand.Internals.FloatVal));
                break;
            default:
                context.WriteInt32(operand.Internals.IntVal);
                break;
        }
    }

    private static void WriteExpressionStringPointerValue(
        ZoneWriterContext context,
        ZonePointer<ExpressionString> pointer)
    {
        context.WriteCString(pointer.Result?.StringPtr.Result);
    }

    private static void WriteMenuEventHandlerSetPointer(
        ZoneWriterContext context,
        ZonePointer<MenuEventHandlerSet> pointer)
    {
        context.WritePointer(pointer, WriteMenuEventHandlerSetPointerValue);
    }

    private static void WriteMenuEventHandlerSetPointerValue(
        ZoneWriterContext context,
        ZonePointer<MenuEventHandlerSet> pointer)
    {
        if (pointer.Result is not { } set)
            return;

        context.WriteInt32(set.EventHandlerCount);
        context.WritePointer(set.EventHandlers, (pointerContext, eventHandlersPointer) =>
        {
            foreach (var eventHandlerPointer in eventHandlersPointer.Result ?? [])
                pointerContext.WritePointer(eventHandlerPointer, WriteMenuEventHandlerPointerValue);
        });
    }

    private static void WriteMenuEventHandlerPointerValue(
        ZoneWriterContext context,
        ZonePointer<MenuEventHandler> pointer)
    {
        if (pointer.Result is not { } handler)
            return;

        WriteEventDataPointer(context, handler);
        context.WriteByte(handler.EventType);
        context.WriteByte(handler.EventTypePadding0);
        context.WriteByte(handler.EventTypePadding1);
        context.WriteByte(handler.EventTypePadding2);
    }

    private static void WriteEventDataPointer(ZoneWriterContext context, MenuEventHandler handler)
    {
        switch (handler.EventType)
        {
            case 0:
                context.WritePointerRaw(handler.EventData.UnconditionalScript);
                context.QueuePointer(handler.EventData.UnconditionalScript, GenericWriter.WriteStringPointerValue);
                break;
            case 1:
                context.WritePointerRaw(handler.EventData.ConditionalScript);
                context.QueuePointer(handler.EventData.ConditionalScript, WriteConditionalScriptPointerValue);
                break;
            case 2:
                context.WritePointerRaw(handler.EventData.ElseScript);
                context.QueuePointer(handler.EventData.ElseScript, WriteMenuEventHandlerSetPointerValue);
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                context.WritePointerRaw(handler.EventData.SetLocalVarData);
                context.QueuePointer(handler.EventData.SetLocalVarData, WriteSetLocalVarDataPointerValue);
                break;
            default:
                context.WriteInt32(handler.EventData.Raw);
                break;
        }
    }

    private static void WriteConditionalScriptPointerValue(
        ZoneWriterContext context,
        ZonePointer<ConditionalScript> pointer)
    {
        if (pointer.Result is not { } script)
            return;

        context.WritePointerRaw(script.EventHandlerSet);
        context.WritePointerRaw(script.EventExpression);
        context.QueuePointer(script.EventExpression, WriteStatementPointerValue);
        context.QueuePointer(script.EventHandlerSet, WriteMenuEventHandlerSetPointerValue);
    }

    private static void WriteSetLocalVarDataPointerValue(
        ZoneWriterContext context,
        ZonePointer<SetLocalVarData> pointer)
    {
        if (pointer.Result is not { } data)
            return;

        GenericWriter.WriteStringPointer(context, data.LocalVarName);
        WriteStatementPointer(context, data.Expression);
    }

    private static void WriteItemKeyHandlerPointer(
        ZoneWriterContext context,
        ZonePointer<ItemKeyHandler> pointer)
    {
        context.WritePointer(pointer, WriteItemKeyHandlerPointerValue);
    }

    private static void WriteItemKeyHandlerPointerValue(
        ZoneWriterContext context,
        ZonePointer<ItemKeyHandler> pointer)
    {
        if (pointer.Result is not { } handler)
            return;

        context.WriteInt32(handler.Key);
        WriteMenuEventHandlerSetPointer(context, handler.Action);
        WriteItemKeyHandlerPointer(context, handler.Next);
    }

    private static void WriteItemFloatExpression(
        ZoneWriterContext context,
        ItemFloatExpression expression)
    {
        context.WriteInt32((int)expression.Target);
        WriteStatementPointer(context, expression.Expression);
    }

    private static void WriteListBoxDefPointerValue(
        ZoneWriterContext context,
        ZonePointer<ListBoxDef> pointer)
    {
        if (pointer.Result is not { } listBox)
            return;

#if !PC
        WriteInt32Array(context, listBox.StartPos);
        WriteInt32Array(context, listBox.EndPos);
        context.WriteInt32(listBox.DrawPadding);
#else
        WriteInt32Array(context, listBox.Unknown);
#endif
        context.WriteFloat(listBox.ElementWidth);
        context.WriteFloat(listBox.ElementHeight);
        context.WriteInt32(listBox.ElementStyle);
        context.WriteInt32(listBox.NumColumns);
        foreach (var column in listBox.ColumnInfo)
            WriteColumnInfo(context, column);
        WriteMenuEventHandlerSetPointer(context, listBox.DoubleClick);
        context.WriteInt32(listBox.NotSelectable);
        context.WriteInt32(listBox.NoScrollbars);
        context.WriteInt32(listBox.UsePaging);
        context.WriteVec4(listBox.SelectBorder);
        MaterialWriter.WriteMaterialPointer(context, listBox.SelectIcon);
    }

    private static void WriteColumnInfo(ZoneWriterContext context, ColumnInfo column)
    {
        context.WriteInt32(column.Pos);
        context.WriteInt32(column.Width);
        context.WriteInt32(column.MaxChars);
        context.WriteInt32(column.Alignment);
    }

    private static void WriteEditFieldDefPointerValue(
        ZoneWriterContext context,
        ZonePointer<EditFieldDef> pointer)
    {
        if (pointer.Result is not { } editField)
            return;

        context.WriteFloat(editField.MinVal);
        context.WriteFloat(editField.MaxVal);
        context.WriteFloat(editField.DefVal);
        context.WriteFloat(editField.Range);
        context.WriteInt32(editField.MaxChars);
        context.WriteInt32(editField.MaxCharsGotoNext);
        context.WriteInt32(editField.MaxPaintChars);
        context.WriteInt32(editField.PaintOffset);
    }

    private static void WriteMultiDefPointerValue(
        ZoneWriterContext context,
        ZonePointer<MultiDef> pointer)
    {
        if (pointer.Result is not { } multi)
            return;

        foreach (var value in multi.DvarList)
            GenericWriter.WriteStringPointer(context, value);
        foreach (var value in multi.DvarStr)
            GenericWriter.WriteStringPointer(context, value);
        foreach (var value in multi.DvarValue)
            context.WriteFloat(value);
        context.WriteInt32(multi.Count);
        context.WriteInt32(multi.StrDef);
    }

    private static void WriteNewsTickerDefPointerValue(
        ZoneWriterContext context,
        ZonePointer<NewsTickerDef> pointer)
    {
        if (pointer.Result is not { } newsTicker)
            return;

        context.WriteInt32(newsTicker.FeedId);
        context.WriteInt32(newsTicker.Speed);
        context.WriteInt32(newsTicker.Spacing);
        context.WriteInt32(newsTicker.LastTime);
        context.WriteInt32(newsTicker.Start);
        context.WriteInt32(newsTicker.End);
        context.WriteFloat(newsTicker.X);
    }

    private static void WriteTextScrollDefPointerValue(
        ZoneWriterContext context,
        ZonePointer<TextScrollDef> pointer)
    {
        if (pointer.Result is { } textScroll)
            context.WriteInt32(textScroll.StartTime);
    }

    private static void WriteExpressionSupportingData(
        ZoneWriterContext context,
        ZonePointer<ExpressionSupportingData> pointer)
    {
        if (pointer.Result is not { } data)
            return;

        WriteUIFunctionList(context, data.UiFunctions);
        WriteStaticDvarList(context, data.StaticDvarList);
        WriteStringList(context, data.UiStrings);
    }

    private static void WriteUIFunctionList(ZoneWriterContext context, UIFunctionList list)
    {
        context.WriteInt32(list.TotalFunctions);
        context.WritePointer(list.Functions, (pointerContext, pointer) =>
        {
            foreach (var functionPointer in pointer.Result ?? [])
                pointerContext.WritePointer(functionPointer, WriteStatementPointerValue);
        });
    }

    private static void WriteStaticDvarList(ZoneWriterContext context, StaticDvarList list)
    {
        context.WriteInt32(list.NumStaticDvars);
        context.WritePointer(list.StaticDvars, (pointerContext, pointer) =>
        {
            foreach (var staticDvarPointer in pointer.Result ?? [])
                pointerContext.WritePointer(staticDvarPointer, WriteStaticDvarPointerValue);
        });
    }

    private static void WriteStaticDvarPointerValue(
        ZoneWriterContext context,
        ZonePointer<StaticDvar> pointer)
    {
        if (pointer.Result is not { } staticDvar)
            return;

        WriteReferencePointer(context, staticDvar.Dvar, "Dvar");
        GenericWriter.WriteStringPointer(context, staticDvar.DvarName);
    }

    private static void WriteStringList(ZoneWriterContext context, StringList list)
    {
        context.WriteInt32(list.TotalStrings);
        context.WritePointer(list.Strings, (pointerContext, pointer) =>
        {
            foreach (var stringPointer in pointer.Result ?? [])
                GenericWriter.WriteStringPointer(pointerContext, stringPointer);
        });
    }

    private static void WriteMenuTransitions(ZoneWriterContext context, MenuTransition[] transitions)
    {
        foreach (var transition in transitions)
            WriteMenuTransition(context, transition);
    }

    private static void WriteMenuTransition(ZoneWriterContext context, MenuTransition transition)
    {
        context.WriteInt32((int)transition.TransitionType);
        context.WriteInt32(transition.TargetField);
        context.WriteInt32(transition.StartTime);
        context.WriteFloat(transition.StartVal);
        context.WriteFloat(transition.EndVal);
        context.WriteFloat(transition.Time);
        context.WriteInt32((int)transition.EndTriggerType);
    }

    private static void WriteReferencePointer<T>(
        ZoneWriterContext context,
        ZonePointer<T> pointer,
        string typeName)
    {
        context.WritePointer(pointer, (_, _) =>
        {
            throw new InvalidDataException($"Inline {typeName} writing is not implemented.");
        });
    }

    private static void WriteInt32Array(ZoneWriterContext context, int[] values)
    {
        WriteInt32Array(context, values, values.Length);
    }

    private static void WriteInt32Array(ZoneWriterContext context, int[] values, int count)
    {
        if (count < 0 || count > values.Length)
            throw new InvalidDataException($"Invalid Int32 array count {count:N0} for array length {values.Length:N0}.");

        for (var i = 0; i < count; i++)
            context.WriteInt32(values[i]);
    }
}
