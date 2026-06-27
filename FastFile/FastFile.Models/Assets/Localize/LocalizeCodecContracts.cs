using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Localize;

public static class LocalizeCodecContracts
{
    private const string EvidenceText =
        "PS3 Localize loader: top-level inline payload pushes TEMP, aligns runtime block to 4, " +
        "Load_Stream root size 0x08, then pushes LARGE and loads value/name XStrings in order; " +
        "validated against patch_mp.";

    public static readonly XPointerFieldContract ValuePointer = new(
        "value",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x00: Load_XString for localized value; direct pointer cell patched before string payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XPointerFieldContract NamePointer = new(
        "name",
        0x04,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x04: Load_XString for localize key/name; direct pointer cell patched before string payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XStructCodecContract Root = new(
        "LocalizeAssetRoot",
        LocalizeAsset.SerializedSize,
        [
            ValuePointer,
            NamePointer
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady,
        new XCodecRecipe(
            "LocalizeAssetRoot",
            [
                XCodecOps.PushBlock(XFileBlockType.TEMP, "Top-level Localize wrapper pushes TEMP."),
                XCodecOps.Align(4, "Localize loader aligns the active runtime block to 4 before root Load_Stream."),
                XCodecOps.StreamStruct("LocalizeAssetRoot", LocalizeAsset.SerializedSize, XFileBlockType.TEMP, 4, "Load_Stream root size 0x08."),
                XCodecOps.PushBlock(XFileBlockType.LARGE, "Localize child XStrings load under LARGE."),
                XCodecOps.Field(ValuePointer),
                XCodecOps.Field(NamePointer),
                XCodecOps.PopBlock(XFileBlockType.LARGE, "Localize pops LARGE after both XStrings."),
                XCodecOps.PopBlock(XFileBlockType.TEMP, "Localize pops TEMP after root and children.")
            ],
            EvidenceText));

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.Localize,
        Root,
        EvidenceText,
        XCodecReadiness.EmitterReady);

    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        Root,
        Asset
    ];

    public static void Register(XCodecContractRegistry registry)
    {
        registry.AddRange(All);
    }
}
