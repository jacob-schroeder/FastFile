using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct SetLocalVarData
{
    public ZonePointer<string> LocalVarName;
    public ZonePointer<Statement_s> Expression;
}