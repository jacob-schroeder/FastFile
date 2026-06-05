using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Assets.XModels;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private static void WriteTechset(XFileWriterContext context, MaterialTechniqueSet techset)
    {
        var name = WriteInlineStringPointer(context, techset.NamePtr);
        context.WriteByte((byte)techset.WorldVertexFormat);
        context.WriteBool(techset.HasBeenUploaded);
        context.WriteBytes(techset.Unused);

        foreach (var technique in techset.Techniques)
            context.WritePointerRaw(technique, PointerResolutionKind.Direct, "Techset.Techniques");

        WritePendingString(context, name);
    }

    private static void WriteMenuList(XFileWriterContext context, MenuList asset)
    {
        var name = WriteInlineStringPointer(context, asset.NamePtr);
        context.WriteInt32(asset.MenuCount);
        var menus = asset.Menus;
        var writeMenus = menus is { IsInlineData: true, Result: not null };
        if (writeMenus)
            context.WriteInlinePointerMarker();
        else
            context.WritePointerRaw(menus);

        WriteQueuedMenuListPointers(context, asset);
    }

    private static void WriteQueuedMenuListPointers(XFileWriterContext context, MenuList asset)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMenuListPointers(context, asset)))
            return;

        WritePendingString(context, asset.NamePtr);

        var menus = asset.Menus;
        if (menus is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(menus);
        for (var i = 0; i < menus.Result.Length; i++)
            context.WritePointerRaw(menus.Result[i], PointerResolutionKind.Alias, $"MenuList.Menus[{i}]");

        foreach (var menuPointer in menus.Result)
        {
            if (menuPointer.Result is { } menu)
            {
                WriteInlineAssetReferenceBody(context, menuPointer, (writeContext, value) => WriteMenuDef(writeContext, value));
            }
        }
    }

    private static void WriteMenuDef(
        XFileWriterContext context,
        MenuDef asset,
        int? windowDynamicFlagCount = null)
    {
        WriteWindow(context, asset.Window, windowDynamicFlagCount);
        context.WritePointerRaw(asset.FontPtr, PointerResolutionKind.Direct, "MenuDef.Font");
        context.WriteInt32(asset.Fullscreen);
        context.WriteInt32(asset.ItemCount);
        context.WriteInt32(asset.FontIndex);
        WriteInt32Array(context, asset.CursorItems);
        context.WriteInt32(asset.FadeCycle);
        context.WriteFloat(asset.FadeClamp);
        context.WriteFloat(asset.FadeAmount);
        context.WriteFloat(asset.FadeInAmount);
        context.WriteFloat(asset.BlurRadius);
        context.WritePointerRaw(asset.OnOpen, PointerResolutionKind.Direct, "MenuDef.OnOpen");
        context.WritePointerRaw(asset.OnRequestClose, PointerResolutionKind.Direct, "MenuDef.OnRequestClose");
        context.WritePointerRaw(asset.OnClose, PointerResolutionKind.Direct, "MenuDef.OnClose");
        context.WritePointerRaw(asset.OnEsc, PointerResolutionKind.Direct, "MenuDef.OnEsc");
        context.WritePointerRaw(asset.ExecKeys, PointerResolutionKind.Direct, "MenuDef.ExecKeys");
        context.WritePointerRaw(asset.VisibleExp, PointerResolutionKind.Direct, "MenuDef.VisibleExp");
        context.WritePointerRaw(asset.AllowedBinding, PointerResolutionKind.Direct, "MenuDef.AllowedBinding");
        context.WritePointerRaw(asset.SoundName, PointerResolutionKind.Direct, "MenuDef.SoundName");
        context.WriteInt32(asset.ImageTrack);
        context.WriteVec4(asset.FocusColor);
        context.WritePointerRaw(asset.RectXExp, PointerResolutionKind.Direct, "MenuDef.RectXExp");
        context.WritePointerRaw(asset.RectYExp, PointerResolutionKind.Direct, "MenuDef.RectYExp");
        context.WritePointerRaw(asset.RectHExp, PointerResolutionKind.Direct, "MenuDef.RectHExp");
        context.WritePointerRaw(asset.RectWExp, PointerResolutionKind.Direct, "MenuDef.RectWExp");
        context.WritePointerRaw(asset.Items, PointerResolutionKind.Direct, "MenuDef.Items");
        WriteMenuTransitions(context, asset.ScaleTransition);
        WriteMenuTransitions(context, asset.AlphaTransition);
        WriteMenuTransitions(context, asset.XTransition);
        WriteMenuTransitions(context, asset.YTransition);
        context.WritePointerRaw(asset.ExpressionData, PointerResolutionKind.Direct, "MenuDef.ExpressionData");

        WriteQueuedMenuDefPointers(context, asset);
    }

    private static void WriteQueuedMenuDefPointers(XFileWriterContext context, MenuDef asset)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMenuDefPointers(context, asset)))
            return;

        WriteQueuedExpressionSupportingData(context, asset.ExpressionData);
        WriteQueuedString(context, asset.Window.NamePtr);
        WriteQueuedString(context, asset.Window.GroupPtr);
        WriteQueuedMaterial(context, asset.Window.Background);
        WriteQueuedString(context, asset.FontPtr);
        WriteQueuedMenuEventHandlerSet(context, asset.OnOpen);
        WriteQueuedMenuEventHandlerSet(context, asset.OnRequestClose);
        WriteQueuedMenuEventHandlerSet(context, asset.OnClose);
        WriteQueuedMenuEventHandlerSet(context, asset.OnEsc);
        WriteQueuedItemKeyHandler(context, asset.ExecKeys);
        WriteQueuedStatement(context, asset.VisibleExp);
        WriteQueuedString(context, asset.AllowedBinding);
        WriteQueuedString(context, asset.SoundName);
        WriteQueuedStatement(context, asset.RectXExp);
        WriteQueuedStatement(context, asset.RectYExp);
        WriteQueuedStatement(context, asset.RectHExp);
        WriteQueuedStatement(context, asset.RectWExp);

        if (asset.Items is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(asset.Items);
        WritePointerArray(context, asset.Items.Result);
        foreach (var itemPointer in asset.Items.Result)
        {
            if (itemPointer.Result is { } item)
            {
                context.RegisterMaterializedPointerValue(itemPointer);
                WriteItemDef(context, item);
            }
        }
    }

    private static void WriteQueuedExpressionSupportingData(
        XFileWriterContext context,
        ZonePointer<ExpressionSupportingData>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedExpressionSupportingData(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(pointer);
            WriteExpressionSupportingData(context, pointer.Result);
        }
    }

    private static void WriteQueuedString(XFileWriterContext context, ZonePointer<string>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedString(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(pointer, GetCStringLength(pointer.Result));
            context.WriteCString(pointer.Result);
        }
    }

    private static int GetCStringLength(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? 1
            : Encoding.Latin1.GetByteCount(value) + 1;
    }

    private static void WriteQueuedStatement(XFileWriterContext context, ZonePointer<Statement>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStatement(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(pointer);
            WriteStatement(context, pointer.Result);
        }
    }

    private static void WriteQueuedMenuEventHandlerSet(
        XFileWriterContext context,
        ZonePointer<MenuEventHandlerSet>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMenuEventHandlerSet(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var set = pointer.Result;
        context.WriteInt32(set.EventHandlerCount);
        context.WritePointerRaw(set.EventHandlers);

        if (set.EventHandlers is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(set.EventHandlers);
        WritePointerArray(context, set.EventHandlers.Result);
        foreach (var handlerPointer in set.EventHandlers.Result)
        {
            if (handlerPointer.Result is { } handler)
            {
                context.RegisterMaterializedPointerValue(handlerPointer);
                WriteMenuEventHandler(context, handler);
            }
        }
    }

    private static void WriteMenuEventHandler(XFileWriterContext context, MenuEventHandler handler)
    {
        WriteEventDataPointer(context, handler);
        context.WriteByte(handler.EventType);
        context.WriteByte(handler.EventTypePadding0);
        context.WriteByte(handler.EventTypePadding1);
        context.WriteByte(handler.EventTypePadding2);
        WriteQueuedEventData(context, handler);
    }

    private static void WriteEventDataPointer(XFileWriterContext context, MenuEventHandler handler)
    {
        switch (handler.EventType)
        {
            case 0:
                context.WritePointerRaw(handler.EventData.UnconditionalScript, PointerResolutionKind.Direct, "MenuEventHandler.UnconditionalScript");
                break;
            case 1:
                context.WritePointerRaw(handler.EventData.ConditionalScript, PointerResolutionKind.Direct, "MenuEventHandler.ConditionalScript");
                break;
            case 2:
                context.WritePointerRaw(handler.EventData.ElseScript, PointerResolutionKind.Direct, "MenuEventHandler.ElseScript");
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                context.WritePointerRaw(handler.EventData.SetLocalVarData, PointerResolutionKind.Direct, "MenuEventHandler.SetLocalVarData");
                break;
            default:
                context.WriteInt32(handler.EventData.Raw);
                break;
        }
    }

    private static void WriteQueuedEventData(XFileWriterContext context, MenuEventHandler handler)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedEventData(context, handler)))
            return;

        switch (handler.EventType)
        {
            case 0:
                WriteQueuedString(context, handler.EventData.UnconditionalScript);
                break;
            case 1:
                WriteQueuedConditionalScript(context, handler.EventData.ConditionalScript);
                break;
            case 2:
                WriteQueuedMenuEventHandlerSet(context, handler.EventData.ElseScript);
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                WriteQueuedSetLocalVarData(context, handler.EventData.SetLocalVarData);
                break;
        }
    }

    private static void WriteQueuedConditionalScript(
        XFileWriterContext context,
        ZonePointer<ConditionalScript>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedConditionalScript(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var script = pointer.Result;
        context.WritePointerRaw(script.EventHandlerSet, PointerResolutionKind.Direct, "ConditionalScript.EventHandlerSet");
        context.WritePointerRaw(script.EventExpression, PointerResolutionKind.Direct, "ConditionalScript.EventExpression");
        WriteQueuedStatement(context, script.EventExpression);
        WriteQueuedMenuEventHandlerSet(context, script.EventHandlerSet);
    }

    private static void WriteQueuedSetLocalVarData(
        XFileWriterContext context,
        ZonePointer<SetLocalVarData>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedSetLocalVarData(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var data = pointer.Result;
        var localVar = WriteInlineStringPointer(context, data.LocalVarName);
        context.WritePointerRaw(data.Expression, PointerResolutionKind.Direct, "SetLocalVarData.Expression");
        WritePendingString(context, localVar);
        WriteQueuedStatement(context, data.Expression);
    }

    private static void WriteQueuedItemKeyHandler(
        XFileWriterContext context,
        ZonePointer<ItemKeyHandler>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedItemKeyHandler(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var handler = pointer.Result;
        context.WriteInt32(handler.Key);
        context.WritePointerRaw(handler.Action, PointerResolutionKind.Direct, "ItemKeyHandler.Action");
        context.WritePointerRaw(handler.Next, PointerResolutionKind.Direct, "ItemKeyHandler.Next");
        WriteQueuedMenuEventHandlerSet(context, handler.Action);
        WriteQueuedItemKeyHandler(context, handler.Next);
    }

    private static void WriteItemDef(XFileWriterContext context, ItemDef item)
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
        context.WritePointerRaw(item.Text, PointerResolutionKind.Direct, "ItemDef.Text");
        context.WriteInt32(item.TextSaveGameInfo);
        context.WriteInt32(item.Parent?.Raw ?? 0);
        context.WritePointerRaw(item.MouseEnterText, PointerResolutionKind.Direct, "ItemDef.MouseEnterText");
        context.WritePointerRaw(item.MouseExitText, PointerResolutionKind.Direct, "ItemDef.MouseExitText");
        context.WritePointerRaw(item.MouseEnter, PointerResolutionKind.Direct, "ItemDef.MouseEnter");
        context.WritePointerRaw(item.MouseExit, PointerResolutionKind.Direct, "ItemDef.MouseExit");
        context.WritePointerRaw(item.Action, PointerResolutionKind.Direct, "ItemDef.Action");
        context.WritePointerRaw(item.Accept, PointerResolutionKind.Direct, "ItemDef.Accept");
        context.WritePointerRaw(item.OnFocus, PointerResolutionKind.Direct, "ItemDef.OnFocus");
        context.WritePointerRaw(item.LeaveFocus, PointerResolutionKind.Direct, "ItemDef.LeaveFocus");
        context.WritePointerRaw(item.Dvar, PointerResolutionKind.Direct, "ItemDef.Dvar");
        context.WritePointerRaw(item.DvarTest, PointerResolutionKind.Direct, "ItemDef.DvarTest");
        context.WritePointerRaw(item.OnKey, PointerResolutionKind.Direct, "ItemDef.OnKey");
        context.WritePointerRaw(item.EnableDvar, PointerResolutionKind.Direct, "ItemDef.EnableDvar");
        context.WriteInt32(item.DvarFlags);
        context.WritePointerRaw(item.FocusSound, PointerResolutionKind.Alias, "ItemDef.FocusSound");
        context.WriteFloat(item.Special);
        WriteInt32Array(context, item.CursorPos);
        WriteItemDefDataPointer(context, item.TypeData, item.Type);
        context.WriteInt32(item.ImageTrack);
        context.WriteInt32(item.FloatExpressionCount);
        context.WritePointerRaw(item.FloatExpressions, PointerResolutionKind.Direct, "ItemDef.FloatExpressions");
        context.WritePointerRaw(item.VisibleExp, PointerResolutionKind.Direct, "ItemDef.VisibleExp");
        context.WritePointerRaw(item.DisabledExp, PointerResolutionKind.Direct, "ItemDef.DisabledExp");
        context.WritePointerRaw(item.TextExp, PointerResolutionKind.Direct, "ItemDef.TextExp");
        context.WritePointerRaw(item.MaterialExp, PointerResolutionKind.Direct, "ItemDef.MaterialExp");
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

        WriteQueuedItemDefPointers(context, item);
    }

    private static void WriteQueuedItemDefPointers(XFileWriterContext context, ItemDef item)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedItemDefPointers(context, item)))
            return;

        WriteQueuedString(context, item.Window.NamePtr);
        WriteQueuedString(context, item.Window.GroupPtr);
        WriteQueuedMaterial(context, item.Window.Background);
        WriteQueuedString(context, item.Text);
        WriteQueuedMenuEventHandlerSet(context, item.MouseEnterText);
        WriteQueuedMenuEventHandlerSet(context, item.MouseExitText);
        WriteQueuedMenuEventHandlerSet(context, item.MouseEnter);
        WriteQueuedMenuEventHandlerSet(context, item.MouseExit);
        WriteQueuedMenuEventHandlerSet(context, item.Action);
        WriteQueuedMenuEventHandlerSet(context, item.Accept);
        WriteQueuedMenuEventHandlerSet(context, item.OnFocus);
        WriteQueuedMenuEventHandlerSet(context, item.LeaveFocus);
        WriteQueuedString(context, item.Dvar);
        WriteQueuedString(context, item.DvarTest);
        WriteQueuedItemKeyHandler(context, item.OnKey);
        WriteQueuedString(context, item.EnableDvar);
        WriteQueuedItemDefData(context, item.TypeData, item.Type);
        WriteQueuedItemFloatExpressions(context, item.FloatExpressions);
        WriteQueuedStatement(context, item.VisibleExp);
        WriteQueuedStatement(context, item.DisabledExp);
        WriteQueuedStatement(context, item.TextExp);
        WriteQueuedStatement(context, item.MaterialExp);
    }

    private static void WriteItemDefDataPointer(
        XFileWriterContext context,
        ItemDefData? data,
        int itemType)
    {
        if (data is null)
        {
            context.WriteNullPointer();
            return;
        }

        switch (itemType)
        {
            case 0:
            case 4:
            case 9:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                context.WritePointerRaw(data.EditField, PointerResolutionKind.Direct, "ItemDef.TypeData.EditField");
                break;
            case 6:
                context.WritePointerRaw(data.ListBox, PointerResolutionKind.Direct, "ItemDef.TypeData.ListBox");
                break;
            case 10:
            case 12:
                context.WritePointerRaw(data.Multi, PointerResolutionKind.Direct, "ItemDef.TypeData.Multi");
                break;
            case 13:
                context.WritePointerRaw(data.EnumDvarName, PointerResolutionKind.Direct, "ItemDef.TypeData.EnumDvarName");
                break;
            case 20:
                context.WritePointerRaw(data.NewsTicker, PointerResolutionKind.Direct, "ItemDef.TypeData.NewsTicker");
                break;
            case 21:
                context.WritePointerRaw(data.TextScroll, PointerResolutionKind.Direct, "ItemDef.TypeData.TextScroll");
                break;
            default:
                context.WriteInt32(data.Raw);
                break;
        }
    }

    private static void WriteQueuedItemDefData(
        XFileWriterContext context,
        ItemDefData? data,
        int itemType)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedItemDefData(context, data, itemType)))
            return;

        if (data is null)
            return;

        switch (itemType)
        {
            case 0:
            case 4:
            case 9:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                WriteQueuedEditFieldDef(context, data.EditField);
                break;
            case 6:
                WriteQueuedListBoxDef(context, data.ListBox);
                break;
            case 10:
            case 12:
                WriteQueuedMultiDef(context, data.Multi);
                break;
            case 13:
                WriteQueuedString(context, data.EnumDvarName);
                break;
            case 20:
                WriteQueuedNewsTickerDef(context, data.NewsTicker);
                break;
            case 21:
                WriteQueuedTextScrollDef(context, data.TextScroll);
                break;
        }
    }

    private static void WriteQueuedEditFieldDef(
        XFileWriterContext context,
        ZonePointer<EditFieldDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedEditFieldDef(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var value = pointer.Result;
        context.WriteFloat(value.MinVal);
        context.WriteFloat(value.MaxVal);
        context.WriteFloat(value.DefVal);
        context.WriteFloat(value.Range);
        context.WriteInt32(value.MaxChars);
        context.WriteInt32(value.MaxCharsGotoNext);
        context.WriteInt32(value.MaxPaintChars);
        context.WriteInt32(value.PaintOffset);
    }

    private static void WriteQueuedListBoxDef(
        XFileWriterContext context,
        ZonePointer<ListBoxDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedListBoxDef(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var value = pointer.Result;
#if !PC
        WriteInt32Array(context, value.StartPos);
        WriteInt32Array(context, value.EndPos);
        context.WriteInt32(value.DrawPadding);
#else
        WriteInt32Array(context, value.Unknown);
#endif
        context.WriteFloat(value.ElementWidth);
        context.WriteFloat(value.ElementHeight);
        context.WriteInt32(value.ElementStyle);
        context.WriteInt32(value.NumColumns);
        foreach (var column in value.ColumnInfo)
            WriteColumnInfo(context, column);
        context.WritePointerRaw(value.DoubleClick, PointerResolutionKind.Direct, "ListBoxDef.DoubleClick");
        context.WriteInt32(value.NotSelectable);
        context.WriteInt32(value.NoScrollbars);
        context.WriteInt32(value.UsePaging);
        context.WriteVec4(value.SelectBorder);
        context.WritePointerRaw(value.SelectIcon, PointerResolutionKind.Alias, "ListBoxDef.SelectIcon");

        WriteQueuedMenuEventHandlerSet(context, value.DoubleClick);
    }

    private static void WriteColumnInfo(XFileWriterContext context, ColumnInfo value)
    {
        context.WriteInt32(value.Pos);
        context.WriteInt32(value.Width);
        context.WriteInt32(value.MaxChars);
        context.WriteInt32(value.Alignment);
    }

    private static void WriteQueuedMultiDef(
        XFileWriterContext context,
        ZonePointer<MultiDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMultiDef(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var value = pointer.Result;
        foreach (var dvar in value.DvarList)
            context.WritePointerRaw(dvar, PointerResolutionKind.Direct, "MultiDef.DvarList");
        foreach (var dvar in value.DvarStr)
            context.WritePointerRaw(dvar, PointerResolutionKind.Direct, "MultiDef.DvarStr");
        foreach (var dvarValue in value.DvarValue)
            context.WriteFloat(dvarValue);
        context.WriteInt32(value.Count);
        context.WriteInt32(value.StrDef);

        foreach (var dvar in value.DvarList)
            WriteQueuedString(context, dvar);
        foreach (var dvar in value.DvarStr)
            WriteQueuedString(context, dvar);
    }

    private static void WriteQueuedNewsTickerDef(
        XFileWriterContext context,
        ZonePointer<NewsTickerDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedNewsTickerDef(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var value = pointer.Result;
        context.WriteInt32(value.FeedId);
        context.WriteInt32(value.Speed);
        context.WriteInt32(value.Spacing);
        context.WriteInt32(value.LastTime);
        context.WriteInt32(value.Start);
        context.WriteInt32(value.End);
        context.WriteFloat(value.X);
    }

    private static void WriteQueuedTextScrollDef(
        XFileWriterContext context,
        ZonePointer<TextScrollDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedTextScrollDef(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(pointer);
            context.WriteInt32(pointer.Result.StartTime);
        }
    }

    private static void WriteQueuedItemFloatExpressions(
        XFileWriterContext context,
        ZonePointer<ItemFloatExpression[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedItemFloatExpressions(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var expression in pointer.Result)
            WriteItemFloatExpression(context, expression);

        foreach (var expression in pointer.Result)
            WriteQueuedStatement(context, expression.Expression);
    }

    private static void WriteItemFloatExpression(
        XFileWriterContext context,
        ItemFloatExpression expression)
    {
        context.WriteInt32((int)expression.Target);
        context.WritePointerRaw(expression.Expression);
    }

    private static void WriteQueuedMaterial(
        XFileWriterContext context,
        ZonePointer<Material>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMaterial(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            WriteInlineAssetReferenceBody(context, pointer, WriteMaterial);
        }
    }

    private static void WriteMaterial(XFileWriterContext context, Material material)
    {
        WriteMaterialInfo(context, material.Info);
        context.WriteBytes(material.StateBitsEntry);
        context.WriteByte(material.TextureCount);
        context.WriteByte(material.ConstantCount);
        context.WriteByte(material.StateBitsCount);
        context.WriteByte(material.StateFlags);
        context.WriteByte(material.CameraRegion);
#if PS3
        context.WriteByte(0);
        foreach (var value in material.Ushorts)
            context.WriteUInt16(value);
        context.WritePointerRaw(material.UshortArray, PointerResolutionKind.Direct, "Material.UshortArray");
#endif
        context.WritePointerRaw(material.TechniqueSet, PointerResolutionKind.Alias, "Material.TechniqueSet");
        context.WritePointerRaw(material.TextureTable, PointerResolutionKind.Direct, "Material.TextureTable");
        context.WritePointerRaw(material.ConstantTable, PointerResolutionKind.Direct, "Material.ConstantTable");
        context.WritePointerRaw(material.StateBitTable, PointerResolutionKind.Direct, "Material.StateBitTable");
        context.WritePointerRaw(material.UnknownXStringArray, PointerResolutionKind.Direct, "Material.UnknownXStringArray");

        WriteQueuedString(context, material.Info.NamePtr);
#if PS3
        WriteQueuedUShortArray(context, material.UshortArray);
#endif
        WriteQueuedTechset(context, material.TechniqueSet);
        WriteQueuedMaterialTextureTable(context, material.TextureTable);
        WriteQueuedMaterialConstantTable(context, material.ConstantTable);
        WriteQueuedStateBitTable(context, material.StateBitTable);
    }

    private static void WriteMaterialInfo(XFileWriterContext context, MaterialInfo info)
    {
        context.WritePointerRaw(info.NamePtr, PointerResolutionKind.Direct, "Material.Info.Name");
        context.WriteByte(info.GameFlags);
        context.WriteByte(info.SortKey);
        context.WriteByte(info.TextureAtlasRowCount);
        context.WriteByte(info.TextureAtlasColumnCount);
        context.WriteUInt64(info.DrawSurf.Packed);
        context.WriteInt32(info.SurfaceTypeBits);
#if PS3
        context.WriteInt32(info.Padding);
#endif
    }

    private static void WriteQueuedUShortArray(
        XFileWriterContext context,
        ZonePointer<ushort[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedUShortArray(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            context.WriteUInt16(value);
    }

    private static void WriteQueuedTechset(
        XFileWriterContext context,
        ZonePointer<MaterialTechniqueSet>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedTechset(context, pointer)))
            return;

        if (pointer is { IsInlineData: true, Result: not null })
        {
            WriteInlineAssetReferenceBody(context, pointer, WriteTechset);
        }
    }

    private static void WriteQueuedMaterialTextureTable(
        XFileWriterContext context,
        ZonePointer<MaterialTextureDef[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMaterialTextureTable(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var texture in pointer.Result)
            WriteMaterialTextureDef(context, texture);

        foreach (var texture in pointer.Result)
            WriteQueuedMaterialTextureDefPointers(context, texture);
    }

    private static void WriteMaterialTextureDef(
        XFileWriterContext context,
        MaterialTextureDef texture)
    {
        context.WriteUInt32(texture.NameHash);
        context.WriteByte(texture.NameStart);
        context.WriteByte(texture.NameEnd);
        context.WriteByte(texture.SampleState);
        context.WriteByte((byte)texture.Semantic);
        context.WriteByte(texture.IsMatureContent);
        context.WriteBytes(texture.Pad);

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
            context.WritePointerRaw(texture.Info.Water, PointerResolutionKind.Direct, "MaterialTextureDef.Water");
        else
            context.WritePointerRaw(texture.Info.Image, PointerResolutionKind.Alias, "MaterialTextureDef.Image");
    }

    private static void WriteQueuedMaterialTextureDefPointers(
        XFileWriterContext context,
        MaterialTextureDef texture)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMaterialTextureDefPointers(context, texture)))
            return;

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
            WriteQueuedWater(context, texture.Info.Water);
        else
            WriteQueuedImage(context, texture.Info.Image);
    }

    private static void WriteQueuedMaterialConstantTable(
        XFileWriterContext context,
        ZonePointer<MaterialConstantDef[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedMaterialConstantTable(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var constant in pointer.Result)
            WriteMaterialConstantDef(context, constant);
    }

    private static void WriteMaterialConstantDef(
        XFileWriterContext context,
        MaterialConstantDef constant)
    {
        context.WriteInt32(constant.NameHash);
        var nameBytes = Encoding.Latin1.GetBytes(constant.Name ?? string.Empty);
        Span<byte> fixedName = stackalloc byte[12];
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, fixedName.Length)).CopyTo(fixedName);
        context.WriteBytes(fixedName);
        context.WriteVec4(constant.Literal);
    }

    private static void WriteQueuedStateBitTable(
        XFileWriterContext context,
        ZonePointer<GfxStateBits[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStateBitTable(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var stateBits in pointer.Result)
            WriteGfxStateBits(context, stateBits);

#if PS3
        foreach (var stateBits in pointer.Result)
            WriteQueuedInt32Array(context, stateBits.LoadBits);
#endif
    }

    private static void WriteGfxStateBits(XFileWriterContext context, GfxStateBits stateBits)
    {
#if XBOX
        foreach (var value in stateBits.LoadBits)
            context.WriteInt32(value);
#elif PS3
        context.WritePointerRaw(stateBits.LoadBits, PointerResolutionKind.Direct, "GfxStateBits.LoadBits");
        context.WriteInt32(stateBits.Unknown);
#endif
    }

    private static void WriteQueuedInt32Array(
        XFileWriterContext context,
        ZonePointer<int[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedInt32Array(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            context.WriteInt32(value);
    }

    private static void WriteQueuedWater(
        XFileWriterContext context,
        ZonePointer<Water>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedWater(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var water = pointer.Result;
        context.WriteFloat(water.Writable.FloatTime);
        context.WritePointerRaw(water.H0X, PointerResolutionKind.Direct, "Water.H0X");
        context.WritePointerRaw(water.H0Y, PointerResolutionKind.Direct, "Water.H0Y");
        context.WritePointerRaw(water.WTerm, PointerResolutionKind.Direct, "Water.WTerm");
        context.WriteInt32(water.M);
        context.WriteInt32(water.N);
        context.WriteFloat(water.Lx);
        context.WriteFloat(water.Lz);
        context.WriteFloat(water.Gravity);
        context.WriteFloat(water.Windvel);
        foreach (var value in water.Winddir)
            context.WriteFloat(value);
        context.WriteFloat(water.Amplitude);
        foreach (var value in water.CodeConstant)
            context.WriteFloat(value);
        context.WritePointerRaw(water.Image);

        WriteQueuedFloatArray(context, water.H0X);
        WriteQueuedFloatArray(context, water.H0Y);
        WriteQueuedFloatArray(context, water.WTerm);
        WriteQueuedImage(context, water.Image);
    }

    private static void WriteQueuedFloatArray(
        XFileWriterContext context,
        ZonePointer<float[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedFloatArray(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            context.WriteFloat(value);
    }

    private static void WriteQueuedImage(
        XFileWriterContext context,
        ZonePointer<GfxImage>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedImage(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        WriteInlineAssetReferenceBody(context, pointer, WriteImage);
    }

    private static void WriteImage(XFileWriterContext context, GfxImage image)
    {
        context.WritePointerRaw(image.LoadDef);
        context.WriteByte(image.MapType);
        context.WriteByte(image.Semantic);
        context.WriteByte(image.Category);
        context.WriteByte(image.UseSrgbReads);
        context.WriteBytes(image.Picmip);
        context.WriteByte(image.NoPicmip);
        context.WriteByte(image.Track);
        WriteInt32Array(context, image.CardMemory);
        context.WritePointerRaw(image.NamePtr);
        context.WriteUInt16(image.Width);
        context.WriteUInt16(image.Height);
        context.WriteUInt16(image.Depth);
        context.WriteByte(image.DelayLoadPixels);
        context.WriteBytes(image.Pad);

        WriteQueuedImageLoadDef(context, image.LoadDef);
        WriteQueuedString(context, image.NamePtr);
    }

    private static void WriteQueuedImageLoadDef(
        XFileWriterContext context,
        ZonePointer<GfxImageLoadDef>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedImageLoadDef(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        var loadDef = pointer.Result;
        context.WriteByte(loadDef.LevelCount);
        context.WriteBytes(loadDef.Pad);
        context.WriteInt32(loadDef.Flags);
        context.WriteInt32(loadDef.Format);
        context.WriteInt32(loadDef.ResourceSize);
        context.WriteBytes(loadDef.Data);
    }

    private static void WriteStringTable(XFileWriterContext context, StringTable table)
    {
        context.WritePointerRaw(table.NamePtr);
        context.WriteInt32(table.ColumnCount);
        context.WriteInt32(table.RowCount);
        context.WritePointerRaw(table.StringsPtr);

        WriteQueuedString(context, table.NamePtr);
        WriteQueuedStringTableCells(context, table.StringsPtr);
    }

    private static void WriteQueuedStringTableCells(
        XFileWriterContext context,
        ZonePointer<StringTableCell[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStringTableCells(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.AlignStreamOnly(XFileWriteRules.StructAlignment);
        context.RegisterMaterializedPointerValue(pointer);

        foreach (var cell in pointer.Result)
            WriteStringTableCell(context, cell);

        foreach (var cell in pointer.Result)
            WriteQueuedString(context, cell.StringPtr);
    }

    private static void WriteStringTableCell(XFileWriterContext context, StringTableCell cell)
    {
        context.WritePointerRaw(cell.StringPtr);
        context.WriteInt32(cell.Hash);
    }

    private static void WriteStructuredDataDefSet(
        XFileWriterContext context,
        StructuredDataDefSet set)
    {
        context.WritePointerRaw(set.NamePtr);
        context.WriteInt32(set.DefCount);
        context.WritePointerRaw(set.DefsPtr);

        WriteQueuedString(context, set.NamePtr);
        WriteQueuedStructuredDataDefs(context, set.DefsPtr);
    }

    private static void WriteQueuedStructuredDataDefs(
        XFileWriterContext context,
        ZonePointer<StructuredDataDef[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataDefs(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataDef(context, value);

        foreach (var value in pointer.Result)
            WriteQueuedStructuredDataDefPointers(context, value);
    }

    private static void WriteStructuredDataDef(
        XFileWriterContext context,
        StructuredDataDef value)
    {
        context.WriteInt32(value.Version);
        context.WriteUInt32(value.FormatChecksum);
        context.WriteInt32(value.EnumCount);
        context.WritePointerRaw(value.EnumsPtr);
        context.WriteInt32(value.StructCount);
        context.WritePointerRaw(value.StructsPtr);
        context.WriteInt32(value.IndexedArrayCount);
        context.WritePointerRaw(value.IndexedArraysPtr);
        context.WriteInt32(value.EnumedArrayCount);
        context.WritePointerRaw(value.EnumedArraysPtr);
        WriteStructuredDataType(context, value.RootType);
        context.WriteUInt32(value.Size);
    }

    private static void WriteQueuedStructuredDataDefPointers(
        XFileWriterContext context,
        StructuredDataDef value)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataDefPointers(context, value)))
            return;

        WriteQueuedStructuredDataEnums(context, value.EnumsPtr);
        WriteQueuedStructuredDataStructs(context, value.StructsPtr);
        WriteQueuedStructuredDataIndexedArrays(context, value.IndexedArraysPtr);
        WriteQueuedStructuredDataEnumedArrays(context, value.EnumedArraysPtr);
    }

    private static void WriteQueuedStructuredDataEnums(
        XFileWriterContext context,
        ZonePointer<StructuredDataEnum[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataEnums(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataEnum(context, value);

        foreach (var value in pointer.Result)
            WriteQueuedStructuredDataEnumEntries(context, value.EntriesPtr);
    }

    private static void WriteStructuredDataEnum(
        XFileWriterContext context,
        StructuredDataEnum value)
    {
        context.WriteInt32(value.EntryCount);
        context.WriteInt32(value.ReservedEntryCount);
        context.WritePointerRaw(value.EntriesPtr);
    }

    private static void WriteQueuedStructuredDataEnumEntries(
        XFileWriterContext context,
        ZonePointer<StructuredDataEnumEntry[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataEnumEntries(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataEnumEntry(context, value);

        foreach (var value in pointer.Result)
            WriteQueuedString(context, value.StringPtr);
    }

    private static void WriteStructuredDataEnumEntry(
        XFileWriterContext context,
        StructuredDataEnumEntry value)
    {
        context.WritePointerRaw(value.StringPtr);
        context.WriteUInt16(value.Index);
        context.WriteUInt16(value.Padding);
    }

    private static void WriteQueuedStructuredDataStructs(
        XFileWriterContext context,
        ZonePointer<StructuredDataStruct[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataStructs(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataStruct(context, value);

        foreach (var value in pointer.Result)
            WriteQueuedStructuredDataStructProperties(context, value.PropertiesPtr);
    }

    private static void WriteStructuredDataStruct(
        XFileWriterContext context,
        StructuredDataStruct value)
    {
        context.WriteInt32(value.PropertyCount);
        context.WritePointerRaw(value.PropertiesPtr);
        context.WriteInt32(value.Size);
        context.WriteUInt32(value.BitOffset);
    }

    private static void WriteQueuedStructuredDataStructProperties(
        XFileWriterContext context,
        ZonePointer<StructuredDataStructProperty[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataStructProperties(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataStructProperty(context, value);

        foreach (var value in pointer.Result)
            WriteQueuedString(context, value.NamePtr);
    }

    private static void WriteStructuredDataStructProperty(
        XFileWriterContext context,
        StructuredDataStructProperty value)
    {
        context.WritePointerRaw(value.NamePtr);
        WriteStructuredDataType(context, value.Type);
        context.WriteUInt32(value.Offset);
    }

    private static void WriteQueuedStructuredDataIndexedArrays(
        XFileWriterContext context,
        ZonePointer<StructuredDataIndexedArray[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataIndexedArrays(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataIndexedArray(context, value);
    }

    private static void WriteStructuredDataIndexedArray(
        XFileWriterContext context,
        StructuredDataIndexedArray value)
    {
        context.WriteInt32(value.ArraySize);
        WriteStructuredDataType(context, value.ElementType);
        context.WriteUInt32(value.ElementSize);
    }

    private static void WriteQueuedStructuredDataEnumedArrays(
        XFileWriterContext context,
        ZonePointer<StructuredDataEnumedArray[]>? pointer)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedStructuredDataEnumedArrays(context, pointer)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.RegisterMaterializedPointerValue(pointer);

        foreach (var value in pointer.Result)
            WriteStructuredDataEnumedArray(context, value);
    }

    private static void WriteStructuredDataEnumedArray(
        XFileWriterContext context,
        StructuredDataEnumedArray value)
    {
        context.WriteInt32(value.EnumIndex);
        WriteStructuredDataType(context, value.ElementType);
        context.WriteUInt32(value.ElementSize);
    }

    private static void WriteStructuredDataType(XFileWriterContext context, StructuredDataType value)
    {
        context.WriteInt32((int)value.Type);
        context.WriteInt32(value.UnionValue);
    }

    private static void WriteRawFile(XFileWriterContext context, RawFile rawFile)
    {
        context.WritePointerRaw(rawFile.NamePtr);
        context.WriteInt32(rawFile.CompressedLen);
        context.WriteInt32(rawFile.Len);
        context.WritePointerRaw(rawFile.BufferPtr);

        WriteQueuedString(context, rawFile.NamePtr);
        WriteQueuedRawFileBuffer(context, rawFile);
    }

    private static void WriteQueuedRawFileBuffer(
        XFileWriterContext context,
        RawFile rawFile)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedRawFileBuffer(context, rawFile)))
            return;

        var pointer = rawFile.BufferPtr;
        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            var buffer = GetRawFileSerializedBuffer(rawFile);
            context.RegisterMaterializedPointerValue(pointer, buffer.Length);
            context.WriteBytes(buffer);
        });
    }

    private static byte[] GetRawFileSerializedBuffer(RawFile rawFile)
    {
        var buffer = rawFile.BufferPtr.Result ?? [];
        if (rawFile.CompressedLen > 0)
            return buffer;

        var expectedLength = checked(rawFile.Len + 1);
        if (buffer.Length == expectedLength)
            return buffer;

        var serialized = new byte[expectedLength];
        buffer.AsSpan(0, Math.Min(buffer.Length, rawFile.Len)).CopyTo(serialized);
        return serialized;
    }

    private static void WriteLocalizeEntry(XFileWriterContext context, LocalizeEntry localize)
    {
        context.WritePointerRaw(localize.ValuePtr);
        context.WritePointerRaw(localize.NamePtr);

        WriteQueuedString(context, localize.ValuePtr);
        WriteQueuedString(context, localize.NamePtr);
    }
}
