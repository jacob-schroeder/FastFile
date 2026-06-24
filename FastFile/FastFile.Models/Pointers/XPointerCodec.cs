using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;

namespace FastFile.Models.Pointers;

public static class XPointerCodec
{
    public static PointerType GetType(int value)
    {
        if (value == 0)  return PointerType.Null;
        if (value == -1) return PointerType.Inline;
        if (value == -2) return PointerType.Insert;
        return PointerType.Offset;
    }

    public static int Offset(int value) => (value & 0x0FFFFFFF) - 1;
    public static int BlockIndex(int value) => (int)((uint)value >> 28);
    public static int Encode(XBlockAddress address) => ((int)address.BlockType << 28) | (address.Offset + 1);
    public static XBlockAddress Decode(int value) => new((XFileBlockType)BlockIndex(value), Offset(value));
}
