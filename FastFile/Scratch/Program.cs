using FastFile.Loaders;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.RawFile;
using FastFile.Models.Assets.StringTable;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Database;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;

namespace Scratch;

class Program
{
    const string scratchRoot = "/Users/jacob/Repositories/FastFile/FastFile/Scratch";
    const string root = "/Users/jacob/Repositories/FastFile/Data/official_ff";
    const string patch_mp = $"{root}/patch_mp.ff";
    const string common_mp = $"{root}/common_mp.ff";
    const string mp_boneyard_load = $"{root}/mp_boneyard_load.ff";

    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : patch_mp;
        
        byte[] buffer = File.ReadAllBytes(path);
        int len = buffer.Length;

        var context = new FastFileLoadContext();
        var loader = new FastFileLoader();
        FastFile.Models.Database.DbFileLoad.FastFileLoad session;
        try
        {
            session = loader.Load(buffer, len, context);
            WriteAssetReadDebug(path, context, null);
        }
        catch (Exception ex)
        {
            string debugPath = WriteAssetReadDebug(path, context, ex);
            TryDumpPartialBlocks(path, context);
            Console.WriteLine($"Asset read debug written: {debugPath}");
            throw;
        }

        var ffheader = session.Header;
        var xheader = session.XFileHeader;
        var assets = session.XAssetList;
        
        Console.WriteLine($"Fastfile: {path}");
        Console.WriteLine($"Magic: {ffheader.Magic}");
        Console.WriteLine($"Version: {ffheader.Version}");
        Console.WriteLine($"Allow update: {ffheader.AllowOnlineUpdate}");
        Console.WriteLine($"Created: {ffheader.FileCreationTime:MM/dd/yyyy HH:mm:ss tt}");
        Console.WriteLine($"Language: {(Language)ffheader.LanguageMask}");
        Console.WriteLine($"Entry Count: {ffheader.EntryCount}");
        Console.WriteLine($"File Size: {ffheader.FileSize}");
        Console.WriteLine($"Max File Size: {ffheader.MaxFileSize}");
        Console.WriteLine("=====================");
        
        Console.WriteLine($"XFile Size: {xheader.Size}");
        Console.WriteLine($"XFile Ext Size: {xheader.ExternalSize}");
        for(int i = 0; i < xheader.BlockSize.Length; i++)
        {
            int size = xheader.BlockSize[i];
            Console.WriteLine($"{(XFileBlockType)i} Block Size: {size}");
        }

        Console.WriteLine("=====================");
        Console.WriteLine($"XAssetList Offset: 0x{assets.SerializedOffset:X}");
        Console.WriteLine($"Script Strings: {assets.ScriptStringCount} ptr={PointerText(assets.ScriptStringsPointer)}");
        foreach (var scriptString in assets.ScriptStrings.Take(16))
            Console.WriteLine($"  script[{scriptString.Index,2}] ptr={PointerText(scriptString.Pointer)} value={scriptString.Value ?? "<null>"}");

        Console.WriteLine($"Assets: {assets.AssetCount} ptr={PointerText(assets.AssetsPointer)}");
        foreach (var asset in assets.Assets.Take(16))
            Console.WriteLine($"  asset[{asset.Index,3}] offset=0x{asset.SerializedOffset:X} type={asset.Type} ptr={PointerText(asset.AssetPointer)}");

        Console.WriteLine("=====================");
        Console.WriteLine($"Dispatched Supported Assets: {session.LoadedAssets.Count(x => x.IsLoaded)}");
        foreach (var result in session.LoadedAssets)
        {
            Console.WriteLine($"  dispatch[{result.Index,3}] source=0x{result.SourceOffset:X}..0x{result.EndSourceOffset:X} type={result.Type} ptr={PointerText(result.AssetPointer)} loaded={result.IsLoaded}");
            if (result.StopReason is not null)
                Console.WriteLine($"    stop: {result.StopReason}");

            if (result.Asset is MaterialTechniqueSetAsset techset)
                PrintTechniqueSet(techset);
            else if (result.Asset is MenuFileAsset menuFile)
                PrintMenuFile(menuFile);
            else if (result.Asset is StringTableAsset stringTable)
                PrintStringTable(stringTable);
            else if (result.Asset is StructuredDataDefSetAsset structuredData)
                PrintStructuredData(structuredData);
            else if (result.Asset is RawFileAsset rawFile)
                PrintRawFile(rawFile);
        }

        string dumpDirectory = Path.Combine(scratchRoot, "block_dumps", Path.GetFileNameWithoutExtension(path));
        context.Blocks.DumpToDirectory(dumpDirectory);
        string report = BuildBlockDumpReport(session, context);
        File.WriteAllText(Path.Combine(dumpDirectory, "block_report.txt"), report);
        WriteMenuWindowReport(session, path);
        Console.WriteLine("=====================");
        Console.WriteLine($"Block streams dumped: {dumpDirectory}");
    }

    private static string WriteAssetReadDebug(
        string path,
        FastFileLoadContext context,
        Exception? exception)
    {
        string directory = Path.Combine(scratchRoot, "debug-output");
        Directory.CreateDirectory(directory);
        string outputPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(path)}.asset-read-debug.log");

        using var writer = new StreamWriter(outputPath);
        writer.WriteLine($"Fastfile: {path}");
        writer.WriteLine($"Final block positions: {context.Blocks.DescribePositions()}");

        if (exception is not null)
        {
            writer.WriteLine();
            writer.WriteLine("Exception:");
            for (Exception? current = exception; current is not null; current = current.InnerException)
                writer.WriteLine($"{current.GetType().FullName}: {current.Message}");
        }

        if (context.Diagnostics.Warnings.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Warnings:");
            foreach (string warning in context.Diagnostics.Warnings)
                writer.WriteLine(warning);
        }

        writer.WriteLine();
        writer.WriteLine("Asset/Field Trace:");
        foreach (string line in context.Diagnostics.TraceLines)
            writer.WriteLine(line);

        return outputPath;
    }

    private static void TryDumpPartialBlocks(
        string path,
        FastFileLoadContext context)
    {
        try
        {
            string dumpDirectory = Path.Combine(scratchRoot, "block_dumps", $"{Path.GetFileNameWithoutExtension(path)}_partial");
            context.Blocks.DumpToDirectory(dumpDirectory);
        }
        catch
        {
        }
    }

    private static void PrintTechniqueSet(MaterialTechniqueSetAsset techset)
    {
        MaterialTechniqueSlot[] loadedSlots = techset.TechniqueSlots
            .Where(x => x.Technique is not null)
            .ToArray();

        Console.WriteLine($"    techset @0x{techset.Offset:X}: name={techset.Name ?? "<external/alias>"} worldVertFormat={techset.WorldVertexFormat}");
        Console.WriteLine($"    technique slots: {loadedSlots.Length}/{techset.TechniqueSlots.Count} inline");

        foreach (MaterialTechniqueSlot slot in techset.TechniqueSlots.Where(x => x.Pointer.Raw != 0).Take(8))
            Console.WriteLine($"      slot[{slot.Index,2}] ref={PointerText(slot.Pointer)} loaded={slot.Technique is not null}");

        foreach (MaterialTechniqueSlot slot in loadedSlots.Take(8))
        {
            MaterialTechniqueAsset technique = slot.Technique!;
            Console.WriteLine($"      slot[{slot.Index,2}] technique @0x{technique.Offset:X} name={technique.Name ?? "<external/alias>"} flags=0x{technique.Flags:X4} passes={technique.PassCount}");

            foreach (MaterialPassAsset pass in technique.Passes.Take(3))
            {
                int argCount = pass.PerPrimArgCount + pass.PerObjArgCount + pass.StableArgCount;
                Console.WriteLine($"        pass @0x{pass.Offset:X}: vd={PointerText(pass.VertexDeclPointer)} vs={ShaderName(pass.VertexShader)} ps={ShaderName(pass.PixelShader)} args={argCount}/{pass.Args.Count}");
            }
        }
    }

    private static void PrintMenuFile(MenuFileAsset menuFile)
    {
        Console.WriteLine($"    menufile @0x{menuFile.Offset:X}: name={menuFile.Name ?? "<external/alias>"} menus={menuFile.MenuCount} ptr={PointerText(menuFile.MenusPointer)}");

        foreach (MenuDefReference menuRef in menuFile.Menus.Take(8))
        {
            Console.WriteLine($"      menu[{menuRef.Index,2}] ref={PointerText(menuRef.Pointer)} loaded={menuRef.Menu is not null}");
            if (menuRef.Menu is { } menu)
            {
                int loadedItems = menu.Items.Count(x => x.Item is not null);
                Console.WriteLine($"        menudef root @0x{menu.Offset:X}: windowName={PointerText(menu.Window.NamePointer)} font={PointerText(menu.FontPointer)} itemCount={menu.ItemCount} loadedItems={loadedItems}/{menu.Items.Count} items={PointerText(menu.ItemsPointer)} expressionData={PointerText(menu.ExpressionData)}");

                foreach (ItemDefReference itemRef in menu.Items.Where(x => x.Item is not null).Take(8))
                {
                    ItemDefAsset item = itemRef.Item!;
                    Console.WriteLine($"          item[{itemRef.Index,2}] @0x{item.Offset:X}: type={item.Type} dataType={item.DataType} typeData={PointerText(item.TypeData.RawPointer)} floats={item.LoadedFloatExpressions.Count}/{item.FloatExpressionCount} visible={PointerText(item.VisibleExpression)} disabled={PointerText(item.DisabledExpression)} material={PointerText(item.MaterialExpression)}");
                }
            }
        }
    }

    private static void PrintStringTable(StringTableAsset stringTable)
    {
        Console.WriteLine($"    stringtable @0x{stringTable.Offset:X}: name={stringTable.Name ?? "<external/alias>"} rows={stringTable.RowCount} cols={stringTable.ColumnCount} cells={stringTable.Cells.Count}/{stringTable.CellCount}");
    }

    private static void PrintStructuredData(StructuredDataDefSetAsset structuredData)
    {
        Console.WriteLine($"    structuredDataDefSet @0x{structuredData.Offset:X}: name={structuredData.Name ?? "<external/alias>"} defs={structuredData.Defs.Count}/{structuredData.DefCount}");
    }

    private static void PrintRawFile(RawFileAsset rawFile)
    {
        Console.WriteLine($"    rawfile @0x{rawFile.Offset:X}: name={rawFile.Name ?? "<external/alias>"} len={rawFile.Len} compressedLen={rawFile.CompressedLen} buffer={rawFile.Buffer?.Length ?? 0}/{rawFile.BufferLength}");
    }

    private static void WriteMenuWindowReport(
        FastFile.Models.Database.DbFileLoad.FastFileLoad session,
        string path)
    {
        string outputPath = Path.Combine(
            scratchRoot,
            "debug-output",
            $"{Path.GetFileNameWithoutExtension(path)}.menu-window-root-values.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var lines = new List<string>
        {
            "assetIndex,menuFileName,menuIndex,menuOffset,itemCount,ownerDraw,ownerDrawFlags,staticFlags,dynamic0,dynamic1,dynamic2,dynamic3,windowNamePointer,backgroundPointer"
        };

        foreach (XAssetLoadResult result in session.LoadedAssets)
        {
            if (result.Asset is not MenuFileAsset menuFile)
                continue;

            foreach (MenuDefReference menuRef in menuFile.Menus)
            {
                if (menuRef.Menu is null)
                    continue;

                WindowDef window = menuRef.Menu.Window;
                string dynamics = string.Join(",", window.DynamicFlags.Select(x => $"0x{(int)x:X8}"));
                lines.Add(string.Join(",",
                    result.Index.ToString(),
                    Csv(menuFile.Name ?? string.Empty),
                    menuRef.Index.ToString(),
                    $"0x{menuRef.Menu.Offset:X}",
                    menuRef.Menu.ItemCount.ToString(),
                    $"0x{(int)window.OwnerDraw:X8}",
                    $"0x{window.OwnerDrawFlags:X8}",
                    $"0x{(int)window.StaticFlags:X8}",
                    dynamics,
                    PointerText(window.NamePointer),
                    PointerText(window.Background)));
            }
        }

        File.WriteAllLines(outputPath, lines);
        Console.WriteLine($"Menu root window values: {outputPath}");
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string ShaderName(MaterialShaderAsset? shader)
    {
        if (shader is null)
            return "<null/alias>";

        string name = shader.Name ?? "<external/alias>";
        string bytes = shader.Data is null ? "no-inline-bytes" : $"0x{shader.Data.Length:X} bytes";
        return $"{name} {bytes}";
    }

    private static string PointerText(XPointerReference pointer)
    {
        string address = pointer.PackedAddress is { } packedAddress
            ? $" {pointer.OffsetMode}->{packedAddress}"
            : string.Empty;

        return $"0x{pointer.Raw:X8} {pointer.Type}{address}";
    }

    private static string PointerText<T>(XPointer<T> pointer)
    {
        string address = pointer.PackedAddress is { } packedAddress
            ? $" {pointer.ResolutionMode}->{packedAddress}"
            : string.Empty;

        return $"0x{pointer.Raw:X8} {pointer.Type}{address}";
    }

    private static string BuildBlockDumpReport(
        FastFile.Models.Database.DbFileLoad.FastFileLoad session,
        FastFileLoadContext context)
    {
        var lines = new List<string>
        {
            "Block stream dump report",
            $"XAssetList source: 0x{session.XAssetList.SerializedOffset:X}",
            $"Loaded dispatch results: {session.LoadedAssets.Count}"
        };

        foreach ((XFileBlockType block, byte[] bytes) in context.Blocks.Snapshot())
            lines.Add($"{block}: 0x{bytes.Length:X} bytes");

        if (session.LoadedAssets.Count > 0 &&
            session.XAssetList.Assets.Count > 0 &&
            session.LoadedAssets[0].Type == XAssetType.Techset)
        {
            AddPatchMpBoundaryChecks(lines, session, context);
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static void AddPatchMpBoundaryChecks(
        List<string> lines,
        FastFile.Models.Database.DbFileLoad.FastFileLoad session,
        FastFileLoadContext context)
    {
        byte[] temp = context.Blocks.GetBytes(XFileBlockType.TEMP);
        byte[] large = context.Blocks.GetBytes(XFileBlockType.LARGE);
        byte[] zone = session.ZoneBytes;

        int rootSource = session.XAssetList.SerializedOffset;
        int scriptSource = rootSource + 0x10;
        int assetTableSource = session.XAssetList.Assets[0].SerializedOffset;
        int scriptLength = assetTableSource - scriptSource;
        int assetTableLength = checked(session.XAssetList.AssetCount * 0x08);
        int assetTableLarge = Align(scriptLength, 4);
        int techsetRootLarge = assetTableLarge + assetTableLength;
        XAssetLoadResult techsetResult = session.LoadedAssets[0];
        int techsetRootLength = Math.Min(0x9c, techsetResult.EndSourceOffset - techsetResult.SourceOffset);
        int techsetNameSource = techsetResult.SourceOffset + techsetRootLength;
        int techsetNameLength = techsetResult.EndSourceOffset - techsetNameSource;
        int techsetNameLarge = techsetRootLarge + techsetRootLength;

        lines.Add("");
        lines.Add("patch_mp proven-boundary comparisons:");
        AddCompare(lines, "TEMP XAssetList root", temp, 0, zone, rootSource, 0x10);
        AddCompare(lines, "LARGE scriptString payload", large, 0, zone, scriptSource, scriptLength);
        AddCompare(lines, "LARGE asset table", large, assetTableLarge, zone, assetTableSource, assetTableLength);
        AddCompare(lines, "LARGE first Techset root", large, techsetRootLarge, zone, techsetResult.SourceOffset, techsetRootLength);
        AddCompare(lines, "LARGE first Techset name/children", large, techsetNameLarge, zone, techsetNameSource, techsetNameLength);

        if (session.LoadedAssets.Count > 1 &&
            session.LoadedAssets[1].Asset is MenuFileAsset menuFile)
        {
            XAssetLoadResult menuResult = session.LoadedAssets[1];
            int menuRootLarge = Align(techsetNameLarge + techsetNameLength, 4);
            int menuNameSource = menuResult.SourceOffset + 0x0c;
            int menuNameLength = menuFile.Name is null ? 0 : menuFile.Name.Length + 1;
            int menuTableSource = menuNameSource + menuNameLength;
            int menuTableLength = checked(menuFile.MenuCount * sizeof(int));
            int menuDefSource = menuTableSource + menuTableLength;
            int menuNameLarge = menuRootLarge + 0x0c;
            int menuTableLarge = menuNameLarge + menuNameLength;
            int menuDefLarge = Align(menuTableLarge + menuTableLength, 4);

            AddCompare(lines, "LARGE first MenuFile root", large, menuRootLarge, zone, menuResult.SourceOffset, 0x0c);
            AddCompare(lines, "LARGE first MenuFile name", large, menuNameLarge, zone, menuNameSource, menuNameLength);
            AddCompare(lines, "LARGE first MenuFile MenuDef pointer table", large, menuTableLarge, zone, menuTableSource, menuTableLength);
            AddCompare(lines, "LARGE first MenuDef root", large, menuDefLarge, zone, menuDefSource, MenuDefAsset.SerializedSize);
        }

        XAssetLoadResult lastResult = session.LoadedAssets.Last();
        lines.Add($"Dispatch stopped at source: 0x{lastResult.EndSourceOffset:X} ({lastResult.Type})");
        if (lastResult.StopReason is not null)
            lines.Add($"Stop reason: {lastResult.StopReason}");
    }

    private static void AddCompare(
        List<string> lines,
        string label,
        byte[] block,
        int blockOffset,
        byte[] zone,
        int zoneOffset,
        int length)
    {
        bool match = blockOffset >= 0 &&
                     zoneOffset >= 0 &&
                     blockOffset + length <= block.Length &&
                     zoneOffset + length <= zone.Length &&
                     block.AsSpan(blockOffset, length).SequenceEqual(zone.AsSpan(zoneOffset, length));

        lines.Add($"{label}: block 0x{blockOffset:X}..0x{blockOffset + length:X} vs zone 0x{zoneOffset:X}..0x{zoneOffset + length:X} => {(match ? "MATCH" : "MISMATCH")}");
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }
}
