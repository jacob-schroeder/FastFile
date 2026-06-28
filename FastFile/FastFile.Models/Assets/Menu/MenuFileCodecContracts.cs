using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Menu;

public static class MenuFileCodecContracts
{
    private const string EvidenceText =
        "PS3 MenuFile loader: top-level inline payload pushes TEMP, aligns runtime block to 4, " +
        "Load_Stream root size 0x0c, then pushes LARGE and loads name XString followed by a direct MenuDef* table. " +
        "Nested MenuDef roots are loaded under TEMP and children are loaded under LARGE in loader-proven order.";

    public static readonly XPointerFieldContract NamePointer = new(
        "name",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "MenuFile +0x00: direct XString name loaded before menus table.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XScalarFieldContract MenuCount = new(
        "menuCount",
        0x04,
        sizeof(int),
        "int32",
        "MenuFile +0x04: menu pointer count.");

    public static readonly XPointerFieldContract MenusPointer = new(
        "menus",
        0x08,
        "MenuDef*[]",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.NullableReferenceOrInline,
        "MenuFile +0x08: direct counted MenuDef pointer table.",
        InlineAlignment: 4,
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XStructCodecContract Root = new(
        "MenuFileRoot",
        0x0c,
        [NamePointer, MenuCount, MenusPointer],
        EvidenceText);

    public static readonly XStructCodecContract MenuTransition = new(
        "MenuTransition",
        Models.Assets.Menu.MenuTransition.SerializedSize,
        [
            new XScalarFieldContract("transitionType", 0x00, 4, "MenuTransitionType", "MenuTransition +0x00."),
            new XScalarFieldContract("targetField", 0x04, 4, "int32", "MenuTransition +0x04."),
            new XScalarFieldContract("startTime", 0x08, 4, "int32", "MenuTransition +0x08."),
            new XScalarFieldContract("startValue", 0x0C, 4, "float", "MenuTransition +0x0c."),
            new XScalarFieldContract("endValue", 0x10, 4, "float", "MenuTransition +0x10."),
            new XScalarFieldContract("time", 0x14, 4, "float", "MenuTransition +0x14."),
            new XScalarFieldContract("endTriggerType", 0x18, 4, "MenuTransitionEndTrigger", "MenuTransition +0x18.")
        ],
        "PROVEN: MenuTransition fixed 0x1c stride read four times for scale/alpha/x/y transition groups.");

    public static readonly XStructCodecContract MenuDef = new(
        "MenuDefAsset",
        Models.Assets.Menu.MenuDefAsset.SerializedSize,
        [
            new XStructFieldContract("window", 0x00, WindowDefContract.SerializedSize, "WindowDef", "MenuDef +0x00: inline WindowDef."),
            new XPointerFieldContract("font", 0xB0, "XString", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "MenuDef +0xb0: direct font XString.", InlineBlock: XFileBlockType.LARGE),
            new XScalarFieldContract("fullscreen", 0xB4, 4, "int32", "MenuDef +0xb4."),
            new XScalarFieldContract("itemCount", 0xB8, 4, "int32", "MenuDef +0xb8."),
            new XPointerFieldContract("items", 0x128, "ItemDef*[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "MenuDef +0x128: direct counted ItemDef pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XArrayFieldContract("scaleTransitions", 0x12C, 0x70, "MenuTransition", 4, Models.Assets.Menu.MenuTransition.SerializedSize, "MenuDef fixed scale transition array."),
            new XArrayFieldContract("alphaTransitions", 0x19C, 0x70, "MenuTransition", 4, Models.Assets.Menu.MenuTransition.SerializedSize, "MenuDef fixed alpha transition array."),
            new XArrayFieldContract("xTransitions", 0x20C, 0x70, "MenuTransition", 4, Models.Assets.Menu.MenuTransition.SerializedSize, "MenuDef fixed x transition array."),
            new XArrayFieldContract("yTransitions", 0x27C, 0x70, "MenuTransition", 4, Models.Assets.Menu.MenuTransition.SerializedSize, "MenuDef fixed y transition array."),
            new XPointerFieldContract("expressionData", 0x2EC, "ExpressionSupportingData", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "MenuDef +0x2ec: direct expression supporting data pointer.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE)
        ],
        EvidenceText);

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.MenuFile,
        Root,
        EvidenceText);

    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        Root,
        MenuTransition,
        MenuDef,
        Asset
    ];
}
