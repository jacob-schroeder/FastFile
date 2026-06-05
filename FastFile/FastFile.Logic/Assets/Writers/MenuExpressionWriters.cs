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
    private static void WriteExpressionSupportingData(
        XFileWriterContext context,
        ExpressionSupportingData data)
    {
        WriteUIFunctionList(context, data.UiFunctions);
        WriteStaticDvarList(context, data.StaticDvarList);
        WriteStringList(context, data.UiStrings);

        if (data.UiFunctions.Functions is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(data.UiFunctions.Functions);
            WritePointerArray(context, data.UiFunctions.Functions.Result);
            foreach (var function in data.UiFunctions.Functions.Result)
            {
                if (function.Result is { } statement)
                {
                    context.RegisterMaterializedPointerValue(function);
                    WriteStatement(context, statement);
                }
            }
        }

        if (data.StaticDvarList.StaticDvars is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(data.StaticDvarList.StaticDvars);
            WritePointerArray(context, data.StaticDvarList.StaticDvars.Result);
            foreach (var staticDvarPointer in data.StaticDvarList.StaticDvars.Result)
            {
                if (staticDvarPointer.Result is { } staticDvar)
                {
                    context.RegisterMaterializedPointerValue(staticDvarPointer);
                    WriteStaticDvar(context, staticDvar);
                }
            }
        }

        if (data.UiStrings.Strings is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(data.UiStrings.Strings);
            WritePointerArray(context, data.UiStrings.Strings.Result);
            foreach (var stringPointer in data.UiStrings.Strings.Result)
            {
                if (stringPointer.Result is { } value)
                {
                    context.RegisterMaterializedPointerValue(stringPointer);
                    context.WriteCString(value);
                }
            }
        }
    }

    private static void WriteUIFunctionList(XFileWriterContext context, UIFunctionList list)
    {
        context.WriteInt32(list.TotalFunctions);
        context.WritePointerRaw(list.Functions);
    }

    private static void WriteStaticDvarList(XFileWriterContext context, StaticDvarList list)
    {
        context.WriteInt32(list.NumStaticDvars);
        context.WritePointerRaw(list.StaticDvars);
    }

    private static void WriteStringList(XFileWriterContext context, StringList list)
    {
        context.WriteInt32(list.TotalStrings);
        context.WritePointerRaw(list.Strings);
    }

    private static void WritePointerArray<T>(
        XFileWriterContext context,
        IReadOnlyList<ZonePointer<T>> pointers)
    {
        foreach (var pointer in pointers)
            context.WritePointerRaw(pointer);
    }

    private static void WriteStaticDvar(XFileWriterContext context, StaticDvar value)
    {
        context.WritePointerRaw(value.Dvar);
        var name = WriteInlineStringPointer(context, value.DvarName);
        WritePendingString(context, name);
    }

    private static void WriteStatement(XFileWriterContext context, Statement statement)
    {
        context.WriteInt32(statement.NumEntries);
        context.WritePointerRaw(statement.Entries);
        context.WritePointerRaw(statement.SupportingData);
        context.WriteInt32(statement.LastExecuteTime);
        WriteOperandRaw(context, statement.LastResult);

        if (statement.Entries is { IsInlineData: true, Result: not null })
        {
            context.RegisterMaterializedPointerValue(statement.Entries);

            var pendingStrings = new List<ZonePointer<string>>();
            var pendingFunctions = new List<ZonePointer<Statement>>();
            foreach (var entry in statement.Entries.Result)
                WriteExpressionEntry(context, entry, pendingStrings, pendingFunctions);

            foreach (var value in pendingStrings)
                WriteQueuedString(context, value);

            foreach (var value in pendingFunctions)
                WriteQueuedStatement(context, value);
        }
    }

    private static void WriteExpressionEntry(
        XFileWriterContext context,
        ExpressionEntry entry,
        List<ZonePointer<string>> pendingStrings,
        List<ZonePointer<Statement>> pendingFunctions)
    {
        context.WriteInt32(entry.Type);
        if (entry.Type != 1)
        {
            context.WriteInt32((int)entry.Data.Op);
            context.WriteInt32(entry.Data.Operand?.Internals?.IntVal ?? 0);
            return;
        }

        WriteOperand(context, entry.Data.Operand, pendingStrings, pendingFunctions);
    }

    private static void WriteOperand(
        XFileWriterContext context,
        Operand operand,
        List<ZonePointer<string>> pendingStrings,
        List<ZonePointer<Statement>> pendingFunctions)
    {
        context.WriteInt32((int)operand.DataType);
        switch (operand.DataType)
        {
            case ExpDataType.VAL_STRING:
                context.WritePointerRaw(operand.Internals.StringVal);
                if (operand.Internals.StringVal is { IsInlineData: true, Result: not null })
                    pendingStrings.Add(operand.Internals.StringVal);
                break;
            case ExpDataType.VAL_FUNCTION:
                context.WritePointerRaw(operand.Internals.Function);
                if (operand.Internals.Function is { IsInlineData: true, Result: not null })
                    pendingFunctions.Add(operand.Internals.Function);
                break;
            case ExpDataType.VAL_FLOAT:
                context.WriteInt32(BitConverter.SingleToInt32Bits(operand.Internals.FloatVal));
                break;
            default:
                context.WriteInt32(operand.Internals.IntVal);
                break;
        }
    }

    private static void WriteOperandRaw(XFileWriterContext context, Operand operand)
    {
        context.WriteInt32((int)operand.DataType);
        context.WriteInt32(operand.Internals.IntVal);
    }

    private static void WriteWindow(
        XFileWriterContext context,
        Window window,
        int? dynamicFlagCount = null)
    {
        context.WritePointerRaw(window.NamePtr, PointerResolutionKind.Direct, "Window.Name");
        WriteRectangle(context, window.Rect);
        WriteRectangle(context, window.RectClient);
        context.WritePointerRaw(window.GroupPtr, PointerResolutionKind.Direct, "Window.Group");
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
        context.WritePointerRaw(window.Background, PointerResolutionKind.Alias, "Window.Background");
    }

    private static void WriteRectangle(XFileWriterContext context, RectangleDef rectangle)
    {
        context.WriteFloat(rectangle.X);
        context.WriteFloat(rectangle.Y);
        context.WriteFloat(rectangle.W);
        context.WriteFloat(rectangle.H);
        context.WriteByte(rectangle.HorzAlign);
        context.WriteByte(rectangle.VertAlign);
        context.WriteUInt16(rectangle.AlignmentPadding);
    }

    private static void WriteMenuTransitions(XFileWriterContext context, MenuTransition[] transitions)
    {
        foreach (var transition in transitions)
            WriteMenuTransition(context, transition);
    }

    private static void WriteMenuTransition(XFileWriterContext context, MenuTransition transition)
    {
        context.WriteInt32((int)transition.TransitionType);
        context.WriteInt32(transition.TargetField);
        context.WriteInt32(transition.StartTime);
        context.WriteFloat(transition.StartVal);
        context.WriteFloat(transition.EndVal);
        context.WriteFloat(transition.Time);
        context.WriteInt32((int)transition.EndTriggerType);
    }

    private static void WriteInt32Array(XFileWriterContext context, int[] values)
    {
        WriteInt32Array(context, values, values.Length);
    }

    private static void WriteInt32Array(XFileWriterContext context, int[] values, int count)
    {
        if (count < 0 || count > values.Length)
            throw new InvalidDataException($"Invalid Int32 array count {count:N0} for array length {values.Length:N0}.");

        for (var i = 0; i < count; i++)
            context.WriteInt32(values[i]);
    }

    private static ZonePointer<string>? WriteInlineStringPointer(
        XFileWriterContext context,
        ZonePointer<string>? pointer)
    {
        if (pointer is null)
        {
            context.WriteNullPointer();
            return null;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            context.WritePointerRaw(pointer, PointerResolutionKind.Direct, "XString");
            return null;
        }

        if (pointer.Result is null)
        {
            context.WriteNullPointer();
            return null;
        }

        context.WritePointerRaw(pointer, PointerResolutionKind.Direct, "XString");
        return pointer.IsInlineData ? pointer : null;
    }

    private static void WritePendingString(XFileWriterContext context, ZonePointer<string>? pointer)
    {
        WriteQueuedString(context, pointer);
    }
}
