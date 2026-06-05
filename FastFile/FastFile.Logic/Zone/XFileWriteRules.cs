using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public enum XFileStreamAlignment
{
    Byte = 1,
    Two = 2,
    Four = 4,
    Eight = 8,
    Sixteen = 16,
    SixtyFour = 64,
    OneTwentyEight = 128,
    Page = 4096,
}

public static class XFileWriteRules
{
    public const int PointerSize = 4;
    public const int XAssetEntrySize = 8;
    // EBOOT offset pointers are relative to g_streamBlocks[block], not the serialized zone offset.
    // Official PS3 patches place block 4 string targets at this initial stream cursor.
    public const int Ps3LargeBlockInitialOffset = 0x16A;

    public static XFILE_BLOCK RootBlock => XFILE_BLOCK.LARGE;
    public static XFILE_BLOCK AssetDataBlock => XFILE_BLOCK.LARGE;
    public static XFILE_BLOCK RawBulkDataBlock => XFILE_BLOCK.LARGE;
    public static XFILE_BLOCK VertexDataBlock => XFILE_BLOCK.XFILE_BLOCK_VERTEX;

    public static XFileStreamAlignment StructAlignment => XFileStreamAlignment.Four;
    public static XFileStreamAlignment PointerAlignment => XFileStreamAlignment.Four;
    public static XFileStreamAlignment PointerArrayAlignment => XFileStreamAlignment.Four;
    public static XFileStreamAlignment UShortArrayAlignment => XFileStreamAlignment.Two;
    public static XFileStreamAlignment StringAlignment => XFileStreamAlignment.Byte;
    public static XFileStreamAlignment RawFileBufferAlignment => XFileStreamAlignment.Byte;
    public static XFileStreamAlignment InsertSlotAlignment => XFileStreamAlignment.Four;
}
