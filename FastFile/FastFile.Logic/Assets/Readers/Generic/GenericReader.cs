using FastFile.Models.Data;
using FastFile.Logic.Zone;

namespace FastFile.Logic.Assets.Readers.Generic;

internal static class GenericReader
{
    public static ZonePointer<string> ReadStringPointer(
        ref XFileReadContext context,
        bool resolve = true)
    {
        return resolve
            ? context.ReadPointer<string>(
                ReadStringPointerValue,
                PointerResolutionKind.Direct,
                "XString")
            : context.ReadDirectPointer<string>("XString");
    }

    public static ZonePointer<ZonePointer<string>[]> ReadStringPointerArrayPointer(
        ref XFileReadContext context,
        int count,
        string fieldPath = "XStringArray")
    {
        var pointer = context.ReadDirectPointer<ZonePointer<string>[]>(fieldPath);
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<string>[]> p) =>
        {
            var values = new ZonePointer<string>[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadDirectPointer<string>($"{fieldPath}[{i}]");

            p.SetResult(values);

            foreach (var value in values)
                ResolveStringPointerNow(ref pointerContext, value);
        });

        return pointer;
    }

    public static void ReadStringPointerValue(
        ref XFileReadContext context,
        ZonePointer<string> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadCString));
    }

    public static void ResolveStringPointerNow(
        ref XFileReadContext context,
        ZonePointer<string> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(pointer, ReadStringPointerValue);
    }

    public static string ReadCString(ref XFileReadContext context)
    {
        return context.ReadCString();
    }
}
