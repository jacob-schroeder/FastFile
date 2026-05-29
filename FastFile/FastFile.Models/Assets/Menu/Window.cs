using FastFile.Models.Data;
using FastFile.Models.Assets.Menu.Elements;

namespace FastFile.Models.Assets.Menu;

public class Window
{
    public ZonePointer<string> NamePtr  { get; set; }
    public string Name => NamePtr?.Result ?? string.Empty;

    public RectangleDef Rect { get; set; }
    public RectangleDef RectClient { get; set; }
    
    public ZonePointer<string> GroupPtr { get; set; }
    public string Group => GroupPtr?.Result ?? string.Empty;
    
    public int Style { get; set; }
    public int Border { get; set; }
    public int OwnerDraw { get; set; }
    public int OwnerDrawFlags { get; set; }
    public float BorderSize { get; set; }
    public int StaticFlags { get; set; }
#if !PC
    public int[] DynamicFlags { get; set; } = new int[4];
#else
    public int[] DynamicFlags  { get; set; } = new int[1];
#endif
    public int NextTime { get; set; }
    public Vec4 foreColor { get; set; }
    public Vec4 backColor { get; set; }
    public Vec4 borderColor { get; set; }
    public Vec4 outlineColor { get; set; }
    public Vec4 disableColor { get; set; }
    public ZonePointer<Material.Material> MaterialPtr { get; set; }
}