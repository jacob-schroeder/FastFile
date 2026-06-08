using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.RawFiles;

public class RawFile() : BaseAsset(XAssetType.RawFile)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public int CompressedLen { get; set; }
    public int Len { get; set; }

    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<byte[]> BufferPtr { get; set; }
    public byte[] Buffer => BufferPtr is { IsResolved: true, Result: not null }
        ? BufferPtr.Result
        : [];

    public override string? GetDisplayName => Name;
}
