using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Database.Streaming;
using FastFile.Runtime.Assets;
using FastFile.Runtime.Blocks;
using FastFile.Runtime.Diagnostics;
using FastFile.Runtime.Pointers;

namespace FastFile.Runtime;

public sealed class FastFileLoadContext
{
    public uint SelectedLanguageMask { get; set; }
    public DbHeader? Header { get; set; }

    public GfxImageStreamTable ImageStreams { get; } = new();
    public StreamFileRef CurrentFastFile { get; set; } = new(0, "<current fastfile>", StreamFileKind.CurrentFastFile);

    public BlockStreamState Blocks { get; } = new();
    public XFilePointerReader PointerReader { get; } = new();
    public PointerResolutionTable Pointers { get; } = new();
    public AssetRegistry Assets { get; } = new();
    public LoadDiagnostics Diagnostics { get; } = new();
}
