using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Generic;

internal static class GenericReader
{
    public static ZonePointer<string> ReadStringPointer(
        ref ZoneReadContext context,
        bool resolve = true)
    {
        return resolve
            ? context.ReadPointer<string>(ReadStringPointerValue)
            : context.ReadPointer<string>();
    }

    public static ZonePointer<ZonePointer<string>[]> ReadStringPointerArrayPointer(
        ref ZoneReadContext context,
        int count)
    {
        var pointer = context.ReadPointer<ZonePointer<string>[]>();
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<string>[]> p) =>
        {
            var values = new ZonePointer<string>[Math.Max(0, count)];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadPointer<string>();

            p.SetResult(values);

            foreach (var value in values)
                ResolveStringPointerNow(ref pointerContext, value);
        });

        return pointer;
    }

    public static void ReadStringPointerValue(
        ref ZoneReadContext context,
        ZonePointer<string> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadCString));
    }

    public static void ResolveStringPointerNow(
        ref ZoneReadContext context,
        ZonePointer<string> pointer)
    {
        if (pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(pointer, ReadStringPointerValue);
    }

    public static string ReadCString(ref ZoneReadContext context)
    {
        return context.ReadCString();
    }
}
