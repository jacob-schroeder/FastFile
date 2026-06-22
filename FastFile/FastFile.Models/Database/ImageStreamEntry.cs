namespace FastFile.Models.Database;

public struct ImageStreamEntry
{
    public uint FileIndex;      // 0 = current fastfile, 1..4 = imagefile%d
    public uint SourceStart;    // compressed/source offset in selected file
    public uint SourceEnd;      // compressed/source end offset, not size
    public uint BlockOffset;    // low 16-bit/intra-block logical stream offset
    public uint StreamOffset;   // full logical uncompressed stream offset
}