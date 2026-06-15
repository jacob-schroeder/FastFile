using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.RawFiles;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
[XEbootEvidence(
    "0x103ec0",
    "Data/eboot/xasset_loader_findings.txt",
    Detail = "RawFile inner loader: Load_Stream size 0x10; XString +0x00; compressedLen +0x04; len +0x08; buffer pointer +0x0c; buffer length is compressedLen when nonzero else len+1.")]
public class RawFile() : BaseAsset(XAssetType.RawFile)
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct

    [XField(Offset = 0x04)]
    public int CompressedLen { get; set; }

    [XField(Offset = 0x08)]
    public int Len { get; set; }

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        PayloadBlock = XFILE_BLOCK.LARGE,
        Alignment = 1,
        CountMember = nameof(BufferLength))]
    public XPointer<byte[]> BufferPtr { get; set; } // Direct

    //Exposed
    public int BufferLength => CompressedLen != 0 ? CompressedLen : Len + 1;
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public byte[] Buffer => BufferPtr is { IsResolved: true, Value: not null }
        ? BufferPtr.Value
        : [];
    public override string? GetDisplayName => Name;
}
