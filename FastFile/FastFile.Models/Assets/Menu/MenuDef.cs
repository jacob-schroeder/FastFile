using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Zone;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu;

public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    public Window Window { get; set; }
    public ZonePointer<string> FontPtr {  get; set; }
    public string Font => FontPtr?.Result ?? string.Empty;
    
    public int Fullscreen { get; set; }
    public int ItemCount { get; set; }
    public int FontIndex { get; set; }
    #if !PC
    public int[] CursorItems { get; set; } = new int[4];
    #else
    public int[] CursorItems { get; set; } = new int[1];
    #endif
    public int FadeCycle { get; set; }
    public float FadeClamp { get; set; }
    public float FadeAmount { get; set; }
    public float FadeInAmount { get; set; }
    public float BlurRadius { get; set; }
    public ZonePointer<MenuEventHandlerSet> OnOpen { get; set; }
    public ZonePointer<MenuEventHandlerSet> OnRequestClose { get; set; }
    public ZonePointer<MenuEventHandlerSet> OnClose { get; set; }
    public ZonePointer<MenuEventHandlerSet> OnEsc { get; set; }
    public ZonePointer<ItemKeyHandler> ExecKeys { get; set; }
    public ZonePointer<Statement_s> VisibleExp { get; set; }
    public ZonePointer<string> AllowedBinding { get; set; }
    public ZonePointer<string> SoundName { get; set; }
    public int ImageTrack { get; set; }
    public Vec4 FocusColor  { get; set; }
    public ZonePointer<Statement_s> RectXExp { get; set; }
    public ZonePointer<Statement_s> RectYExp { get; set; }
    public ZonePointer<Statement_s> RectHExp { get; set; }
    public ZonePointer<Statement_s> RectWExp { get; set; }
#if PC
    public ZonePointer<Statement_s> OpenSoundExp { get; set; }
    public ZonePointer<Statement_s> CloseSoundExp { get; set; }
#endif
    public ZonePointer<ZonePointer<ItemDef_s>[]> Items; 
#if !PC
    //menuTransition scaleTransition[4];
    //menuTransition alphaTransition[4];
    //menuTransition xTransition[4];
    //menuTransition yTransition[4];
#else
    public unsafe fixed byte unknown[112];
#endif
    public ZonePointer<ExpressionSupportingData> ExpressionData;



    public override string? GetDisplayName => Window.Name;
}