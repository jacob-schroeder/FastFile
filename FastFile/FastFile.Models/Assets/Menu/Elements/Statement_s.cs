using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct Statement_s
{
    public int NumEntries;
    public ZonePointer<ExpressionEntry> Entries;
    public ZonePointer<ExpressionSupportingData> SupportingData;
    public unsafe fixed byte Unknown[0xC]; //TODO: Find ut what this is
}