using FastFile.Models.Zone;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu;

public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    public Window Window { get; set; }
    public ZonePointer<string> FontPtr {  get; set; }
    public string Font => FontPtr?.Result ?? string.Empty;
    
    public int fullscreen { get; set; }
    public int itemCount { get; set; }
    public int fontIndex { get; set; }
    #if !PC
    public int[] cursorItems { get; set; } = new int[4];
    #else
    public int[] cursorItems { get; set; } = new int[1];
    #endif
    public int fadeCycle { get; set; }
    public float fadeClamp { get; set; }
    public float fadeAmount { get; set; }
    public float fadeInAmount { get; set; }
    public float blurRadius { get; set; }
    /*
    MenuEventHandlerSet *onOpen;
    MenuEventHandlerSet *onRequestClose;
    MenuEventHandlerSet *onClose;
    MenuEventHandlerSet *onEsc;
    ItemKeyHandler *execKeys;
    Statement_s *visibleExp;
    const char *allowedBinding;
    const char *soundName;
    int imageTrack;
    vec4_t focusColor;
    Statement_s *rectXExp;
    Statement_s *rectYExp;
    Statement_s *rectHExp;
    Statement_s *rectWExp; 
#ifdef PC
    Statement_s *openSoundExp;
    Statement_s *closeSoundExp;
#endif
    itemDef_s **items; 
#ifndef PC
    menuTransition scaleTransition[4];
    menuTransition alphaTransition[4];
    menuTransition xTransition[4];
    menuTransition yTransition[4];
#else
    char unknown[112];
#endif
    ExpressionSupportingData *expressionData;
    */
    
    
    
    public override string? GetDisplayName => "menufile";
}