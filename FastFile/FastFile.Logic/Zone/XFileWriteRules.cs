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
    public const int Ps3AssetStreamLeadInSize = 0x58;

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
