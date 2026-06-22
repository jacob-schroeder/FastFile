using System.Reflection;
using FastFile.ModelsOLD.Assets.Menu;
using FastFile.ModelsOLD.Assets.Menu.Enums;
using FastFile.ModelsOLD.Assets.Menu.Elements;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.LogicOLD.Assets.Readers;

public sealed class MenuAssetReader : XAssetReadHandler
{
    public override bool TryReadField(
        object owner,
        PropertyInfo property,
        XFieldAttribute field,
        IXAssetReaderContext context,
        out object? value)
    {
        if (owner is ItemDef item && property.PropertyType == typeof(ItemDefData))
        {
            value = ReadItemDefTypeDataPointer(item, context);
            return true;
        }

        value = null;
        return false;
    }

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        if (value is not MenuDef)
            return false;

        context.WithStreamBlock(
            XFILE_BLOCK.LARGE,
            () => context.ResolveObjectPointers(value));
        return true;
    }

    public override bool TryResolvePointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case MenuDef menu:
                ResolveMenuDefPointers(menu, context);
                return true;

            case MenuEventHandler handler:
                ResolveMenuEventHandler(handler, context);
                return true;

            case ConditionalScript conditionalScript:
                ResolveConditionalScript(conditionalScript, context);
                return true;

            case ExpressionEntry entry:
                ResolveExpressionEntry(entry, context);
                return true;

            case EventData:
            case OperandInternalData:
                return true;

            default:
                return false;
        }
    }

    public override bool TryResolveField(
        object owner,
        object? value,
        IXAssetReaderContext context)
    {
        if (owner is ItemDef item && value is ItemDefData typeData)
        {
            ResolveItemDefTypeData(item, typeData, context);
            return true;
        }

        return false;
    }

    private static void ResolveMenuDefPointers(
        MenuDef menu,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(menu, nameof(MenuDef.ExpressionData));
        context.ResolveChildPointers(menu.Window);
        context.ResolvePointerProperty(menu, nameof(MenuDef.FontPtr));
        context.ResolvePointerProperty(menu, nameof(MenuDef.OnOpen));
        context.ResolvePointerProperty(menu, nameof(MenuDef.OnClose));
        context.ResolvePointerProperty(menu, nameof(MenuDef.OnRequestClose));
        context.ResolvePointerProperty(menu, nameof(MenuDef.OnEsc));
        context.ResolvePointerProperty(menu, nameof(MenuDef.ExecKeys));
        context.ResolvePointerProperty(menu, nameof(MenuDef.VisibleExp));
        context.ResolvePointerProperty(menu, nameof(MenuDef.AllowedBinding));
        context.ResolvePointerProperty(menu, nameof(MenuDef.SoundName));
        context.ResolvePointerProperty(menu, nameof(MenuDef.RectXExp));
        context.ResolvePointerProperty(menu, nameof(MenuDef.RectYExp));
        context.ResolvePointerProperty(menu, nameof(MenuDef.RectWExp));
        context.ResolvePointerProperty(menu, nameof(MenuDef.RectHExp));
        context.ResolvePointerProperty(menu, nameof(MenuDef.Items));
    }

    private static void ResolveMenuEventHandler(
        MenuEventHandler handler,
        IXAssetReaderContext context)
    {
        var data = handler.EventData
                   ?? throw new InvalidDataException("MenuEventHandler.EventData was null.");
        var pointer = data.DataPtr
                      ?? throw new InvalidDataException("MenuEventHandler.EventData.DataPtr was null.");

        switch (handler.EventType)
        {
            case 0:
                data.UnconditionalScript = context.ReinterpretPointer<string?>(pointer, PointerResolutionKind.Direct);
                context.MaterializeCStringPointer(data.UnconditionalScript);
                break;

            case 1:
                data.ConditionalScript = context.ReinterpretPointer<ConditionalScript>(pointer, PointerResolutionKind.CurrentStream);
                context.ResolveCurrentStreamObjectPointer(data.ConditionalScript);
                break;

            case 2:
                data.ElseScript = context.ReinterpretPointer<MenuEventHandlerSet>(pointer, PointerResolutionKind.CurrentStream);
                context.ResolveCurrentStreamObjectPointer(data.ElseScript);
                break;

            case >= 3 and <= 6:
                data.SetLocalVarData = context.ReinterpretPointer<SetLocalVarData>(pointer, PointerResolutionKind.CurrentStream);
                context.ResolveCurrentStreamObjectPointer(data.SetLocalVarData);
                break;
        }
    }

    private static void ResolveConditionalScript(
        ConditionalScript conditionalScript,
        IXAssetReaderContext context)
    {
        // EBOOT 0x10c028 resolves +0x04 before +0x00.
        context.ResolvePointerProperty(conditionalScript, nameof(ConditionalScript.EventExpression));
        context.ResolvePointerProperty(conditionalScript, nameof(ConditionalScript.EventHandlerSet));
    }

    private static void ResolveExpressionEntry(
        ExpressionEntry entry,
        IXAssetReaderContext context)
    {
        if (entry.Type == 0)
            return;

        var operand = entry.Data?.Operand
                      ?? throw new InvalidDataException("ExpressionEntry.Data.Operand was null.");
        var internals = operand.Internals
                        ?? throw new InvalidDataException("ExpressionEntry operand internals were null.");

        var payload = internals.DataPtr
                      ?? throw new InvalidDataException("ExpressionEntry operand payload was null.");

        switch (operand.DataType)
        {
            case ExpDataType.VAL_INT:
            case ExpDataType.VAL_FLOAT:
                break;

            case ExpDataType.VAL_STRING:
                internals.StringVal = context.ReinterpretPointer<string?>(payload, PointerResolutionKind.Direct);
                context.MaterializeCStringPointer(internals.StringVal);
                break;

            case ExpDataType.VAL_FUNCTION:
                internals.Function = context.ReinterpretPointer<Statement>(payload, PointerResolutionKind.CurrentStream);
                context.ResolveCurrentStreamObjectPointer(internals.Function);
                break;
        }
    }

    private static ItemDefData ReadItemDefTypeDataPointer(
        ItemDef item,
        IXAssetReaderContext context)
    {
        var typeData = new ItemDefData();

        switch (item.Type)
        {
            case 6:
                typeData.ListBox = context.ReadPointer<ListBoxDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.ListBox.Raw;
                break;

            case 12:
                typeData.Multi = context.ReadPointer<MultiDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.Multi.Raw;
                break;

            case 13:
                typeData.EnumDvarName = context.ReadPointer<string?>(PointerResolutionKind.Direct);
                typeData.Raw = typeData.EnumDvarName.Raw;
                break;

            case 20:
                typeData.NewsTicker = context.ReadPointer<NewsTickerDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.NewsTicker.Raw;
                break;

            case 21:
                typeData.TextScroll = context.ReadPointer<TextScrollDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.TextScroll.Raw;
                break;

            case 0:
            case 4:
            case 9:
            case 10:
            case 11:
            case 14:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                typeData.EditField = context.ReadPointer<EditFieldDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.EditField.Raw;
                break;

            default:
                typeData.Raw = context.ReadPointer<ItemDefData>(PointerResolutionKind.CurrentStream).Raw;
                break;
        }

        return typeData;
    }

    private static void ResolveItemDefTypeData(
        ItemDef item,
        ItemDefData typeData,
        IXAssetReaderContext context)
    {
        if (typeData.ListBox is not null)
        {
            context.ResolveCurrentStreamObjectPointer(typeData.ListBox);
            return;
        }

        if (typeData.Multi is not null)
        {
            context.ResolveCurrentStreamObjectPointer(typeData.Multi);
            return;
        }

        if (typeData.EnumDvarName is not null)
        {
            context.MaterializeCStringPointer(typeData.EnumDvarName);
            return;
        }

        if (typeData.NewsTicker is not null)
        {
            context.ResolveCurrentStreamObjectPointer(typeData.NewsTicker);
            return;
        }

        if (typeData.TextScroll is not null)
        {
            context.ResolveCurrentStreamObjectPointer(typeData.TextScroll);
            return;
        }

        if (typeData.EditField is not null)
            context.ResolveCurrentStreamObjectPointer(typeData.EditField);
    }
}
