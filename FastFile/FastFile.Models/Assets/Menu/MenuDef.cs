using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Menu;

public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    public Window Window { get; set; }
    public ZonePointer<string> FontPtr { get; set; }
    public string Font => FontPtr is { IsResolved: true } ? FontPtr.Result ?? string.Empty : string.Empty;

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
    public ZonePointer<Statement> VisibleExp { get; set; }
    public ZonePointer<string> AllowedBinding { get; set; }
    public ZonePointer<string> SoundName { get; set; }
    public int ImageTrack { get; set; }
    public Vec4 FocusColor { get; set; }
    public ZonePointer<Statement> RectXExp { get; set; }
    public ZonePointer<Statement> RectYExp { get; set; }
    public ZonePointer<Statement> RectHExp { get; set; }
    public ZonePointer<Statement> RectWExp { get; set; }
#if PC
    public ZonePointer<Statement> OpenSoundExp { get; set; }
    public ZonePointer<Statement> CloseSoundExp { get; set; }
#endif
    public ZonePointer<ZonePointer<ItemDef>[]> Items { get; set; }
#if !PC
    public MenuTransition[] ScaleTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] AlphaTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] XTransition { get; set; } = new MenuTransition[4];
    public MenuTransition[] YTransition { get; set; } = new MenuTransition[4];
#else
    public byte[] Unknown { get; set; } = new byte[112];
#endif
    public ZonePointer<ExpressionSupportingData> ExpressionData { get; set; }

    public override string? GetDisplayName => Window?.Name ?? string.Empty;
}
