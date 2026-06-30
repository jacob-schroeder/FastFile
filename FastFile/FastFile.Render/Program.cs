using FastFile.Loaders;
using FastFile.Models.Assets.ColMap;
using FastFile.Models.Assets.GfxMap;
using FastFile.Runtime;
using FastFile.Render.Export;

namespace FastFile.Render;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            RenderOptions options = RenderOptionsParser.Parse(args);
            byte[] buffer = File.ReadAllBytes(options.InputPath);
            var context = new FastFileLoadContext();
            var session = new FastFileLoader().Load(buffer, buffer.Length, context);

            GfxWorldAsset? gfxMap = session.LoadedAssets
                .Select(x => x.Asset)
                .OfType<GfxWorldAsset>()
                .SingleOrDefault();

            ClipMapAsset? colMap = session.LoadedAssets
                .Select(x => x.Asset)
                .OfType<ClipMapAsset>()
                .SingleOrDefault();

            Directory.CreateDirectory(options.OutputDirectory);
            var assetLookup = new RenderAssetLookup(context.Blocks, session);
            var imageStreams = new GfxImageStreamResolver(session.Header, options.InputPath);
            var exporter = new MapRenderExporter(options, assetLookup, imageStreams);
            MapRenderSummary summary = exporter.Export(options.InputPath, gfxMap, colMap);
            PrintSummary(summary);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintSummary(MapRenderSummary summary)
    {
        Console.WriteLine($"Input: {summary.InputPath}");
        Console.WriteLine($"Output directory: {summary.OutputDirectory}");
        Console.WriteLine();

        foreach (string path in summary.WrittenFiles)
            Console.WriteLine($"Wrote: {path}");

        Console.WriteLine();
        Console.WriteLine($"GfxMap: vertices={summary.GfxVertexCount} indices={summary.GfxIndexCount} surfaces={summary.GfxSurfaceCount}");
        Console.WriteLine($"GfxMap layer bytes: {summary.GfxVertexLayerByteCount}");
        Console.WriteLine($"Materials/images: materials={summary.RuntimeMaterialCount} images={summary.RuntimeImageCount} decodedTextures={summary.DecodedTextureCount} skippedTextures={summary.TextureDecodeSkippedCount}");
        if (!string.IsNullOrWhiteSpace(summary.TexCoordStatus))
            Console.WriteLine($"Texcoords: {summary.TexCoordStatus}");
        if (!string.IsNullOrWhiteSpace(summary.TextureOutputDirectory))
            Console.WriteLine($"Texture PNGs: {summary.TextureOutputDirectory}");
        Console.WriteLine($"ColMap: verts={summary.CollisionVertexCount} triIndices={summary.CollisionIndexCount} staticModels={summary.StaticModelCount}");
        Console.WriteLine($"MapEnts chars: {summary.MapEntsCharCount}");

        if (summary.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Warnings:");
            foreach (string warning in summary.Warnings)
                Console.WriteLine($"  {warning}");
        }
    }
}

internal sealed record RenderOptions(
    string InputPath,
    string OutputDirectory,
    bool RawCoordinates);

internal static class RenderOptionsParser
{
    public static RenderOptions Parse(string[] args)
    {
        string inputPath = ProgramDefaultInput.Value;
        string? outputDirectory = null;
        bool rawCoordinates = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "--out":
                case "-o":
                    if (++i >= args.Length)
                        throw new ArgumentException($"{arg} requires a directory.");
                    outputDirectory = args[i];
                    break;

                case "--raw-coordinates":
                    rawCoordinates = true;
                    break;

                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;

                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Unknown option '{arg}'.");
                    inputPath = arg;
                    break;
            }
        }

        inputPath = Path.GetFullPath(inputPath);
        outputDirectory ??= Path.Combine(
            Directory.GetCurrentDirectory(),
            "render-output",
            Path.GetFileNameWithoutExtension(inputPath));

        return new RenderOptions(inputPath, Path.GetFullPath(outputDirectory), rawCoordinates);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project FastFile/FastFile.Render/FastFile.Render.csproj -- [fastfile.ff] [--out directory] [--raw-coordinates]");
        Console.WriteLine();
        Console.WriteLine("Outputs:");
        Console.WriteLine("  <map>.gfx.glb                 GfxMap render-world mesh");
        Console.WriteLine("  <map>.collision-debug.glb     ColMap collision mesh and debug boxes");
        Console.WriteLine("  <map>.static-xmodels.csv      ColMap static xmodel placement table");
        Console.WriteLine("  <map>.mapents.txt             MapEnts entity string");
        Console.WriteLine("  <map>.stages.csv              MapEnts stage table");
    }
}

internal static class ProgramDefaultInput
{
    public const string Value = "/Users/jacob/Repositories/FastFile/Data/official_ff/mp_boneyard.ff";
}
