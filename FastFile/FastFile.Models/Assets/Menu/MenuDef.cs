using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Menu;

public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    public Window Window { get; set; }
    public XPointer<string> FontPtr { get; set; } // Direct
    public string Font => FontPtr is { IsResolved: true } ? FontPtr.Value ?? string.Empty : string.Empty;

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
    public XPointer<MenuEventHandlerSet> OnOpen { get; set; } // ?
    public XPointer<MenuEventHandlerSet> OnRequestClose { get; set; } // ?
    public XPointer<MenuEventHandlerSet> OnClose { get; set; } // ?
    public XPointer<MenuEventHandlerSet> OnEsc { get; set; } // ?
    public XPointer<ItemKeyHandler> ExecKeys { get; set; } // ?
    public XPointer<Statement> VisibleExp { get; set; } // ?
    public XPointer<string> AllowedBinding { get; set; } // Direct
    public XPointer<string> SoundName { get; set; } // Direct
    public int ImageTrack { get; set; }
    public Vec4 FocusColor { get; set; }
    public XPointer<Statement> RectXExp { get; set; } // ?
    public XPointer<Statement> RectYExp { get; set; } // ?
    public XPointer<Statement> RectHExp { get; set; } // ?
    public XPointer<Statement> RectWExp { get; set; } // ?
    public XPointer<XPointer<ItemDef>[]> Items { get; set; } // ? -> ?
    
    public MenuTransition[] ScaleTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] AlphaTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] XTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] YTransition { get; set; } = new MenuTransition[4];

    public XPointer<ExpressionSupportingData> ExpressionData { get; set; } // ?

    public override string? GetDisplayName => Window?.Name ?? string.Empty;
}
