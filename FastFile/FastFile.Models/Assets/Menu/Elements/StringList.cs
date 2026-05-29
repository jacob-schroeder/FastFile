using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct StringList
{
    public int TotalStrings;
    public ZonePointer<ZonePointer<string>[]> Strings;
}