using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.RawFile;

public static class RawFileCodecContracts
{
    private const string EvidenceText =
        "PS3 RawFile loader: top-level inline payload pushes TEMP, aligns runtime block to 4, " +
        "Load_Stream root size 0x10, then pushes LARGE and loads name XString followed by buffer bytes; " +
        "buffer length is compressedLen when nonzero else len + 1; validated against patch_mp.";

    public static readonly XPointerFieldContract NamePointer = new(
        "name",
        0x00,
        "XString",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x00: Load_XString for RawFile name; direct pointer cell patched before string payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XScalarFieldContract CompressedLen = new(
        "compressedLen",
        0x04,
        sizeof(int),
        "int32",
        "0x04: compressedLen copied by root Load_Stream; controls buffer length when nonzero.");

    public static readonly XScalarFieldContract Len = new(
        "len",
        0x08,
        sizeof(int),
        "int32",
        "0x08: len copied by root Load_Stream; buffer length is len + 1 when compressedLen is zero.");

    public static readonly XPointerFieldContract BufferPointer = new(
        "buffer",
        0x0C,
        "byte[]",
        XPointerResolutionMode.Direct,
        XPointerSourceSemantics.RequiredInline,
        "0x0C: direct byte buffer pointer; destination cell patched before buffer payload.",
        InlineBlock: XFileBlockType.LARGE);

    public static readonly XStructCodecContract Root = new(
        "RawFileRoot",
        RawFileAsset.SerializedSize,
        [
            NamePointer,
            CompressedLen,
            Len,
            BufferPointer
        ],
        EvidenceText,
        XCodecReadiness.EmitterReady,
        new XCodecRecipe(
            "RawFileRoot",
            [
                XCodecOps.PushBlock(XFileBlockType.TEMP, "Top-level RawFile wrapper pushes TEMP."),
                XCodecOps.Align(4, "RawFile loader aligns the active runtime block to 4 before root Load_Stream."),
                XCodecOps.StreamStruct("RawFileRoot", RawFileAsset.SerializedSize, XFileBlockType.TEMP, 4, "Load_Stream root size 0x10."),
                XCodecOps.PushBlock(XFileBlockType.LARGE, "RawFile name and buffer load under LARGE."),
                XCodecOps.Field(NamePointer),
                XCodecOps.Field(BufferPointer),
                XCodecOps.PopBlock(XFileBlockType.LARGE, "RawFile pops LARGE after name and buffer."),
                XCodecOps.PopBlock(XFileBlockType.TEMP, "RawFile pops TEMP after root and children.")
            ],
            EvidenceText));

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.RawFile,
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
