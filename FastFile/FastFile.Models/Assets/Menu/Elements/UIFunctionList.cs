using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct UIFunctionList
{
    public int TotalFunctions;
    public ZonePointer<ZonePointer<Statement_s>[]> Functions;
}