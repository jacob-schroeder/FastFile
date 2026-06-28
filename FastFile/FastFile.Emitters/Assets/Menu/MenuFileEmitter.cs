using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Codecs;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.Menu;

public sealed class MenuFileEmitter : IXAssetEmitter<MenuFileAsset>
{
    private const int MenuFileSize = 0x0c;

    public IXAssetCodecContract Contract => MenuFileCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, MenuFileAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        string name = Required(asset.Name, "MenuFile.name");
        ValidateCount(asset.MenuCount, asset.Menus.Count, "MenuFile.menus");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress rootAddress = context.Blocks.AllocateCurrent(MenuFileSize);

            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(asset.MenuCount);
            context.Source.WriteInt32(PointerRaw(asset.MenuCount));

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitInlineXString(context, rootAddress.Add(0x00), name);
                if (asset.MenuCount > 0)
                    EmitMenuPointerArray(context, rootAddress.Add(0x08), asset.Menus);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"MenuFile emitted source=0x{sourceOffset:X} name='{name}' menus={asset.MenuCount} blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitMenuPointerArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<MenuDefReference> menus)
    {
        XBlockAddress table = PatchAndAllocateTable(context, cell, menus.Count, sizeof(int));
        foreach (MenuDefReference menu in menus)
            context.Source.WriteInt32(menu.Menu is null ? 0 : -1);

        for (int i = 0; i < menus.Count; i++)
        {
            if (menus[i].Menu is { } menu)
                EmitMenuDefPointer(context, table.Add(checked(i * sizeof(int))), menu);
        }
    }

    private static void EmitMenuDefPointer(XEmitContext context, XBlockAddress cell, MenuDefAsset menu)
    {
        ValidateCount(menu.ItemCount, menu.Items.Count, "MenuDef.items");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
            XBlockAddress root = context.Blocks.AllocateCurrent(MenuDefAsset.SerializedSize);
            EmitMenuDefRoot(context, menu);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitExpressionSupportingDataPointer(context, root.Add(0x2EC), menu.ExpressionDataValue);
                EmitWindowChildren(context, root.Add(0x00), menu.Window);
                EmitOptionalXString(context, root.Add(0xB0), menu.Font);
                EmitEventHandlerSetPointer(context, root.Add(0xD0), menu.OnOpenSet);
                EmitEventHandlerSetPointer(context, root.Add(0xD8), menu.OnCloseSet);
                EmitEventHandlerSetPointer(context, root.Add(0xD4), menu.OnCloseRequestSet);
                EmitEventHandlerSetPointer(context, root.Add(0xDC), menu.OnEscSet);
                EmitItemKeyHandlerPointer(context, root.Add(0xE0), menu.ExecKeyHandler);
                EmitStatementPointer(context, root.Add(0xE4), menu.VisibleStatement);
                EmitOptionalXString(context, root.Add(0xFC), menu.AllowedBindingString);
                EmitOptionalXString(context, root.Add(0x100), menu.SoundNameString);
                EmitStatementPointer(context, root.Add(0x114), menu.RectXStatement);
                EmitStatementPointer(context, root.Add(0x118), menu.RectYStatement);
                EmitStatementPointer(context, root.Add(0x11C), menu.RectWStatement);
                EmitStatementPointer(context, root.Add(0x120), menu.RectHStatement);
                if (menu.ItemCount > 0)
                    EmitItemPointerArray(context, root.Add(0x128), menu.Items);
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitMenuDefRoot(XEmitContext context, MenuDefAsset menu)
    {
        EmitWindowRoot(context, menu.Window);
        context.Source.WriteInt32(PointerRaw(menu.Font));
        context.Source.WriteInt32(menu.Fullscreen);
        context.Source.WriteInt32(menu.ItemCount);
        context.Source.WriteInt32(menu.FontIndex);
        WriteFixedInt32Array(context, menu.CursorItems, 4, "MenuDef.cursorItems");
        context.Source.WriteInt32(menu.FadeCycle);
        context.Source.WriteSingle(menu.FadeClamp);
        context.Source.WriteSingle(menu.FadeAmount);
        context.Source.WriteSingle(menu.FadeInAmount);
        context.Source.WriteSingle(menu.BlurRadius);
        context.Source.WriteInt32(PointerRaw(menu.OnOpenSet));
        context.Source.WriteInt32(PointerRaw(menu.OnCloseRequestSet));
        context.Source.WriteInt32(PointerRaw(menu.OnCloseSet));
        context.Source.WriteInt32(PointerRaw(menu.OnEscSet));
        context.Source.WriteInt32(PointerRaw(menu.ExecKeyHandler));
        context.Source.WriteInt32(PointerRaw(menu.VisibleStatement));
        context.Source.WriteInt32(PointerRaw(menu.AllowedBindingString));
        context.Source.WriteInt32(PointerRaw(menu.SoundNameString));
        context.Source.WriteInt32(menu.ImageTrack);
        WriteVec4(context, menu.FocusColor);
        context.Source.WriteInt32(PointerRaw(menu.RectXStatement));
        context.Source.WriteInt32(PointerRaw(menu.RectYStatement));
        context.Source.WriteInt32(PointerRaw(menu.RectWStatement));
        context.Source.WriteInt32(PointerRaw(menu.RectHStatement));
        context.Source.WriteInt32(PointerRaw(menu.ItemCount));
        WriteTransitions(context, menu.ScaleTransitions, "MenuDef.scaleTransitions");
        WriteTransitions(context, menu.AlphaTransitions, "MenuDef.alphaTransitions");
        WriteTransitions(context, menu.XTransitions, "MenuDef.xTransitions");
        WriteTransitions(context, menu.YTransitions, "MenuDef.yTransitions");
        context.Source.WriteInt32(PointerRaw(menu.ExpressionDataValue));
    }

    private static void EmitItemPointerArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<ItemDefReference> items)
    {
        XBlockAddress table = PatchAndAllocateTable(context, cell, items.Count, sizeof(int));
        foreach (ItemDefReference item in items)
            context.Source.WriteInt32(item.Item is null ? 0 : -1);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Item is { } item)
                EmitItemDefPointer(context, table.Add(checked(i * sizeof(int))), item);
        }
    }

    private static void EmitItemDefPointer(XEmitContext context, XBlockAddress cell, ItemDefAsset item)
    {
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(ItemDefAsset.SerializedSize);
        EmitItemDefRoot(context, item);

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            EmitWindowChildren(context, root.Add(0x00), item.Window);
            EmitOptionalXString(context, root.Add(0x12C), item.TextString);
            EmitEventHandlerSetPointer(context, root.Add(0x138), item.MouseEnterTextSet);
            EmitEventHandlerSetPointer(context, root.Add(0x13C), item.MouseExitTextSet);
            EmitEventHandlerSetPointer(context, root.Add(0x140), item.MouseEnterSet);
            EmitEventHandlerSetPointer(context, root.Add(0x144), item.MouseExitSet);
            EmitEventHandlerSetPointer(context, root.Add(0x148), item.ActionSet);
            EmitEventHandlerSetPointer(context, root.Add(0x14C), item.AcceptSet);
            EmitEventHandlerSetPointer(context, root.Add(0x150), item.OnFocusSet);
            EmitEventHandlerSetPointer(context, root.Add(0x154), item.LeaveFocusSet);
            EmitOptionalXString(context, root.Add(0x158), item.DvarString);
            EmitOptionalXString(context, root.Add(0x15C), item.DvarTestString);
            EmitItemKeyHandlerPointer(context, root.Add(0x160), item.OnKeyHandler);
            EmitOptionalXString(context, root.Add(0x164), item.EnableDvarString);
            RejectExternalPointer(item.FocusSound.Raw, "ItemDef.focusSound");
            EmitItemTypeData(context, root.Add(0x184), item);
            EmitItemFloatExpressions(context, root.Add(0x190), item.LoadedFloatExpressions);
            EmitStatementPointer(context, root.Add(0x194), item.VisibleStatement);
            EmitStatementPointer(context, root.Add(0x198), item.DisabledStatement);
            EmitStatementPointer(context, root.Add(0x19C), item.TextStatement);
            EmitStatementPointer(context, root.Add(0x1A0), item.MaterialStatement);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitItemDefRoot(XEmitContext context, ItemDefAsset item)
    {
        ValidateCount(item.FloatExpressionCount, item.LoadedFloatExpressions.Count, "ItemDef.floatExpressions");

        EmitWindowRoot(context, item.Window);
        WriteFixedRectArray(context, item.TextRect, 4, "ItemDef.textRect");
        context.Source.WriteInt32((int)item.Type);
        context.Source.WriteInt32(item.DataType);
        context.Source.WriteInt32(item.Align);
        context.Source.WriteInt32(item.FontEnum);
        context.Source.WriteInt32(item.TextAlignMode);
        context.Source.WriteSingle(item.TextAlignX);
        context.Source.WriteSingle(item.TextAlignY);
        context.Source.WriteSingle(item.TextScale);
        context.Source.WriteInt32(item.TextStyle);
        context.Source.WriteInt32(item.GameMsgWindowIndex);
        context.Source.WriteInt32(item.GameMsgWindowMode);
        context.Source.WriteInt32(PointerRaw(item.TextString));
        context.Source.WriteInt32(item.TextSaveGameInfo);
        context.Source.WriteInt32(item.RuntimeParentPointer);
        context.Source.WriteInt32(PointerRaw(item.MouseEnterTextSet));
        context.Source.WriteInt32(PointerRaw(item.MouseExitTextSet));
        context.Source.WriteInt32(PointerRaw(item.MouseEnterSet));
        context.Source.WriteInt32(PointerRaw(item.MouseExitSet));
        context.Source.WriteInt32(PointerRaw(item.ActionSet));
        context.Source.WriteInt32(PointerRaw(item.AcceptSet));
        context.Source.WriteInt32(PointerRaw(item.OnFocusSet));
        context.Source.WriteInt32(PointerRaw(item.LeaveFocusSet));
        context.Source.WriteInt32(PointerRaw(item.DvarString));
        context.Source.WriteInt32(PointerRaw(item.DvarTestString));
        context.Source.WriteInt32(PointerRaw(item.OnKeyHandler));
        context.Source.WriteInt32(PointerRaw(item.EnableDvarString));
        context.Source.WriteInt32(item.DvarFlags);
        WriteExternalNullPointer(context, item.FocusSound.Raw, "ItemDef.focusSound");
        context.Source.WriteSingle(item.Special);
        WriteFixedInt32Array(context, item.CursorPos, 4, "ItemDef.cursorPos");
        context.Source.WriteInt32(ItemTypeDataRaw(item));
        context.Source.WriteInt32(item.ImageTrack);
        context.Source.WriteInt32(item.FloatExpressionCount);
        context.Source.WriteInt32(PointerRaw(item.FloatExpressionCount));
        context.Source.WriteInt32(PointerRaw(item.VisibleStatement));
        context.Source.WriteInt32(PointerRaw(item.DisabledStatement));
        context.Source.WriteInt32(PointerRaw(item.TextStatement));
        context.Source.WriteInt32(PointerRaw(item.MaterialStatement));
        WriteVec4(context, item.GlowColor);
        context.Source.WriteByte(item.DecayActive);
        context.Source.WriteByte(item.DecayActivePad0);
        context.Source.WriteByte(item.DecayActivePad1);
        context.Source.WriteByte(item.DecayActivePad2);
        context.Source.WriteInt32(item.FxBirthTime);
        context.Source.WriteInt32(item.FxLetterTime);
        context.Source.WriteInt32(item.FxDecayStartTime);
        context.Source.WriteInt32(item.FxDecayDuration);
        context.Source.WriteInt32(item.LastSoundPlayedTime);
    }

    private static void EmitWindowRoot(XEmitContext context, WindowDef window)
    {
        context.Source.WriteInt32(PointerRaw(window.Name));
        EmitRectangle(context, window.Rect);
        EmitRectangle(context, window.RectClient);
        context.Source.WriteInt32(PointerRaw(window.Group));
        context.Source.WriteInt32((int)window.Style);
        context.Source.WriteInt32((int)window.Border);
        context.Source.WriteInt32((int)window.OwnerDraw);
        context.Source.WriteInt32(window.OwnerDrawFlags);
        context.Source.WriteSingle(window.BorderSize);
        context.Source.WriteInt32((int)window.StaticFlags);
        WriteFixedEnumArray(context, window.DynamicFlags, 4, "WindowDef.dynamicFlags");
        context.Source.WriteInt32(window.NextTime);
        WriteVec4(context, window.ForeColor);
        WriteVec4(context, window.BackColor);
        WriteVec4(context, window.BorderColor);
        WriteVec4(context, window.OutlineColor);
        WriteVec4(context, window.DisableColor);
        WriteExternalNullPointer(context, window.Background.Raw, "WindowDef.background");
    }

    private static void EmitWindowChildren(XEmitContext context, XBlockAddress windowAddress, WindowDef window)
    {
        EmitOptionalXString(context, windowAddress.Add(0x00), window.Name);
        EmitOptionalXString(context, windowAddress.Add(0x2C), window.Group);
        RejectExternalPointer(window.Background.Raw, "WindowDef.background");
    }

    private static void EmitEventHandlerSetPointer(XEmitContext context, XBlockAddress cell, MenuEventHandlerSet? set)
    {
        if (set is null)
            return;

        ValidateCount(set.EventHandlerCount, set.Handlers.Count, "MenuEventHandlerSet.handlers");
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(MenuEventHandlerSet.SerializedSize);
        context.Source.WriteInt32(set.EventHandlerCount);
        context.Source.WriteInt32(PointerRaw(set.EventHandlerCount));

        if (set.EventHandlerCount > 0)
            EmitEventHandlerPointerArray(context, root.Add(0x04), set.Handlers);
    }

    private static void EmitEventHandlerPointerArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<MenuEventHandlerReference> handlers)
    {
        XBlockAddress table = PatchAndAllocateTable(context, cell, handlers.Count, sizeof(int));
        foreach (MenuEventHandlerReference handler in handlers)
            context.Source.WriteInt32(handler.Handler is null ? 0 : -1);

        for (int i = 0; i < handlers.Count; i++)
        {
            if (handlers[i].Handler is { } handler)
                EmitEventHandlerPointer(context, table.Add(checked(i * sizeof(int))), handler);
        }
    }

    private static void EmitEventHandlerPointer(XEmitContext context, XBlockAddress cell, MenuEventHandler handler)
    {
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(MenuEventHandler.SerializedSize);
        context.Source.WriteInt32(EventDataRaw(handler));
        context.Source.WriteByte((byte)handler.EventType);
        context.Source.WriteByte(handler.Pad05);
        context.Source.WriteByte(handler.Pad06);
        context.Source.WriteByte(handler.Pad07);
        EmitEventData(context, root.Add(0x00), handler);
    }

    private static void EmitEventData(XEmitContext context, XBlockAddress cell, MenuEventHandler handler)
    {
        switch (handler.EventType)
        {
            case MenuEventHandlerType.UnconditionalScript:
                EmitOptionalXString(context, cell, handler.UnconditionalScript);
                break;
            case MenuEventHandlerType.ConditionalScript:
                EmitConditionalScriptPointer(context, cell, handler.ConditionalScript);
                break;
            case MenuEventHandlerType.ElseScript:
                EmitEventHandlerSetPointer(context, cell, handler.ElseScriptSet);
                break;
            case MenuEventHandlerType.SetLocalVarBool:
            case MenuEventHandlerType.SetLocalVarInt:
            case MenuEventHandlerType.SetLocalVarFloat:
            case MenuEventHandlerType.SetLocalVarString:
                EmitSetLocalVarDataPointer(context, cell, handler.SetLocalVarData);
                break;
        }
    }

    private static void EmitConditionalScriptPointer(XEmitContext context, XBlockAddress cell, ConditionalScript? script)
    {
        if (script is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(ConditionalScript.SerializedSize);
        context.Source.WriteInt32(PointerRaw(script.EventHandlers));
        context.Source.WriteInt32(PointerRaw(script.EventStatement));
        EmitStatementPointer(context, root.Add(0x04), script.EventStatement);
        EmitEventHandlerSetPointer(context, root.Add(0x00), script.EventHandlers);
    }

    private static void EmitSetLocalVarDataPointer(XEmitContext context, XBlockAddress cell, SetLocalVarData? data)
    {
        if (data is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(SetLocalVarData.SerializedSize);
        context.Source.WriteInt32(PointerRaw(data.LocalVarNameString));
        context.Source.WriteInt32(PointerRaw(data.ExpressionStatement));
        EmitOptionalXString(context, root.Add(0x00), data.LocalVarNameString);
        EmitStatementPointer(context, root.Add(0x04), data.ExpressionStatement);
    }

    private static void EmitItemKeyHandlerPointer(XEmitContext context, XBlockAddress cell, ItemKeyHandler? handler)
    {
        if (handler is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(ItemKeyHandler.SerializedSize);
        context.Source.WriteInt32(handler.Key);
        context.Source.WriteInt32(PointerRaw(handler.ActionSet));
        context.Source.WriteInt32(PointerRaw(handler.NextHandler));
        EmitEventHandlerSetPointer(context, root.Add(0x04), handler.ActionSet);
        EmitItemKeyHandlerPointer(context, root.Add(0x08), handler.NextHandler);
    }

    private static void EmitStatementPointer(XEmitContext context, XBlockAddress cell, Statement? statement)
    {
        if (statement is null)
            return;

        ValidateCount(statement.NumEntries, statement.LoadedEntries.Count, "Statement.entries");
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(Statement.SerializedSize);
        context.Source.WriteInt32(statement.NumEntries);
        context.Source.WriteInt32(PointerRaw(statement.NumEntries));
        context.Source.WriteInt32(PointerRaw(statement.SupportingDataValue));
        context.Source.WriteInt32(statement.LastExecuteTime);
        EmitOperand(context, statement.LastResult, null, null);

        if (statement.NumEntries > 0)
            EmitExpressionEntries(context, root.Add(0x04), statement.LoadedEntries);
        EmitExpressionSupportingDataPointer(context, root.Add(0x08), statement.SupportingDataValue);
    }

    private static void EmitExpressionEntries(XEmitContext context, XBlockAddress cell, IReadOnlyList<ExpressionEntry> entries)
    {
        XBlockAddress table = PatchAndAllocateTable(context, cell, entries.Count, ExpressionEntry.SerializedSize);
        for (int i = 0; i < entries.Count; i++)
        {
            context.Source.WriteInt32((int)entries[i].Kind);
            EmitOperand(context, entries[i].Operand, entries[i].StringValue, entries[i].FunctionStatement);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Kind != ExpressionEntryKind.Operand)
                continue;

            XBlockAddress operandCell = table.Add(checked(i * ExpressionEntry.SerializedSize + 0x08));
            if (entries[i].Operand.DataType == ExpDataType.VAL_STRING)
                EmitOptionalXString(context, operandCell, entries[i].StringValue);
            else if (entries[i].Operand.DataType == ExpDataType.VAL_FUNCTION)
                EmitStatementPointer(context, operandCell, entries[i].FunctionStatement);
        }
    }

    private static void EmitOperand(XEmitContext context, Operand operand, string? stringValue, Statement? function)
    {
        context.Source.WriteInt32((int)operand.DataType);
        int raw = operand.DataType switch
        {
            ExpDataType.VAL_STRING => PointerRaw(stringValue),
            ExpDataType.VAL_FUNCTION => PointerRaw(function),
            _ => operand.Internals.Raw
        };
        context.Source.WriteInt32(raw);
    }

    private static void EmitExpressionSupportingDataPointer(XEmitContext context, XBlockAddress cell, ExpressionSupportingData? data)
    {
        if (data is null)
            return;

        ValidateCount(data.UiFunctions.TotalFunctions, data.UiFunctions.LoadedFunctions.Count, "UIFunctionList.functions");
        ValidateCount(data.StaticDvarList.NumStaticDvars, data.StaticDvarList.LoadedStaticDvars.Count, "StaticDvarList.staticDvars");
        ValidateCount(data.UiStrings.TotalStrings, data.UiStrings.LoadedStrings.Count, "StringList.strings");
        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(ExpressionSupportingData.SerializedSize);

        context.Source.WriteInt32(data.UiFunctions.TotalFunctions);
        context.Source.WriteInt32(PointerRaw(data.UiFunctions.TotalFunctions));
        context.Source.WriteInt32(data.StaticDvarList.NumStaticDvars);
        context.Source.WriteInt32(PointerRaw(data.StaticDvarList.NumStaticDvars));
        context.Source.WriteInt32(data.UiStrings.TotalStrings);
        context.Source.WriteInt32(PointerRaw(data.UiStrings.TotalStrings));

        EmitStatementReferenceArray(context, root.Add(0x04), data.UiFunctions.LoadedFunctions);
        EmitStaticDvarReferenceArray(context, root.Add(0x0C), data.StaticDvarList.LoadedStaticDvars);
        EmitXStringReferenceArray(context, root.Add(0x14), data.UiStrings.LoadedStrings);
    }

    private static void EmitStatementReferenceArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<StatementReference> refs)
    {
        if (refs.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, refs.Count, sizeof(int));
        foreach (StatementReference value in refs)
            context.Source.WriteInt32(value.Statement is null ? 0 : -1);

        for (int i = 0; i < refs.Count; i++)
            EmitStatementPointer(context, table.Add(checked(i * sizeof(int))), refs[i].Statement);
    }

    private static void EmitStaticDvarReferenceArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<StaticDvarReference> refs)
    {
        if (refs.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, refs.Count, sizeof(int));
        foreach (StaticDvarReference value in refs)
            context.Source.WriteInt32(value.StaticDvar is null ? 0 : -1);

        for (int i = 0; i < refs.Count; i++)
            EmitStaticDvarPointer(context, table.Add(checked(i * sizeof(int))), refs[i].StaticDvar);
    }

    private static void EmitXStringReferenceArray(XEmitContext context, XBlockAddress cell, IReadOnlyList<XStringReference> refs)
    {
        if (refs.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, refs.Count, sizeof(int));
        foreach (XStringReference value in refs)
            context.Source.WriteInt32(PointerRaw(value.Value));

        for (int i = 0; i < refs.Count; i++)
            EmitOptionalXString(context, table.Add(checked(i * sizeof(int))), refs[i].Value);
    }

    private static void EmitStaticDvarPointer(XEmitContext context, XBlockAddress cell, StaticDvar? dvar)
    {
        if (dvar is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(StaticDvar.SerializedSize);
        WriteExternalNullPointer(context, dvar.Dvar.Raw, "StaticDvar.dvar runtime handle");
        context.Source.WriteInt32(PointerRaw(dvar.DvarNameString));
        EmitOptionalXString(context, root.Add(0x04), dvar.DvarNameString);
    }

    private static void EmitItemTypeData(XEmitContext context, XBlockAddress cell, ItemDefAsset item)
    {
        switch (item.Type)
        {
            case ItemDefType.Text:
            case ItemDefType.EditField:
            case ItemDefType.NumericField:
            case ItemDefType.Slider:
            case ItemDefType.YesNo:
            case ItemDefType.Bind:
            case ItemDefType.Validation:
            case ItemDefType.DecimalField:
            case ItemDefType.UpDown:
            case ItemDefType.EmailField:
            case ItemDefType.PassWordField:
                EmitEditFieldPointer(context, cell, item.EditField);
                break;
            case ItemDefType.ListBox:
                EmitListBoxPointer(context, cell, item.ListBox);
                break;
            case ItemDefType.Multi:
                EmitMultiPointer(context, cell, item.Multi);
                break;
            case ItemDefType.DvarEnum:
                EmitOptionalXString(context, cell, item.DvarEnumName);
                break;
            case ItemDefType.NewsTicker:
                EmitNewsTickerPointer(context, cell, item.NewsTicker);
                break;
            case ItemDefType.TextScroll:
                EmitTextScrollPointer(context, cell, item.TextScroll);
                break;
        }
    }

    private static int ItemTypeDataRaw(ItemDefAsset item)
    {
        return item.Type switch
        {
            ItemDefType.Text or ItemDefType.EditField or ItemDefType.NumericField or ItemDefType.Slider or ItemDefType.YesNo or
                ItemDefType.Bind or ItemDefType.Validation or ItemDefType.DecimalField or ItemDefType.UpDown or
                ItemDefType.EmailField or ItemDefType.PassWordField => PointerRaw(item.EditField),
            ItemDefType.ListBox => PointerRaw(item.ListBox),
            ItemDefType.Multi => PointerRaw(item.Multi),
            ItemDefType.DvarEnum => PointerRaw(item.DvarEnumName),
            ItemDefType.NewsTicker => PointerRaw(item.NewsTicker),
            ItemDefType.TextScroll => PointerRaw(item.TextScroll),
            _ => 0
        };
    }

    private static void EmitEditFieldPointer(XEmitContext context, XBlockAddress cell, EditFieldDef? edit)
    {
        if (edit is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        context.Blocks.AllocateCurrent(EditFieldDef.SerializedSize);
        context.Source.WriteSingle(edit.MinVal);
        context.Source.WriteSingle(edit.MaxVal);
        context.Source.WriteSingle(edit.DefVal);
        context.Source.WriteSingle(edit.Range);
        context.Source.WriteInt32(edit.MaxChars);
        context.Source.WriteInt32(edit.MaxCharsGotoNext);
        context.Source.WriteInt32(edit.MaxPaintChars);
        context.Source.WriteInt32(edit.PaintOffset);
    }

    private static void EmitListBoxPointer(XEmitContext context, XBlockAddress cell, ListBoxDef? list)
    {
        if (list is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(ListBoxDef.SerializedSize);
        WriteFixedInt32Array(context, list.StartPos, 4, "ListBoxDef.startPos");
        WriteFixedInt32Array(context, list.EndPos, 4, "ListBoxDef.endPos");
        context.Source.WriteInt32(list.DrawPadding);
        context.Source.WriteSingle(list.ElementWidth);
        context.Source.WriteSingle(list.ElementHeight);
        context.Source.WriteInt32(list.ElementStyle);
        context.Source.WriteInt32(list.NumColumns);
        WriteColumnInfoArray(context, list.ColumnInfo, 16);
        context.Source.WriteInt32(PointerRaw(list.DoubleClickSet));
        context.Source.WriteInt32(list.NotSelectable);
        context.Source.WriteInt32(list.NoScrollbars);
        context.Source.WriteInt32(list.UsePaging);
        WriteVec4(context, list.SelectBorder);
        WriteExternalNullPointer(context, list.SelectIcon.Raw, "ListBoxDef.selectIcon");
        EmitEventHandlerSetPointer(context, root.Add(0x12C), list.DoubleClickSet);
        RejectExternalPointer(list.SelectIcon.Raw, "ListBoxDef.selectIcon");
    }

    private static void EmitMultiPointer(XEmitContext context, XBlockAddress cell, MultiDef? multi)
    {
        if (multi is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        XBlockAddress root = context.Blocks.AllocateCurrent(MultiDef.SerializedSize);
        WriteFixedStringPointerArray(context, multi.DvarListStrings, MultiDef.EntryCapacity, "MultiDef.dvarList");
        WriteFixedStringPointerArray(context, multi.DvarStrStrings, MultiDef.EntryCapacity, "MultiDef.dvarStr");
        WriteFixedFloatArray(context, multi.DvarValue, MultiDef.EntryCapacity, "MultiDef.dvarValue");
        context.Source.WriteInt32(multi.Count);
        context.Source.WriteInt32(multi.StrDef);

        for (int i = 0; i < MultiDef.EntryCapacity; i++)
            EmitOptionalXString(context, root.Add(checked(i * sizeof(int))), GetOrNull(multi.DvarListStrings, i));
        for (int i = 0; i < MultiDef.EntryCapacity; i++)
            EmitOptionalXString(context, root.Add(checked(0x80 + i * sizeof(int))), GetOrNull(multi.DvarStrStrings, i));
    }

    private static void EmitNewsTickerPointer(XEmitContext context, XBlockAddress cell, NewsTickerDef? ticker)
    {
        if (ticker is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        context.Blocks.AllocateCurrent(NewsTickerDef.SerializedSize);
        context.Source.WriteInt32(ticker.FeedId);
        context.Source.WriteInt32(ticker.Speed);
        context.Source.WriteInt32(ticker.Spacing);
        context.Source.WriteInt32(ticker.LastTime);
        context.Source.WriteInt32(ticker.Start);
        context.Source.WriteInt32(ticker.End);
        context.Source.WriteSingle(ticker.X);
    }

    private static void EmitTextScrollPointer(XEmitContext context, XBlockAddress cell, TextScrollDef? textScroll)
    {
        if (textScroll is null)
            return;

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        context.Blocks.AllocateCurrent(TextScrollDef.SerializedSize);
        context.Source.WriteInt32(textScroll.StartTime);
    }

    private static void EmitItemFloatExpressions(XEmitContext context, XBlockAddress cell, IReadOnlyList<ItemFloatExpression> expressions)
    {
        if (expressions.Count == 0)
            return;

        XBlockAddress table = PatchAndAllocateTable(context, cell, expressions.Count, ItemFloatExpression.SerializedSize);
        foreach (ItemFloatExpression expression in expressions)
        {
            context.Source.WriteInt32((int)expression.Target);
            context.Source.WriteInt32(PointerRaw(expression.Statement));
        }

        for (int i = 0; i < expressions.Count; i++)
            EmitStatementPointer(context, table.Add(checked(i * ItemFloatExpression.SerializedSize + 0x04)), expressions[i].Statement);
    }

    private static XBlockAddress PatchAndAllocateTable(XEmitContext context, XBlockAddress cell, int count, int stride)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Inline table emit requires a positive element count.");

        context.Blocks.PatchInlinePointerCell(cell, alignment: 4);
        return context.Blocks.AllocateCurrent(checked(count * stride));
    }

    private static void EmitRectangle(XEmitContext context, RectangleDef rectangle)
    {
        context.Source.WriteSingle(rectangle.X);
        context.Source.WriteSingle(rectangle.Y);
        context.Source.WriteSingle(rectangle.W);
        context.Source.WriteSingle(rectangle.H);
        context.Source.WriteByte((byte)rectangle.HorzAlign);
        context.Source.WriteByte((byte)rectangle.VertAlign);
        context.Source.WriteUInt16(rectangle.Pad12);
    }

    private static void WriteFixedRectArray(XEmitContext context, IReadOnlyList<RectangleDef> values, int count, string owner)
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"{owner} must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
            EmitRectangle(context, values.Count == 0 ? new RectangleDef() : values[i]);
    }

    private static void WriteTransitions(XEmitContext context, IReadOnlyList<MenuTransition> transitions, string owner)
    {
        if (transitions.Count is not 0 && transitions.Count != 4)
            throw new InvalidDataException($"{owner} must contain exactly 4 entries when provided.");

        for (int i = 0; i < 4; i++)
            EmitTransition(context, transitions.Count == 0 ? null : transitions[i]);
    }

    private static void EmitTransition(XEmitContext context, MenuTransition? transition)
    {
        context.Source.WriteInt32((int)(transition?.TransitionType ?? MenuTransitionType.TRANS_INACTIVE));
        context.Source.WriteInt32(transition?.TargetField ?? 0);
        context.Source.WriteInt32(transition?.StartTime ?? 0);
        context.Source.WriteSingle(transition?.StartValue ?? 0);
        context.Source.WriteSingle(transition?.EndValue ?? 0);
        context.Source.WriteSingle(transition?.Time ?? 0);
        context.Source.WriteInt32((int)(transition?.EndTriggerType ?? MenuTransitionEndTrigger.TRIGGER_NONE));
    }

    private static void WriteColumnInfoArray(XEmitContext context, IReadOnlyList<ColumnInfo> values, int count)
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"ListBoxDef.columnInfo must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
        {
            ColumnInfo value = values.Count == 0 ? new ColumnInfo() : values[i];
            context.Source.WriteInt32(value.Pos);
            context.Source.WriteInt32(value.Width);
            context.Source.WriteInt32(value.MaxChars);
            context.Source.WriteInt32(value.Alignment);
        }
    }

    private static void WriteFixedStringPointerArray(XEmitContext context, IReadOnlyList<string?> values, int count, string owner)
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"{owner} must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
            context.Source.WriteInt32(PointerRaw(GetOrNull(values, i)));
    }

    private static void WriteFixedFloatArray(XEmitContext context, IReadOnlyList<float> values, int count, string owner)
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"{owner} must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
            context.Source.WriteSingle(values.Count == 0 ? 0 : values[i]);
    }

    private static void WriteFixedInt32Array(XEmitContext context, IReadOnlyList<int> values, int count, string owner)
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"{owner} must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
            context.Source.WriteInt32(values.Count == 0 ? 0 : values[i]);
    }

    private static void WriteFixedEnumArray<TEnum>(XEmitContext context, IReadOnlyList<TEnum> values, int count, string owner)
        where TEnum : Enum
    {
        if (values.Count is not 0 && values.Count != count)
            throw new InvalidDataException($"{owner} must contain exactly {count} entries when provided.");

        for (int i = 0; i < count; i++)
            context.Source.WriteInt32(values.Count == 0 ? 0 : Convert.ToInt32(values[i]));
    }

    private static void WriteVec4(XEmitContext context, Vec4 value)
    {
        context.Source.WriteSingle(value.A);
        context.Source.WriteSingle(value.R);
        context.Source.WriteSingle(value.G);
        context.Source.WriteSingle(value.B);
    }

    private static void EmitOptionalXString(XEmitContext context, XBlockAddress cell, string? value)
    {
        if (value is not null)
            EmitInlineXString(context, cell, value);
    }

    private static void EmitInlineXString(XEmitContext context, XBlockAddress cell, string value)
    {
        context.Blocks.PatchInlinePointerCell(cell);
        context.Blocks.AllocateCurrent(checked(System.Text.Encoding.Latin1.GetByteCount(value) + 1));
        context.Source.WriteCString(value);
    }

    private static void WriteExternalNullPointer(XEmitContext context, int raw, string fieldName)
    {
        RejectExternalPointer(raw, fieldName);
        context.Source.WriteInt32(0);
    }

    private static void RejectExternalPointer(int raw, string fieldName)
    {
        if (raw != 0)
            throw new NotSupportedException($"{fieldName} is an external/reference asset pointer; linker alias-cell emission is not implemented here.");
    }

    private static string Required(string? value, string fieldName)
    {
        return value ?? throw new InvalidDataException($"{fieldName} is required for inline PS3 emission.");
    }

    private static string? GetOrNull(IReadOnlyList<string?> values, int index)
    {
        return values.Count == 0 ? null : values[index];
    }

    private static int EventDataRaw(MenuEventHandler handler)
    {
        return handler.EventType switch
        {
            MenuEventHandlerType.UnconditionalScript => PointerRaw(handler.UnconditionalScript),
            MenuEventHandlerType.ConditionalScript => PointerRaw(handler.ConditionalScript),
            MenuEventHandlerType.ElseScript => PointerRaw(handler.ElseScriptSet),
            MenuEventHandlerType.SetLocalVarBool or MenuEventHandlerType.SetLocalVarInt or
                MenuEventHandlerType.SetLocalVarFloat or MenuEventHandlerType.SetLocalVarString => PointerRaw(handler.SetLocalVarData),
            _ => 0
        };
    }

    private static int PointerRaw(string? value) => value is null ? 0 : -1;
    private static int PointerRaw(object? value) => value is null ? 0 : -1;

    private static int PointerRaw(int count)
    {
        if (count < 0)
            throw new InvalidDataException($"Negative pointer-backed count {count}.");

        return count == 0 ? 0 : -1;
    }

    private static void ValidateCount(int declared, int actual, string owner)
    {
        if (declared < 0)
            throw new InvalidDataException($"{owner} has negative declared count {declared}.");

        if (declared != actual)
            throw new InvalidDataException($"{owner} declared count {declared} does not match {actual} value(s).");
    }
}
