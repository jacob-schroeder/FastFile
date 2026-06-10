using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.RawFiles;

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x10)]
public class RawFile() : BaseAsset(XAssetType.RawFile)
{
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> NamePtr { get; set; } // Direct

    public int CompressedLen { get; set; }
    public int Len { get; set; }

    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<byte[]> BufferPtr { get; set; } // Direct

    //Exposed
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public byte[] Buffer => BufferPtr is { IsResolved: true, Value: not null }
        ? BufferPtr.Value
        : [];
    public override string? GetDisplayName => Name;
}
