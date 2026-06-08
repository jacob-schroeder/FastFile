using FastFile.Logic.Extensions;
using FastFile.Models.Data;

namespace FastFile.Logic;

internal static class Memory
{
    public static ZonePointer<T> ReadPointer<T>(ReadOnlySpan<byte> span, ref int position)
    {
        int raw = span.ReadInt32(ref position);

        return new ZonePointer<T>(raw);
    }

}
