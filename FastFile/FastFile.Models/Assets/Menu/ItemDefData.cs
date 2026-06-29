using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ItemDefData
{
    public ItemDefDataValue Value { get; init; } = new NoItemDefData();

    public EditFieldItemDefData? EditField => Value as EditFieldItemDefData;
    public ListBoxItemDefData? ListBox => Value as ListBoxItemDefData;
    public MultiItemDefData? Multi => Value as MultiItemDefData;
    public DvarEnumItemDefData? DvarEnum => Value as DvarEnumItemDefData;
    public NewsTickerItemDefData? NewsTicker => Value as NewsTickerItemDefData;
    public TextScrollItemDefData? TextScroll => Value as TextScrollItemDefData;
}

public abstract class ItemDefDataValue
{
}

public sealed class NoItemDefData : ItemDefDataValue
{
    public int Reserved { get; init; }
}

public sealed class EditFieldItemDefData : ItemDefDataValue
{
    public XPointer<EditFieldDef> EditFieldPointer { get; init; }
}

public sealed class ListBoxItemDefData : ItemDefDataValue
{
    public XPointer<ListBoxDef> ListBoxPointer { get; init; }
}

public sealed class MultiItemDefData : ItemDefDataValue
{
    public XPointer<MultiDef> MultiPointer { get; init; }
}

public sealed class DvarEnumItemDefData : ItemDefDataValue
{
    public XPointer<string> DvarEnumNamePointer { get; init; }
}

public sealed class NewsTickerItemDefData : ItemDefDataValue
{
    public XPointer<NewsTickerDef> NewsTickerPointer { get; init; }
}

public sealed class TextScrollItemDefData : ItemDefDataValue
{
    public XPointer<TextScrollDef> TextScrollPointer { get; init; }
}
