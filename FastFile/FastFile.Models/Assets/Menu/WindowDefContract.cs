using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Menu;

public static class RectangleDefContract
{
    public const int SerializedSize = 0x14;

    public static readonly XStructCodecContract Contract = new(
        "RectangleDef",
        SerializedSize,
        [
            new XScalarFieldContract("x", 0x00, 4, "float", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("y", 0x04, 4, "float", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("w", 0x08, 4, "float", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("h", 0x0C, 4, "float", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("horzAlign", 0x10, 1, "HorizontalAlign", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("vertAlign", 0x11, 1, "VerticalAlign", "PS3 RectangleDef inline field order."),
            new XScalarFieldContract("pad12", 0x12, 2, "uint16", "PS3 RectangleDef inline field order.")
        ],
        "PROVEN: embedded RectangleDef layout consumed by WindowDef/MenuDef/ItemDef menu loaders.");
}

public static class WindowDefContract
{
    public const int SerializedSize = 0xB0;

    public static readonly XPointerFieldContract NamePointer = new(
        "name",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.NullableReferenceOrInline,
        "PROVEN: Load_Window stores window + 0x00 into varXString and calls Load_XString.");

    public static readonly XPointerFieldContract GroupPointer = new(
        "group",
        0x2C,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.NullableReferenceOrInline,
        "PROVEN: Load_Window stores window + 0x2C into varXString and calls Load_XString.");

    public static readonly XPointerFieldContract Background = new(
        "background",
        0xAC,
        "Material",
        XPointerResolutionMode.AliasCell,
        XPointerSourceSemantics.NullableReferenceInlineOrInsert,
        "PROVEN: Load_Window sets varMaterial = window + 0xAC and tail-calls MaterialPtr helper 0x10D980.",
        InlineAlignment: 4,
        InlineBlock: XFileBlockType.TEMP,
        TargetAssetType: XAssetType.Material);

    public static readonly XStructCodecContract Contract = new(
        "WindowDef",
        SerializedSize,
        [
            NamePointer,
            new XStructFieldContract("rect", 0x04, RectangleDefContract.SerializedSize, "RectangleDef", "PROVEN: inline RectangleDef after name pointer."),
            new XStructFieldContract("rectClient", 0x18, RectangleDefContract.SerializedSize, "RectangleDef", "PROVEN: inline RectangleDef after rect."),
            GroupPointer,
            new XScalarFieldContract("style", 0x30, 4, "WindowStyle", "PROVEN: WindowDef inline int32 field."),
            new XScalarFieldContract("border", 0x34, 4, "WindowBorder", "PROVEN: WindowDef inline int32 field."),
            new XScalarFieldContract("ownerDraw", 0x38, 4, "WindowOwnerDraw", "PROVEN: WindowDef inline int32 field."),
            new XScalarFieldContract("ownerDrawFlags", 0x3C, 4, "int32", "PROVEN: WindowDef inline int32 field."),
            new XScalarFieldContract("borderSize", 0x40, 4, "float", "PROVEN: WindowDef inline float field."),
            new XScalarFieldContract("staticFlags", 0x44, 4, "WindowStaticFlags", "PROVEN: WindowDef inline int32 field."),
            new XArrayFieldContract("dynamicFlags", 0x48, 0x10, "WindowDynamicFlags", 4, 4, "PROVEN: PS3 local-client window dynamic flags array."),
            new XScalarFieldContract("nextTime", 0x58, 4, "int32", "PROVEN: WindowDef inline int32 field."),
            new XArrayFieldContract("foreColor", 0x5C, 0x10, "float", 4, 4, "PROVEN: WindowDef inline Vec4 field."),
            new XArrayFieldContract("backColor", 0x6C, 0x10, "float", 4, 4, "PROVEN: WindowDef inline Vec4 field."),
            new XArrayFieldContract("borderColor", 0x7C, 0x10, "float", 4, 4, "PROVEN: WindowDef inline Vec4 field."),
            new XArrayFieldContract("outlineColor", 0x8C, 0x10, "float", 4, 4, "PROVEN: WindowDef inline Vec4 field."),
            new XArrayFieldContract("disableColor", 0x9C, 0x10, "float", 4, 4, "PROVEN: WindowDef inline Vec4 field."),
            Background
        ],
        "PROVEN: EBOOT Load_Window 0x10E4C8 copies 0xB0 bytes, loads XStrings at +0x00/+0x2C, then MaterialPtr at +0xAC.");
}
