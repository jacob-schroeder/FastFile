using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Menu;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x2F0)]
[XEbootEvidence(
    "0x10ecd8",
    "Data/eboot/xasset_loader_findings.txt",
    Detail = "MenuDef inner loader: Load_Stream size 0x2f0; Window at +0x000; XStrings at +0x0b0/+0x0fc/+0x100; event handlers at +0x0e4/+0x0e8/+0x0ec/+0x0f0; expression pointers at +0x0f8/+0x118/+0x11c/+0x120/+0x124; items pointer at +0x128 with count +0x0b8; expression data at +0x2ec.")]
public class MenuDef() : BaseAsset(XAssetType.Menu)
{
    [XField(Offset = 0x00)]
    public Window Window { get; set; }

    [XField(Offset = 0xB0)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> FontPtr { get; set; } // Direct
    public string Font => FontPtr is { IsResolved: true } ? FontPtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0xB4)]
    public int Fullscreen { get; set; }

    [XField(Offset = 0xB8)]
    public int ItemCount { get; set; }

    [XField(Offset = 0xBC)]
    public int FontIndex { get; set; }
#if !PC
    [XField(Offset = 0xC0, Count = 4)]
    public int[] CursorItems { get; set; } = new int[4];
#else
    [XField(Offset = 0xC0, Count = 1)]
    public int[] CursorItems { get; set; } = new int[1];
#endif
    [XField(Offset = 0xD0)]
    public int FadeCycle { get; set; }

    [XField(Offset = 0xD4)]
    public float FadeClamp { get; set; }

    [XField(Offset = 0xD8)]
    public float FadeAmount { get; set; }

    [XField(Offset = 0xDC)]
    public float FadeInAmount { get; set; }

    [XField(Offset = 0xE0)]
    public float BlurRadius { get; set; }

    [XField(Offset = 0xE4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> OnOpen { get; set; } // ?

    [XField(Offset = 0xE8)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> OnRequestClose { get; set; } // ?

    [XField(Offset = 0xEC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> OnClose { get; set; } // ?

    [XField(Offset = 0xF0)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<MenuEventHandlerSet> OnEsc { get; set; } // ?

    [XField(Offset = 0xF4)]
    [XPointerField(ResolutionKind = PointerResolutionKind.CurrentStream, Target = XPointerTarget.Object)]
    public XPointer<ItemKeyHandler> ExecKeys { get; set; } // ?

    [XField(Offset = 0xF8)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> VisibleExp { get; set; } // ?

    [XField(Offset = 0xFC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> AllowedBinding { get; set; } // Direct

    [XField(Offset = 0x100)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> SoundName { get; set; } // Direct

    [XField(Offset = 0x104)]
    public int ImageTrack { get; set; }

    [XField(Offset = 0x108)]
    public Vec4 FocusColor { get; set; }

    [XField(Offset = 0x118)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> RectXExp { get; set; } // ?

    [XField(Offset = 0x11C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> RectYExp { get; set; } // ?

    [XField(Offset = 0x120)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> RectWExp { get; set; } // ?

    [XField(Offset = 0x124)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<Statement> RectHExp { get; set; } // ?

    [XField(Offset = 0x128)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.CurrentStream,
        Target = XPointerTarget.PointerArray,
        CountMember = nameof(ItemCount),
        ElementResolutionKind = PointerResolutionKind.CurrentStream,
        ElementTarget = XPointerTarget.Object)]
    public XPointer<XPointer<ItemDef>[]> Items { get; set; } // ? -> ?
    
    [XField(Offset = 0x12C, Count = 4)]
    public MenuTransition[] ScaleTransition { get; set; } = new MenuTransition[4];

    [XField(Offset = 0x19C, Count = 4)]
    public MenuTransition[] AlphaTransition { get; set; } = new MenuTransition[4];

    [XField(Offset = 0x20C, Count = 4)]
    public MenuTransition[] XTransition { get; set; } = new MenuTransition[4];

    [XField(Offset = 0x27C, Count = 4)]
    public MenuTransition[] YTransition { get; set; } = new MenuTransition[4];

    [XField(Offset = 0x2EC)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<ExpressionSupportingData> ExpressionData { get; set; } // Direct

    public override string? GetDisplayName => Window?.Name ?? string.Empty;
}
