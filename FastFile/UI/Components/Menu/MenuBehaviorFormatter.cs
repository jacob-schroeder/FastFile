using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FastFile.Models.Zone;

namespace UI.Components.Menu;

internal static class MenuBehaviorFormatter
{
    public static MenuInsightDisplayItem[] BuildMenuEvents(MenuDef menu)
    {
        return
        [
            BuildEvent("On Open", menu.OnOpen),
            BuildEvent("On Request Close", menu.OnRequestClose),
            BuildEvent("On Close", menu.OnClose),
            BuildEvent("On Esc", menu.OnEsc),
            BuildExecKeys("Exec Keys", menu.ExecKeys)
        ];
    }

    public static MenuInsightDisplayItem[] BuildMenuExpressions(MenuDef menu)
    {
        var items = new List<MenuInsightDisplayItem>
        {
            BuildExpression("Visible", menu.VisibleExp),
            BuildExpression("Rect X", menu.RectXExp),
            BuildExpression("Rect Y", menu.RectYExp),
            BuildExpression("Rect Height", menu.RectHExp),
            BuildExpression("Rect Width", menu.RectWExp)
        };

#if PC
        items.Add(BuildExpression("Open Sound", menu.OpenSoundExp));
        items.Add(BuildExpression("Close Sound", menu.CloseSoundExp));
#endif

        return items.ToArray();
    }

    public static MenuInsightDisplayItem[] BuildItemExpressions(IEnumerable<MenuItemDefDisplayItem> items)
    {
        var rows = new List<MenuInsightDisplayItem>();

        foreach (var item in items.Where(item => item.Item is not null))
        {
            var label = $"#{item.Index} {item.Name}";
            AddItemExpression(rows, label, "Visible", item.Item!.VisibleExp);
            AddItemExpression(rows, label, "Disabled", item.Item.DisabledExp);
            AddItemExpression(rows, label, "Text", item.Item.TextExp);
            AddItemExpression(rows, label, "Material", item.Item.MaterialExp);
            AddFloatExpressions(rows, label, item.Item.FloatExpressions);
        }

        return rows.ToArray();
    }

    public static MenuInsightDisplayItem[] BuildItemEvents(ItemDef item)
    {
        return
        [
            BuildEvent("Mouse Enter Text", item.MouseEnterText),
            BuildEvent("Mouse Exit Text", item.MouseExitText),
            BuildEvent("Mouse Enter", item.MouseEnter),
            BuildEvent("Mouse Exit", item.MouseExit),
            BuildEvent("Action", item.Action),
            BuildEvent("Accept", item.Accept),
            BuildEvent("On Focus", item.OnFocus),
            BuildEvent("Leave Focus", item.LeaveFocus),
            BuildExecKeys("On Key", item.OnKey)
        ];
    }

    public static MenuInsightDisplayItem[] BuildItemExpressions(ItemDef item)
    {
        var rows = new List<MenuInsightDisplayItem>();

        AddItemExpression(rows, "Visible", item.VisibleExp);
        AddItemExpression(rows, "Disabled", item.DisabledExp);
        AddItemExpression(rows, "Text", item.TextExp);
        AddItemExpression(rows, "Material", item.MaterialExp);
        AddFloatExpressions(rows, item.FloatExpressions);

        return rows.ToArray();
    }

    public static string FormatStatementPointer(XPointer<Statement>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return MenuDisplayFormatter.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        return FormatStatement(pointer.Value);
    }

    private static MenuInsightDisplayItem BuildEvent(string name, XPointer<MenuEventHandlerSet>? pointer)
    {
        return new MenuInsightDisplayItem(
            name,
            FormatEventSummary(pointer),
            FormatEventHandlerSetPointer(pointer));
    }

    private static MenuInsightDisplayItem BuildExecKeys(string name, XPointer<ItemKeyHandler>? pointer)
    {
        return new MenuInsightDisplayItem(
            name,
            FormatExecKeysSummary(pointer),
            FormatItemKeyHandlerPointer(pointer));
    }

    private static MenuInsightDisplayItem BuildExpression(string name, XPointer<Statement>? pointer)
    {
        return new MenuInsightDisplayItem(
            name,
            FormatStatementPointer(pointer),
            FormatPointerStatus(pointer));
    }

    private static void AddItemExpression(
        List<MenuInsightDisplayItem> rows,
        string itemLabel,
        string expressionName,
        XPointer<Statement>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return;
        }

        rows.Add(new MenuInsightDisplayItem(
            $"{itemLabel} - {expressionName}",
            FormatStatementPointer(pointer),
            FormatPointerStatus(pointer)));
    }

    private static void AddItemExpression(
        List<MenuInsightDisplayItem> rows,
        string expressionName,
        XPointer<Statement>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return;
        }

        rows.Add(new MenuInsightDisplayItem(
            expressionName,
            FormatStatementPointer(pointer),
            FormatPointerStatus(pointer)));
    }

    private static void AddFloatExpressions(
        List<MenuInsightDisplayItem> rows,
        string itemLabel,
        XPointer<ItemFloatExpression[]>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            rows.Add(new MenuInsightDisplayItem($"{itemLabel} - Float Expressions", MenuDisplayFormatter.OffsetPointerText));
            return;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            rows.Add(new MenuInsightDisplayItem($"{itemLabel} - Float Expressions", MenuDisplayFormatter.UnresolvedPointerText));
            return;
        }

        foreach (var expression in pointer.Value)
        {
            rows.Add(new MenuInsightDisplayItem(
                $"{itemLabel} - {CleanName(expression.Target.ToString())}",
                FormatStatementPointer(expression.Expression),
                FormatPointerStatus(expression.Expression)));
        }
    }

    private static void AddFloatExpressions(
        List<MenuInsightDisplayItem> rows,
        XPointer<ItemFloatExpression[]>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            rows.Add(new MenuInsightDisplayItem("Float Expressions", MenuDisplayFormatter.OffsetPointerText));
            return;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            rows.Add(new MenuInsightDisplayItem("Float Expressions", MenuDisplayFormatter.UnresolvedPointerText));
            return;
        }

        foreach (var expression in pointer.Value)
        {
            rows.Add(new MenuInsightDisplayItem(
                CleanName(expression.Target.ToString()),
                FormatStatementPointer(expression.Expression),
                FormatPointerStatus(expression.Expression)));
        }
    }

    private static string FormatEventSummary(XPointer<MenuEventHandlerSet>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return MenuDisplayFormatter.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        return pointer.Value.EventHandlerCount == 1
            ? "1 handler"
            : $"{pointer.Value.EventHandlerCount:N0} handlers";
    }

    private static string FormatExecKeysSummary(XPointer<ItemKeyHandler>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return MenuDisplayFormatter.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        var count = CountKeyHandlers(pointer.Value);
        return count == 1 ? "1 key handler" : $"{count:N0} key handlers";
    }

    private static string FormatEventHandlerSetPointer(
        XPointer<MenuEventHandlerSet>? pointer,
        int depth = 0)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return string.Empty;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        var set = pointer.Value;
        if (set.EventHandlers is not { IsResolved: true, Value: not null })
        {
            return FormatPointerStatus(set.EventHandlers);
        }

        var handlers = set.EventHandlers.Value
            .Select((handler, index) => FormatEventHandlerPointer(handler, index, depth))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, handlers);
    }

    private static string FormatEventHandlerPointer(
        XPointer<MenuEventHandler>? pointer,
        int index,
        int depth)
    {
        var indent = new string(' ', depth * 2);
        var prefix = $"{indent}{index + 1}. ";

        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return $"{prefix}{MenuDisplayFormatter.NullPointerText}";
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return $"{prefix}{MenuDisplayFormatter.OffsetPointerText}";
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return $"{prefix}{MenuDisplayFormatter.UnresolvedPointerText}";
        }

        return prefix + FormatEventHandler(pointer.Value, depth);
    }

    private static string FormatEventHandler(MenuEventHandler handler, int depth)
    {
        return handler.EventType switch
        {
            0 => MenuDisplayFormatter.FormatStringPointer(
                handler.EventData.UnconditionalScript,
                handler.EventData.UnconditionalScript?.Value,
                string.Empty),
            1 => FormatConditionalScript(handler.EventData.ConditionalScript, depth),
            2 => $"else {{{Environment.NewLine}{FormatEventHandlerSetPointer(handler.EventData.ElseScript, depth + 1)}{Environment.NewLine}{new string(' ', depth * 2)}}}",
            3 => FormatSetLocalVar(handler.EventData.SetLocalVarData, "bool"),
            4 => FormatSetLocalVar(handler.EventData.SetLocalVarData, "int"),
            5 => FormatSetLocalVar(handler.EventData.SetLocalVarData, "float"),
            6 => FormatSetLocalVar(handler.EventData.SetLocalVarData, "string"),
            _ => $"event type {handler.EventType}: 0x{handler.EventData.Raw:X8}"
        };
    }

    private static string FormatConditionalScript(XPointer<ConditionalScript>? pointer, int depth)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return MenuDisplayFormatter.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        var indent = new string(' ', depth * 2);
        var body = FormatEventHandlerSetPointer(pointer.Value.EventHandlerSet, depth + 1);
        return $"if ({FormatStatementPointer(pointer.Value.EventExpression)}) {{{Environment.NewLine}{body}{Environment.NewLine}{indent}}}";
    }

    private static string FormatSetLocalVar(XPointer<SetLocalVarData>? pointer, string typeName)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return MenuDisplayFormatter.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        var localVar = MenuDisplayFormatter.FormatStringPointer(
            pointer.Value.LocalVarName,
            pointer.Value.LocalVar,
            "(unnamed local var)");
        return $"set {typeName} {localVar} = {FormatStatementPointer(pointer.Value.Expression)}";
    }

    private static string FormatItemKeyHandlerPointer(XPointer<ItemKeyHandler>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return string.Empty;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        var lines = new List<string>();
        var current = pointer.Value;
        var index = 1;
        while (current is not null)
        {
            lines.Add($"{index}. key {current.Key}: {FormatEventHandlerSetPointer(current.Action)}");
            current = current.Next is { IsResolved: true } ? current.Next.Value : null;
            index++;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static int CountKeyHandlers(ItemKeyHandler handler)
    {
        var count = 0;
        var current = handler;
        while (current is not null)
        {
            count++;
            current = current.Next is { IsResolved: true } ? current.Next.Value : null;
        }

        return count;
    }

    private static string FormatStatement(Statement statement)
    {
        if (statement.Entries is null || statement.Entries.Kind == PointerKind.Null)
        {
            return "(empty)";
        }

        if (statement.Entries.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!statement.Entries.IsResolved || statement.Entries.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        if (statement.Entries.Value.Length == 0)
        {
            return "(empty)";
        }

        var tokens = statement.Entries.Value.Select(FormatExpressionEntry);
        return string.Join(" ", tokens);
    }

    private static string FormatExpressionEntry(ExpressionEntry entry)
    {
        return entry.Type == 1
            ? FormatOperand(entry.Data.Operand)
            : FormatOperation(entry.Data.Op);
    }

    private static string FormatOperand(Operand? operand)
    {
        if (operand is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        return operand.DataType switch
        {
            ExpDataType.VAL_INT => operand.Internals.IntVal.ToString(CultureInfo.CurrentCulture),
            ExpDataType.VAL_FLOAT => FormatFloat(operand.Internals.FloatVal),
            ExpDataType.VAL_STRING => FormatExpressionString(operand.Internals.StringVal),
            ExpDataType.VAL_FUNCTION => $"({FormatStatementPointer(operand.Internals.Function)})",
            _ => operand.DataType.ToString()
        };
    }

    private static string FormatExpressionString(XPointer<string>? pointer)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return "\"\"";
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return MenuDisplayFormatter.OffsetPointerText;
        }

        if (!pointer.IsResolved || pointer.Value is null)
        {
            return MenuDisplayFormatter.UnresolvedPointerText;
        }

        return $"\"{pointer.Value}\"";
    }

    private static string FormatFloat(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? value.ToString(CultureInfo.CurrentCulture)
            : value.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private static string FormatOperation(OperationEnum op)
    {
        return op switch
        {
            OperationEnum.OP_RIGHTPAREN => ")",
            OperationEnum.OP_LEFTPAREN => "(",
            OperationEnum.OP_MULTIPLY => "*",
            OperationEnum.OP_DIVIDE => "/",
            OperationEnum.OP_MODULUS => "%",
            OperationEnum.OP_ADD => "+",
            OperationEnum.OP_SUBTRACT => "-",
            OperationEnum.OP_NOT => "!",
            OperationEnum.OP_LESSTHAN => "<",
            OperationEnum.OP_LESSTHANEQUALTO => "<=",
            OperationEnum.OP_GREATERTHAN => ">",
            OperationEnum.OP_GREATERTHANEQUALTO => ">=",
            OperationEnum.OP_EQUALS => "==",
            OperationEnum.OP_NOTEQUAL => "!=",
            OperationEnum.OP_AND => "&&",
            OperationEnum.OP_OR => "||",
            OperationEnum.OP_COMMA => ",",
            OperationEnum.OP_BITWISEAND => "&",
            OperationEnum.OP_BITWISEOR => "|",
            OperationEnum.OP_BITWISENOT => "~",
            OperationEnum.OP_BITSHIFTLEFT => "<<",
            OperationEnum.OP_BITSHIFTRIGHT => ">>",
            OperationEnum.OP_NOOP => "noop",
            _ => CleanName(op.ToString())
        };
    }

    private static string FormatPointerStatus(Pointer? pointer)
    {
        return string.Empty;
    }

    private static string CleanName(string name)
    {
        return name
            .Replace("ITEM_FLOATEXP_TGT_", string.Empty, StringComparison.Ordinal)
            .Replace("OP_", string.Empty, StringComparison.Ordinal)
            .Replace('_', ' ')
            .ToLowerInvariant();
    }
}
