using FastFile.Loaders;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.RawFile;
using FastFile.Models.Assets.StringTable;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Database;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.Coverage;
using FastFile.Runtime.Pointers;

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
            WritePointerValidationReport(path, context);
            WriteSourceCoverageReport(path, context, assertComplete: true);
        }
        catch (Exception ex)
        {
            string debugPath = WriteAssetReadDebug(path, context, ex);
            WriteDecodedZoneDump(path, context);
            WritePointerValidationReport(path, context);
            WriteSourceCoverageReport(path, context, assertComplete: false);
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
        Console.WriteLine(
            $"Pointer Validations: directTargets={context.PointerReader.DirectTargetValidationCount} " +
            $"aliasCells={context.PointerReader.AliasCellValidationCount} " +
            $"resolvedAliasTargets={context.PointerReader.ResolvedAliasTargetValidationCount} " +
            $"typeProvenTargets={context.PointerReader.TypeProvenTargetValidationCount} " +
            $"untypedTargets={context.PointerReader.UntypedTargetValidationCount} " +
            $"issues={context.PointerReader.ValidationIssues.Count} " +
            $"stableIssues={StablePointerIssueCount(context)} " +
            $"stableUntypedIssues={StableUntypedPointerIssueCount(context)} " +
            $"nullObjects={context.PointerReader.NullObjectPointerCount} " +
            $"stableNullObjects={StableNullObjectIssueCount(context)} " +
            $"nullAliasTargets={context.PointerReader.NullAliasTargetCount} " +
            $"emptyXStrings={context.PointerReader.EmptyXStringTargetCount}");
        SourceCoverageSummary coverageSummary = context.SourceCoverage.BuildSummary();
        Console.WriteLine(
            $"Source Coverage: covered={coverageSummary.CoveredBytes}/0x{coverageSummary.SourceLength:X} " +
            $"uncovered={coverageSummary.UncoveredBytes} records={coverageSummary.RecordCount} " +
            $"overlaps={coverageSummary.OverlapCount}");

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
            else if (result.Asset is LocalizeAsset localize)
                PrintLocalize(localize);
        }

        string dumpDirectory = Path.Combine(scratchRoot, "block_dumps", Path.GetFileNameWithoutExtension(path));
        context.Blocks.DumpToDirectory(dumpDirectory);
        string report = BuildBlockDumpReport(session, context);
        File.WriteAllText(Path.Combine(dumpDirectory, "block_report.txt"), report);
        WriteScriptStringTableReport(session, path);
        WriteMenuWindowReport(session, path);
        WriteMenuPointerNullReport(session, path);
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
        writer.WriteLine(
            $"Pointer validations: directTargets={context.PointerReader.DirectTargetValidationCount}, " +
            $"aliasCells={context.PointerReader.AliasCellValidationCount}, " +
            $"resolvedAliasTargets={context.PointerReader.ResolvedAliasTargetValidationCount}, " +
            $"typeProvenTargets={context.PointerReader.TypeProvenTargetValidationCount}, " +
            $"untypedTargets={context.PointerReader.UntypedTargetValidationCount}, " +
            $"issues={context.PointerReader.ValidationIssues.Count}, " +
            $"stableIssues={StablePointerIssueCount(context)}, " +
            $"stableUntypedIssues={StableUntypedPointerIssueCount(context)}, " +
            $"nullObjects={context.PointerReader.NullObjectPointerCount}, " +
            $"stableNullObjects={StableNullObjectIssueCount(context)}, " +
            $"nullAliasTargets={context.PointerReader.NullAliasTargetCount}, " +
            $"emptyXStrings={context.PointerReader.EmptyXStringTargetCount}");

        if (context.PointerReader.ValidationIssues.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("Pointer Validation Issues (non-TEMP first):");
            foreach (PointerValidationRecord issue in context.PointerReader.ValidationIssues.OrderBy(x => x.IsTempRelated).Take(250))
                writer.WriteLine($"{issue.Kind}: raw=0x{issue.RawPointer:X8} mode={issue.ResolutionMode} target={issue.TargetName} cell={AddressText(issue.PointerCellAddress)} packed={AddressText(issue.PackedAddress)} resolved={AddressText(issue.ResolvedTargetAddress)} message={issue.Message}");

            if (context.PointerReader.ValidationIssues.Count > 250)
                writer.WriteLine($"... {context.PointerReader.ValidationIssues.Count - 250} additional pointer validation issue(s); see pointer-validation CSV.");
        }

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

    private static string? WriteDecodedZoneDump(
        string path,
        FastFileLoadContext context)
    {
        if (context.DecodedZoneBytes is null)
            return null;

        string directory = Path.Combine(scratchRoot, "debug-output");
        Directory.CreateDirectory(directory);
        string outputPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(path)}.xfile-zone");
        File.WriteAllBytes(outputPath, context.DecodedZoneBytes);
        return outputPath;
    }

    private static string WritePointerValidationReport(
        string path,
        FastFileLoadContext context)
    {
        string directory = Path.Combine(scratchRoot, "debug-output");
        Directory.CreateDirectory(directory);
        string outputPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(path)}.pointer-validation.csv");

        using var writer = new StreamWriter(outputPath);
        writer.WriteLine("severity,kind,targetName,targetType,typeProven,tempRelated,rawPointer,pointerType,resolutionMode,pointerCellAddress,packedAddress,resolvedTargetAddress,byteCount,aliasedRaw,pointerCellBytes,resolvedTargetBytes,message");
        foreach (PointerValidationRecord record in context.PointerReader.ValidationRecords)
        {
            writer.WriteLine(string.Join(
                ",",
                Csv(record.Severity),
                Csv(record.Kind),
                Csv(record.TargetName),
                Csv(record.TargetType),
                Csv(record.TypeProven ? "true" : "false"),
                Csv(record.IsTempRelated ? "true" : "false"),
                Csv($"0x{record.RawPointer:X8}"),
                Csv(record.PointerType.ToString()),
                Csv(record.ResolutionMode),
                Csv(AddressText(record.PointerCellAddress)),
                Csv(AddressText(record.PackedAddress)),
                Csv(AddressText(record.ResolvedTargetAddress)),
                Csv(record.ByteCount?.ToString()),
                Csv(record.AliasedRaw.HasValue ? $"0x{record.AliasedRaw.Value:X8}" : null),
                Csv(record.PointerCellBytes),
                Csv(record.ResolvedTargetBytes),
                Csv(record.Message)));
        }

        return outputPath;
    }

    private static string WriteSourceCoverageReport(
        string path,
        FastFileLoadContext context,
        bool assertComplete)
    {
        string directory = Path.Combine(scratchRoot, "debug-output");
        Directory.CreateDirectory(directory);
        string stem = Path.GetFileNameWithoutExtension(path);
        string csvPath = Path.Combine(directory, $"{stem}.source-coverage.csv");
        string summaryPath = Path.Combine(directory, $"{stem}.source-coverage.summary.txt");

        SourceCoverageSummary summary = context.SourceCoverage.BuildSummary();

        using (var writer = new StreamWriter(csvPath))
        {
            writer.WriteLine("sourceStart,sourceEndExclusive,length,kind,ownerPath,memberName,callerName,destinationBlock,destinationOffset");
            foreach (SourceCoverageRecord record in context.SourceCoverage.Records.OrderBy(x => x.SourceStart).ThenBy(x => x.SourceEndExclusive))
            {
                writer.WriteLine(string.Join(
                    ",",
                    Csv($"0x{record.SourceStart:X}"),
                    Csv($"0x{record.SourceEndExclusive:X}"),
                    Csv(record.Length.ToString()),
                    Csv(record.Kind),
                    Csv(record.OwnerPath),
                    Csv(record.MemberName),
                    Csv(record.CallerName),
                    Csv(record.DestinationBlock?.ToString()),
                    Csv(record.DestinationOffset.HasValue ? $"0x{record.DestinationOffset.Value:X}" : null)));
            }
        }

        using (var writer = new StreamWriter(summaryPath))
        {
            writer.WriteLine($"Fastfile: {path}");
            writer.WriteLine($"Zone source length: 0x{summary.SourceLength:X} ({summary.SourceLength})");
            writer.WriteLine($"Covered bytes: 0x{summary.CoveredBytes:X} ({summary.CoveredBytes})");
            writer.WriteLine($"Uncovered bytes: 0x{summary.UncoveredBytes:X} ({summary.UncoveredBytes})");
            writer.WriteLine($"Coverage records: {summary.RecordCount}");
            writer.WriteLine($"Overlapping records: {summary.OverlapCount}");
            writer.WriteLine($"Overlapped bytes: 0x{summary.OverlappedBytes:X} ({summary.OverlappedBytes})");
            writer.WriteLine($"Complete: {summary.IsComplete}");

            if (summary.UncoveredRanges.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine("Uncovered ranges:");
                foreach (SourceCoverageGap gap in summary.UncoveredRanges)
                    writer.WriteLine($"0x{gap.SourceStart:X}..0x{gap.SourceEndExclusive:X} len=0x{gap.Length:X}");
            }

            writer.WriteLine();
            writer.WriteLine("Coverage kinds:");
            foreach (var group in context.SourceCoverage.Records.GroupBy(x => x.Kind).OrderBy(x => x.Key))
                writer.WriteLine($"{group.Key}: records={group.Count()} bytes=0x{group.Sum(x => (long)x.Length):X}");
        }

        Console.WriteLine($"Source coverage CSV: {csvPath}");
        Console.WriteLine($"Source coverage summary: {summaryPath}");

        if (assertComplete && !summary.IsComplete)
            throw new InvalidDataException($"Source coverage proof failed: 0x{summary.UncoveredBytes:X} uncovered byte(s). See {summaryPath}");

        return summaryPath;
    }

    private static int StablePointerIssueCount(FastFileLoadContext context)
    {
        return context.PointerReader.ValidationIssues.Count(x => !x.IsTempRelated);
    }

    private static int StableUntypedPointerIssueCount(FastFileLoadContext context)
    {
        return context.PointerReader.ValidationIssues.Count(x =>
            !x.IsTempRelated &&
            !x.TypeProven &&
            x.Kind != "NullObjectPointer");
    }

    private static int StableNullObjectIssueCount(FastFileLoadContext context)
    {
        return context.PointerReader.ValidationIssues.Count(x =>
            !x.IsTempRelated &&
            x.Kind == "NullObjectPointer");
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
                    Console.WriteLine($"          item[{itemRef.Index,2}] @0x{item.Offset:X}: type={item.Type} dataType={item.DataType} typeData={TypeDataPointerText(item.TypeData)} floats={item.LoadedFloatExpressions.Count}/{item.FloatExpressionCount} visible={PointerText(item.VisibleExpression)} disabled={PointerText(item.DisabledExpression)} material={PointerText(item.MaterialExpression)}");
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

    private static void PrintLocalize(LocalizeAsset localize)
    {
        Console.WriteLine($"    localize @0x{localize.Offset:X}: name={localize.Name ?? "<external/alias>"} value={localize.Value ?? "<null>"}");
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

    private static void WriteMenuPointerNullReport(
        FastFile.Models.Database.DbFileLoad.FastFileLoad session,
        string path)
    {
        var stats = new Dictionary<string, MenuPointerFieldStat>();
        var seenEventSets = new HashSet<MenuEventHandlerSet>(ReferenceEqualityComparer.Instance);
        var seenKeyHandlers = new HashSet<ItemKeyHandler>(ReferenceEqualityComparer.Instance);
        var seenStatements = new HashSet<Statement>(ReferenceEqualityComparer.Instance);
        var seenSupportData = new HashSet<ExpressionSupportingData>(ReferenceEqualityComparer.Instance);

        foreach (XAssetLoadResult result in session.LoadedAssets)
        {
            if (result.Asset is not MenuFileAsset menuFile)
                continue;

            string menuFileName = menuFile.Name ?? $"asset[{result.Index}]";
            ObservePointer(stats, "MenuFile.MenusPointer", menuFile.MenusPointer, menuFileName, "required menufile menu pointer table");

            foreach (MenuDefReference menuRef in menuFile.Menus)
            {
                string menuContext = $"{menuFileName} menu[{menuRef.Index}]";
                ObservePointer(stats, "MenuFile.MenuDefRef", menuRef.Pointer, menuContext, "menufile menu array entry");
                if (menuRef.Menu is { } menu)
                    AnalyzeMenuPointers(stats, menu, menuContext, seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
            }
        }

        string outputPath = Path.Combine(
            scratchRoot,
            "debug-output",
            $"{Path.GetFileNameWithoutExtension(path)}.menu-pointer-null-fields.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var lines = new List<string>
        {
            "field,targetType,resolutionMode,classification,nullCount,nonNullCount,total,itemTypes,nullItemTypes,nonNullItemTypes,samples"
        };

        foreach (MenuPointerFieldStat stat in stats.Values
                     .OrderByDescending(x => x.NullCount)
                     .ThenBy(x => x.Field)
                     .ThenBy(x => x.TargetType))
        {
            lines.Add(string.Join(
                ",",
                Csv(stat.Field),
                Csv(stat.TargetType),
                Csv(stat.ResolutionMode),
                Csv(stat.Classification),
                stat.NullCount.ToString(),
                stat.NonNullCount.ToString(),
                (stat.NullCount + stat.NonNullCount).ToString(),
                Csv(string.Join("|", stat.ItemTypes.OrderBy(x => x))),
                Csv(FormatCountMap(stat.NullItemTypes)),
                Csv(FormatCountMap(stat.NonNullItemTypes)),
                Csv(string.Join(" || ", stat.Samples))));
        }

        File.WriteAllLines(outputPath, lines);
        Console.WriteLine($"Menu pointer null fields: {outputPath}");
    }

    private static void AnalyzeMenuPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        MenuDefAsset menu,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        string menuContext = $"{context} {menu.Window.Name ?? "<unnamed-menu>"}";

        ObservePointer(stats, "MenuDef.Window.Background", menu.Window.Background, $"{menuContext} style={menu.Window.Style}", "optional window material; runtime consumes for shader/fullscreen background paths");
        ObservePointer(stats, "MenuDef.OnOpen", menu.OnOpen, menuContext, "optional event handler set; PS3 0x10c4c0 permits null");
        ObservePointer(stats, "MenuDef.OnCloseRequest", menu.OnCloseRequest, menuContext, "optional event handler set; PS3 0x10c4c0 permits null");
        ObservePointer(stats, "MenuDef.OnClose", menu.OnClose, menuContext, "optional event handler set; PS3 0x10c4c0 permits null");
        ObservePointer(stats, "MenuDef.OnEsc", menu.OnEsc, menuContext, "optional event handler set; PS3 0x10c4c0 permits null");
        ObservePointer(stats, "MenuDef.ExecKeys", menu.ExecKeys, menuContext, "optional key handler chain; PS3 0x10c540/0x10c5d8 permits null");
        ObservePointer(stats, "MenuDef.VisibleExpression", menu.VisibleExpression, menuContext, "optional statement expression; PS3 0x10bb88 permits null");
        ObservePointer(stats, "MenuDef.RectXExpression", menu.RectXExpression, menuContext, "optional statement expression; PS3 0x10bb88 permits null");
        ObservePointer(stats, "MenuDef.RectYExpression", menu.RectYExpression, menuContext, "optional statement expression; PS3 0x10bb88 permits null");
        ObservePointer(stats, "MenuDef.RectWExpression", menu.RectWExpression, menuContext, "optional statement expression; PS3 0x10bb88 permits null");
        ObservePointer(stats, "MenuDef.RectHExpression", menu.RectHExpression, menuContext, "optional statement expression; PS3 0x10bb88 permits null");
        ObservePointer(stats, "MenuDef.ItemsPointer", menu.ItemsPointer, menuContext, "required item pointer table when itemCount > 0");
        ObservePointer(stats, "MenuDef.ExpressionData", menu.ExpressionData, menuContext, "optional expression support data; PS3 0x10b968 permits null");

        AnalyzeEventSetPointers(stats, menu.OnOpenSet, $"{menuContext}.OnOpen", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, menu.OnCloseRequestSet, $"{menuContext}.OnCloseRequest", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, menu.OnCloseSet, $"{menuContext}.OnClose", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, menu.OnEscSet, $"{menuContext}.OnEsc", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeKeyHandlerPointers(stats, menu.ExecKeyHandler, $"{menuContext}.ExecKeys", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, menu.VisibleStatement, $"{menuContext}.VisibleExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, menu.RectXStatement, $"{menuContext}.RectXExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, menu.RectYStatement, $"{menuContext}.RectYExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, menu.RectWStatement, $"{menuContext}.RectWExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, menu.RectHStatement, $"{menuContext}.RectHExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeExpressionSupportPointers(stats, menu.ExpressionDataValue, $"{menuContext}.ExpressionData", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);

        foreach (ItemDefReference itemRef in menu.Items)
        {
            string itemContext = $"{menuContext} item[{itemRef.Index}]";
            ObservePointer(stats, "MenuDef.ItemDefRef", itemRef.Pointer, itemContext, "menu item array entry");
            if (itemRef.Item is { } item)
                AnalyzeItemPointers(stats, item, itemContext, seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        }
    }

    private static void AnalyzeItemPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        ItemDefAsset item,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        string itemType = item.Type.ToString();
        string itemContext = $"{context} type={item.Type} dataType={item.DataType} text={item.TextString ?? "<null>"}";

        ObservePointer(stats, "ItemDef.Window.Background", item.Window.Background, $"{itemContext} style={item.Window.Style}", "optional window material; runtime consumes for shader-style backgrounds", itemType);
        ObservePointer(stats, "ItemDef.MouseEnterText", item.MouseEnterText, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.MouseExitText", item.MouseExitText, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.MouseEnter", item.MouseEnter, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.MouseExit", item.MouseExit, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.Action", item.Action, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.Accept", item.Accept, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.OnFocus", item.OnFocus, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.LeaveFocus", item.LeaveFocus, itemContext, "optional event handler set; PS3 0x10c4c0 permits null", itemType);
        ObservePointer(stats, "ItemDef.OnKey", item.OnKey, itemContext, "optional key handler chain; PS3 0x10c540/0x10c5d8 permits null", itemType);
        ObservePointer(stats, "ItemDef.FocusSound", item.FocusSound, itemContext, "optional focus sound alias; null means no focus sound", itemType);
        ObservePointer(stats, "ItemDef.FloatExpressions", item.FloatExpressions, itemContext, "optional float expression array; count gates runtime use", itemType);
        ObservePointer(stats, "ItemDef.VisibleExpression", item.VisibleExpression, itemContext, "optional statement expression; PS3 0x10bb88 permits null", itemType);
        ObservePointer(stats, "ItemDef.DisabledExpression", item.DisabledExpression, itemContext, "optional statement expression; PS3 0x10bb88 permits null", itemType);
        ObservePointer(stats, "ItemDef.TextExpression", item.TextExpression, itemContext, "optional statement expression; PS3 0x10bb88 permits null", itemType);
        ObservePointer(stats, "ItemDef.MaterialExpression", item.MaterialExpression, itemContext, "optional statement expression; PS3 0x10bb88 permits null", itemType);
        ObserveTypeDataPointer(stats, item, itemContext);

        AnalyzeEventSetPointers(stats, item.MouseEnterTextSet, $"{itemContext}.MouseEnterText", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.MouseExitTextSet, $"{itemContext}.MouseExitText", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.MouseEnterSet, $"{itemContext}.MouseEnter", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.MouseExitSet, $"{itemContext}.MouseExit", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.ActionSet, $"{itemContext}.Action", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.AcceptSet, $"{itemContext}.Accept", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.OnFocusSet, $"{itemContext}.OnFocus", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeEventSetPointers(stats, item.LeaveFocusSet, $"{itemContext}.LeaveFocus", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeKeyHandlerPointers(stats, item.OnKeyHandler, $"{itemContext}.OnKey", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, item.VisibleStatement, $"{itemContext}.VisibleExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, item.DisabledStatement, $"{itemContext}.DisabledExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, item.TextStatement, $"{itemContext}.TextExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeStatementPointers(stats, item.MaterialStatement, $"{itemContext}.MaterialExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);

        foreach (ItemFloatExpression expression in item.LoadedFloatExpressions)
            AnalyzeStatementPointers(stats, expression.Statement, $"{itemContext}.FloatExpression[{expression.Target}]", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);

        if (item.ListBox is { } listBox)
        {
            ObservePointer(stats, "ListBoxDef.DoubleClick", listBox.DoubleClick, itemContext, "optional listbox double-click handler; PS3 listbox child edge permits null", itemType);
            ObservePointer(stats, "ListBoxDef.SelectIcon", listBox.SelectIcon, itemContext, "optional listbox select icon material; PS3 listbox child edge permits null", itemType);
            AnalyzeEventSetPointers(stats, listBox.DoubleClickSet, $"{itemContext}.ListBox.DoubleClick", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        }
    }

    private static void ObserveTypeDataPointer(
        Dictionary<string, MenuPointerFieldStat> stats,
        ItemDefAsset item,
        string context)
    {
        string itemType = item.Type.ToString();
        string classification = "typeData union; PS3 0x10e850 branch table controls whether null is valid";
        switch (item.Type)
        {
            case ItemDefType.ListBox:
                if (item.TypeData.ListBox is { } listBox)
                    ObservePointer(stats, "ItemDef.TypeData.ListBox", listBox.ListBoxPointer, context, classification, itemType);
                break;
            case ItemDefType.Multi:
                if (item.TypeData.Multi is { } multi)
                    ObservePointer(stats, "ItemDef.TypeData.Multi", multi.MultiPointer, context, classification, itemType);
                break;
            case ItemDefType.NewsTicker:
                if (item.TypeData.NewsTicker is { } newsTicker)
                    ObservePointer(stats, "ItemDef.TypeData.NewsTicker", newsTicker.NewsTickerPointer, context, classification, itemType);
                break;
            case ItemDefType.TextScroll:
                if (item.TypeData.TextScroll is { } textScroll)
                    ObservePointer(stats, "ItemDef.TypeData.TextScroll", textScroll.TextScrollPointer, context, classification, itemType);
                break;
            default:
                if (ItemUsesEditFieldData(item.Type) && item.TypeData.EditField is { } editField)
                    ObservePointer(stats, "ItemDef.TypeData.EditField", editField.EditFieldPointer, context, classification, itemType);
                break;
        }
    }

    private static string TypeDataPointerText(ItemDefData typeData)
    {
        return typeData.Value switch
        {
            EditFieldItemDefData editField => PointerText(editField.EditFieldPointer),
            ListBoxItemDefData listBox => PointerText(listBox.ListBoxPointer),
            MultiItemDefData multi => PointerText(multi.MultiPointer),
            DvarEnumItemDefData dvarEnum => PointerText(dvarEnum.DvarEnumNamePointer),
            NewsTickerItemDefData newsTicker => PointerText(newsTicker.NewsTickerPointer),
            TextScrollItemDefData textScroll => PointerText(textScroll.TextScrollPointer),
            NoItemDefData none => $"0x{none.Reserved:X8} Reserved",
            _ => "<unset>"
        };
    }

    private static bool ItemUsesEditFieldData(ItemDefType type)
    {
        return type is ItemDefType.Text
            or ItemDefType.EditField
            or ItemDefType.NumericField
            or ItemDefType.Slider
            or ItemDefType.YesNo
            or ItemDefType.Bind
            or ItemDefType.Validation
            or ItemDefType.DecimalField
            or ItemDefType.UpDown
            or ItemDefType.EmailField
            or ItemDefType.PassWordField;
    }

    private static void AnalyzeEventSetPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        MenuEventHandlerSet? set,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        if (set is null || !seenEventSets.Add(set))
            return;

        ObservePointer(stats, "MenuEventHandlerSet.EventHandlers", set.EventHandlers, context, "required handler pointer table when eventHandlerCount > 0");
        foreach (MenuEventHandlerReference handlerRef in set.Handlers)
        {
            string handlerContext = $"{context}.handler[{handlerRef.Index}]";
            ObservePointer(stats, "MenuEventHandlerSet.HandlerRef", handlerRef.Pointer, handlerContext, "handler array entry");
            if (handlerRef.Handler is { } handler)
                AnalyzeEventHandlerPointers(stats, handler, handlerContext, seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        }
    }

    private static void AnalyzeEventHandlerPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        MenuEventHandler handler,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        string handlerContext = $"{context} type={handler.EventType}";
        switch (handler.EventType)
        {
            case MenuEventHandlerType.ConditionalScript:
                if (handler.EventData.ConditionalScript is { } conditionalData)
                    ObservePointer(stats, "MenuEventHandler.EventData.ConditionalScript", conditionalData.ConditionalScriptPointer, handlerContext, "event union branch selected by eventType=1");
                if (handler.ConditionalScript is { } conditional)
                {
                    ObservePointer(stats, "ConditionalScript.EventExpression", conditional.EventExpression, handlerContext, "conditional expression statement; PS3 resolves before nested event set");
                    ObservePointer(stats, "ConditionalScript.EventHandlerSet", conditional.EventHandlerSet, handlerContext, "nested event handler set");
                    AnalyzeStatementPointers(stats, conditional.EventStatement, $"{handlerContext}.ConditionalExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
                    AnalyzeEventSetPointers(stats, conditional.EventHandlers, $"{handlerContext}.ConditionalHandlers", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
                }
                break;
            case MenuEventHandlerType.ElseScript:
                if (handler.EventData.ElseScript is { } elseData)
                    ObservePointer(stats, "MenuEventHandler.EventData.ElseScript", elseData.EventHandlerSetPointer, handlerContext, "event union branch selected by eventType=2");
                AnalyzeEventSetPointers(stats, handler.ElseScriptSet, $"{handlerContext}.ElseScript", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
                break;
            case MenuEventHandlerType.SetLocalVarBool:
            case MenuEventHandlerType.SetLocalVarInt:
            case MenuEventHandlerType.SetLocalVarFloat:
            case MenuEventHandlerType.SetLocalVarString:
                if (handler.EventData.SetLocalVarData is { } setLocalData)
                    ObservePointer(stats, "MenuEventHandler.EventData.SetLocalVarData", setLocalData.SetLocalVarDataPointer, handlerContext, "event union branch selected by eventType=3..6");
                if (handler.SetLocalVarData is { } setLocal)
                {
                    ObservePointer(stats, "SetLocalVarData.Expression", setLocal.Expression, handlerContext, "set-local-var expression statement");
                    AnalyzeStatementPointers(stats, setLocal.ExpressionStatement, $"{handlerContext}.SetLocalVarExpression", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
                }
                break;
        }
    }

    private static void AnalyzeKeyHandlerPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        ItemKeyHandler? handler,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        if (handler is null || !seenKeyHandlers.Add(handler))
            return;

        string handlerContext = $"{context} key={handler.Key}";
        ObservePointer(stats, "ItemKeyHandler.Action", handler.Action, handlerContext, "optional key action event set; PS3 0x10c4c0 permits null");
        ObservePointer(stats, "ItemKeyHandler.Next", handler.Next, handlerContext, "optional key handler linked-list tail; PS3 recursive helper permits null");
        AnalyzeEventSetPointers(stats, handler.ActionSet, $"{handlerContext}.Action", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        AnalyzeKeyHandlerPointers(stats, handler.NextHandler, $"{handlerContext}.Next", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
    }

    private static void AnalyzeStatementPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        Statement? statement,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        if (statement is null || !seenStatements.Add(statement))
            return;

        string statementContext = $"{context} entries={statement.NumEntries}";
        ObservePointer(stats, "Statement.Entries", statement.Entries, statementContext, "expression entry table; count gates runtime use");
        ObservePointer(stats, "Statement.SupportingData", statement.SupportingData, statementContext, "optional expression support data; required only for function/static-dvar/string lists");
        AnalyzeExpressionSupportPointers(stats, statement.SupportingDataValue, $"{statementContext}.SupportingData", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);

        for (int i = 0; i < statement.LoadedEntries.Count; i++)
        {
            ExpressionEntry entry = statement.LoadedEntries[i];
            if (entry.Kind == ExpressionEntryKind.Operand && entry.Operand.Value is FunctionOperandValue functionValue)
            {
                ObservePointer(stats, "ExpressionEntry.FunctionStatement", functionValue.StatementPointer, $"{statementContext}.entry[{i}]", "optional nested function statement operand");
                AnalyzeStatementPointers(stats, entry.FunctionStatement, $"{statementContext}.entry[{i}].Function", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
            }
        }
    }

    private static void AnalyzeExpressionSupportPointers(
        Dictionary<string, MenuPointerFieldStat> stats,
        ExpressionSupportingData? data,
        string context,
        HashSet<MenuEventHandlerSet> seenEventSets,
        HashSet<ItemKeyHandler> seenKeyHandlers,
        HashSet<Statement> seenStatements,
        HashSet<ExpressionSupportingData> seenSupportData)
    {
        if (data is null || !seenSupportData.Add(data))
            return;

        ObservePointer(stats, "ExpressionSupportingData.UiFunctions", data.UiFunctions.Functions, context, "optional statement-function pointer table");
        foreach (StatementReference function in data.UiFunctions.LoadedFunctions)
        {
            ObservePointer(stats, "UIFunctionList.Function", function.Pointer, $"{context}.function[{function.Index}]", "function statement reference");
            AnalyzeStatementPointers(stats, function.Statement, $"{context}.function[{function.Index}]", seenEventSets, seenKeyHandlers, seenStatements, seenSupportData);
        }

        ObservePointer(stats, "ExpressionSupportingData.StaticDvars", data.StaticDvarList.StaticDvars, context, "optional static dvar pointer table");
        foreach (StaticDvarReference dvar in data.StaticDvarList.LoadedStaticDvars)
        {
            ObservePointer(stats, "StaticDvarList.StaticDvar", dvar.Pointer, $"{context}.staticDvar[{dvar.Index}]", "static dvar reference");
            if (dvar.StaticDvar is { } staticDvar)
                ObservePointer(stats, "StaticDvar.DvarRuntimeHandle", staticDvar.Dvar, $"{context}.staticDvar[{dvar.Index}] name={staticDvar.DvarNameString ?? "<null>"}", "runtime cache slot; PS3 helper fills from dvarName, serialized null is expected");
        }

        ObservePointer(stats, "ExpressionSupportingData.UiStrings", data.UiStrings.Strings, context, "optional XString pointer table");
    }

    private static void ObservePointer<T>(
        Dictionary<string, MenuPointerFieldStat> stats,
        string field,
        XPointer<T> pointer,
        string context,
        string classification,
        string? itemType = null)
    {
        string targetType = GetPointerTargetName(typeof(T));
        string key = $"{field}|{targetType}|{pointer.ResolutionMode}|{classification}";
        if (!stats.TryGetValue(key, out MenuPointerFieldStat? stat))
        {
            stat = new MenuPointerFieldStat(field, targetType, pointer.ResolutionMode.ToString(), classification);
            stats.Add(key, stat);
        }

        if (pointer.Raw == 0)
        {
            stat.NullCount++;
            if (itemType is not null)
                IncrementCount(stat.NullItemTypes, itemType);
        }
        else
        {
            stat.NonNullCount++;
            if (itemType is not null)
                IncrementCount(stat.NonNullItemTypes, itemType);
        }

        if (itemType is not null)
            stat.ItemTypes.Add(itemType);

        if (pointer.Raw == 0 && stat.Samples.Count < 5)
            stat.Samples.Add($"{context} raw=0x{pointer.Raw:X8}");
    }

    private static string GetPointerTargetName(Type type)
    {
        if (type.IsArray)
            return $"{GetPointerTargetName(type.GetElementType() ?? typeof(object))}[]";

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(XPointer<>))
            return $"XPointer<{GetPointerTargetName(type.GetGenericArguments()[0])}>";

        return type.Name;
    }

    private static void IncrementCount(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
    }

    private static string FormatCountMap(Dictionary<string, int> counts)
    {
        return string.Join("|", counts
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => $"{x.Key}:{x.Value}"));
    }

    private sealed class MenuPointerFieldStat
    {
        public MenuPointerFieldStat(
            string field,
            string targetType,
            string resolutionMode,
            string classification)
        {
            Field = field;
            TargetType = targetType;
            ResolutionMode = resolutionMode;
            Classification = classification;
        }

        public string Field { get; }
        public string TargetType { get; }
        public string ResolutionMode { get; }
        public string Classification { get; }
        public int NullCount { get; set; }
        public int NonNullCount { get; set; }
        public HashSet<string> ItemTypes { get; } = [];
        public Dictionary<string, int> NullItemTypes { get; } = [];
        public Dictionary<string, int> NonNullItemTypes { get; } = [];
        public List<string> Samples { get; } = [];
    }

    private static void WriteScriptStringTableReport(
        FastFile.Models.Database.DbFileLoad.FastFileLoad session,
        string path)
    {
        string outputPath = Path.Combine(
            scratchRoot,
            "debug-output",
            $"{Path.GetFileNameWithoutExtension(path)}.scriptstrings.tsv");
        string mapPath = Path.Combine(
            scratchRoot,
            "debug-output",
            $"{Path.GetFileNameWithoutExtension(path)}.scriptstrings.map.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var lines = new List<string>
        {
            "indexHex\tindexDecimal\tpointerSourceOffset\tpointerCell\tpointerRaw\tvalue"
        };
        var mapLines = new List<string>
        {
            $"Fastfile: {path}",
            $"Script string count: {session.XAssetList.ScriptStringCount}",
            ""
        };

        foreach (XScriptStringEntry entry in session.XAssetList.ScriptStrings)
        {
            lines.Add(string.Join("\t",
                $"0x{entry.Index:X4}",
                entry.Index.ToString(),
                $"0x{entry.PointerSerializedOffset:X}",
                entry.PointerCellAddress.ToString(),
                $"0x{entry.Pointer.Raw:X8}",
                Tsv(entry.Value ?? string.Empty)));
            mapLines.Add($"0x{entry.Index:X4} -> {ScriptStringMapValue(entry.Value)}");
        }

        File.WriteAllLines(outputPath, lines);
        File.WriteAllLines(mapPath, mapLines);
        Console.WriteLine($"Script string table: {outputPath}");
        Console.WriteLine($"Script string map: {mapPath}");
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string? AddressText(XBlockAddress? address)
    {
        return address?.ToString();
    }

    private static string Tsv(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private static string ScriptStringMapValue(string? value)
    {
        return value is null
            ? "<null>"
            : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
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
