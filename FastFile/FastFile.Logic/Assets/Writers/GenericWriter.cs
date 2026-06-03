using FastFile.Logic.Zone;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class GenericWriter
{
    public static void WriteStringPointer(
        ZoneWriterContext context,
        ZonePointer<string>? pointer)
    {
        context.WritePointer(pointer, WriteStringPointerValue);
    }

    public static void WriteStringPointerValue(
        ZoneWriterContext context,
        ZonePointer<string> pointer)
    {
        context.WriteCString(pointer.Result);
    }

    public static void WriteStringPointerArrayPointer(
        ZoneWriterContext context,
        ZonePointer<ZonePointer<string>[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            var values = p.Result ?? [];
            foreach (var value in values)
                WriteStringPointer(pointerContext, value);
        });
    }
}
