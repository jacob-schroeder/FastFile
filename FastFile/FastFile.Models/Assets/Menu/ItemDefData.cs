using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ItemDefData
{
    public XPointerReference RawPointer { get; init; }

    public XPointer<ListBoxDef> ListBox => RawPointer.AsPointer<ListBoxDef>();
    public XPointer<EditFieldDef> EditField => RawPointer.AsPointer<EditFieldDef>();
    public XPointer<MultiDef> Multi => RawPointer.AsPointer<MultiDef>();
    public XPointer<string> DvarEnumName => RawPointer.AsPointer<string>();
    public XPointer<NewsTickerDef> NewsTicker => RawPointer.AsPointer<NewsTickerDef>();
    public XPointer<TextScrollDef> TextScroll => RawPointer.AsPointer<TextScrollDef>();
}
