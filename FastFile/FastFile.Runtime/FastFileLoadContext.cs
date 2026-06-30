using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Database.Streaming;
using FastFile.Models.Assets.Image;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime.Assets;
using FastFile.Runtime.Blocks;
using FastFile.Runtime.Coverage;
using FastFile.Runtime.Diagnostics;
using FastFile.Runtime.Pointers;

namespace FastFile.Runtime;

public sealed class FastFileLoadContext
{
    private int _nextGfxImageStreamIndex;
    private readonly Dictionary<XBlockAddress, GfxImageAsset> _gfxImagesByAddress = new();

    public FastFileLoadContext()
    {
        Blocks.SourceCoverage = SourceCoverage;
        PointerReader = new XFilePointerReader(Blocks);
    }

    public uint SelectedLanguageMask { get; set; }
    public DbHeader? Header { get; set; }
    public byte[]? DecodedZoneBytes { get; set; }

    public GfxImageStreamTable ImageStreams { get; } = new();
    public StreamFileRef CurrentFastFile { get; set; } = new(0, "<current fastfile>", StreamFileKind.CurrentFastFile);

    public BlockStreamState Blocks { get; } = new();
    public SourceCoverageRecorder SourceCoverage { get; } = new();
    public XFilePointerReader PointerReader { get; }
    public PointerResolutionTable Pointers { get; } = new();
    public AssetRegistry Assets { get; } = new();
    public LoadDiagnostics Diagnostics { get; } = new();
    public IReadOnlyDictionary<XBlockAddress, GfxImageAsset> GfxImagesByAddress => _gfxImagesByAddress;

    public int? AllocateGfxImageStreamIndex(bool hasStreamingData)
    {
        return hasStreamingData ? _nextGfxImageStreamIndex++ : null;
    }

    public void RegisterGfxImage(GfxImageAsset? image, XBlockAddress? pointerCellAddress)
    {
        if (image is null)
            return;

        if (image.RuntimeAddress is { } runtimeAddress)
            _gfxImagesByAddress.TryAdd(runtimeAddress, image);

        if (pointerCellAddress is { } cellAddress)
            _gfxImagesByAddress[cellAddress] = image;
    }

    public GfxImageAsset? ResolveGfxImage(XPointerReference pointer)
    {
        if (pointer.PackedAddress is { } packedAddress && _gfxImagesByAddress.TryGetValue(packedAddress, out GfxImageAsset? image))
            return image;

        if (pointer.Type == PointerType.Offset && pointer.ResolutionMode == XPointerResolutionMode.AliasCell)
        {
            if (pointer.PackedAddress is not { } aliasCell)
                return null;

            int raw = Blocks.ReadInt32(aliasCell);
            if (XPointerCodec.GetType(raw) == PointerType.Offset &&
                _gfxImagesByAddress.TryGetValue(XPointerCodec.Decode(raw), out image))
            {
                return image;
            }
        }

        return null;
    }
}
