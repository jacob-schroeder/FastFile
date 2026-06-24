using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ExpressionString
{
    public const int SerializedSize = 0x04;

    public XPointer<string> String { get; init; }
}
