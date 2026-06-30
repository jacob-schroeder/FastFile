using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using FastFile.Models.Assets.ColMap;
using FastFile.Models.Assets.GfxMap;
using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Pointers;
using FastFile.Render.Glb;
using ModelVec3 = FastFile.Models.Math.Vec3;

namespace FastFile.Render.Export;

internal sealed class MapRenderExporter
{
    private const int WorldVertexLayerStride = 0x1C;
    private const int WorldVertexLayerTexCoordOffset = 0x04;
    private const int WorldVertexLayerTexCoordSize = 0x08;
    private const float WorldVertexLayerMaxTexCoordMagnitude = 1_000_000f;
    private const float MaxTexturedUvFailureRatio = 0.45f;

    private readonly RenderOptions _options;
    private readonly RenderAssetLookup _assets;
    private readonly GfxImageStreamResolver _imageStreams;
    private readonly Dictionary<string, CachedTexture> _textureByImageAddress = new();

    public MapRenderExporter(RenderOptions options, RenderAssetLookup assets, GfxImageStreamResolver imageStreams)
    {
        _options = options;
        _assets = assets;
        _imageStreams = imageStreams;
    }

    public MapRenderSummary Export(
        string inputPath,
        GfxWorldAsset? gfxMap,
        ClipMapAsset? colMap)
    {
        string stem = Path.GetFileNameWithoutExtension(inputPath);
        var summary = new MapRenderSummary(inputPath, _options.OutputDirectory);

        if (gfxMap is not null)
            ExportGfxMap(stem, gfxMap, summary);
        else
            summary.Warnings.Add("No GfxMap asset was loaded.");

        if (colMap is not null)
        {
            ExportCollisionDebug(stem, colMap, summary);
            ExportStaticModelCsv(stem, colMap, summary);
            ExportMapEnts(stem, colMap, summary);
        }
        else
        {
            summary.Warnings.Add("No ColMapMp asset was loaded.");
        }

        if (gfxMap is not null)
            WriteViewerHtml(stem, colMap is not null, summary);

        return summary;
    }

    private void ExportGfxMap(
        string stem,
        GfxWorldAsset gfxMap,
        MapRenderSummary summary)
    {
        List<Vec3f> positions = DecodeWorldPositions(gfxMap.WorldDraw.VertexData.PackedVertices);
        var builder = new GlbSceneBuilder("FastFile.Render GfxMap export");
        int mesh = builder.AddMesh($"{NameOrStem(gfxMap.Name, stem)} GfxMap");
        var materialRows = new List<MaterialTextureReportRow>();
        var uvLayoutSamples = new List<string>();

        int skippedIndices = 0;
        int suppressedUvTextureGroups = 0;
        var suppressedUvTextureSamples = new List<string>();
        var uvCandidateSurfaces = new Dictionary<WorldUvCandidateGroupKey, List<GfxSurface>>();
        var materialUvCandidateGroups = new List<WorldMaterialUvCandidateGroup>();
        var surfaceDebugRows = new List<WorldSurfaceDebugRow>();
        foreach (IGrouping<string, IndexedGfxSurface> group in gfxMap.Dpvs.Surfaces
            .Select((surface, index) => new IndexedGfxSurface(index, surface))
            .GroupBy(x => MaterialKey(x.Surface))
            .OrderBy(x => x.Key))
        {
            if (IsShadowCasterMaterial(group.Key))
                continue;

            List<IndexedGfxSurface> indexedSurfaces = group.ToList();
            List<GfxSurface> surfaces = indexedSurfaces.Select(x => x.Surface).ToList();
            MaterialAsset? resolvedMaterial = indexedSurfaces.Select(x => ResolveSurfaceMaterial(x.Surface)).FirstOrDefault(x => x is not null);
            TextureBinding? texture = resolvedMaterial is null ? null : BindBaseColorTexture(builder, resolvedMaterial, summary);
            materialRows.Add(new MaterialTextureReportRow(group.Key, group.Count(), resolvedMaterial, texture));
            int? textureIndex = texture?.GlbTextureIndex;
            MaterialTechniqueSetAsset? techset = ResolveTechniqueSet(resolvedMaterial);
            if (techset is not null)
            {
                var candidateGroup = new WorldUvCandidateGroupKey(
                    techset.WorldVertexFormat,
                    TechniqueRoutesText(techset) ?? string.Empty,
                    TechniqueRoutesSemanticText(techset) ?? string.Empty);
                if (!uvCandidateSurfaces.TryGetValue(candidateGroup, out List<GfxSurface>? formatSurfaces))
                {
                    formatSurfaces = [];
                    uvCandidateSurfaces.Add(candidateGroup, formatSurfaces);
                }

                formatSurfaces.AddRange(surfaces);
                materialUvCandidateGroups.Add(new WorldMaterialUvCandidateGroup(
                    group.Key,
                    techset.Name ?? string.Empty,
                    techset.WorldVertexFormat,
                    TechniqueRoutesText(techset) ?? string.Empty,
                    surfaces));
            }

            WorldTexCoordDecoder? texCoordDecoder = textureIndex.HasValue
                ? SelectWorldTexCoordDecoder(gfxMap, surfaces, techset?.WorldVertexFormat, uvLayoutSamples)
                : null;
            if (textureIndex.HasValue && texCoordDecoder is null)
                textureIndex = null;
            bool hasTexture = textureIndex.HasValue;

            var groupPositions = new List<Vec3f>();
            List<Vec2f>? groupTexCoords = hasTexture ? [] : null;
            List<uint> indices = new();
            int texCoordFailures = 0;
            foreach (IndexedGfxSurface indexedSurface in indexedSurfaces)
            {
                GfxSurface surface = indexedSurface.Surface;
                SrfTriangles triangles = surface.Triangles;
                int indexCount = checked(triangles.TriCount * 3);
                if (triangles.BaseIndex < 0 || triangles.BaseIndex + indexCount > gfxMap.WorldDraw.Indices.Count)
                {
                    skippedIndices += indexCount;
                    continue;
                }

                int firstTriangle = indices.Count / 3;
                int writtenSurfaceIndices = 0;
                for (int i = 0; i < indexCount; i++)
                {
                    int surfaceIndex = gfxMap.WorldDraw.Indices[triangles.BaseIndex + i];
                    int vertex = triangles.BaseVertex + surfaceIndex;
                    if (vertex >= 0 && vertex < positions.Count)
                    {
                        indices.Add((uint)groupPositions.Count);
                        groupPositions.Add(positions[vertex]);
                        if (groupTexCoords is not null)
                        {
                            if (texCoordDecoder!.TryRead(surface, surfaceIndex, vertex, out Vec2f texCoord))
                            {
                                groupTexCoords.Add(texCoord);
                            }
                            else
                            {
                                texCoordFailures++;
                                groupTexCoords.Add(default);
                            }
                        }
                        writtenSurfaceIndices++;
                    }
                    else
                    {
                        skippedIndices++;
                    }
                }

                if (writtenSurfaceIndices >= 3)
                {
                    surfaceDebugRows.Add(CreateSurfaceDebugRow(
                        gfxMap,
                        indexedSurface.Index,
                        group.Key,
                        resolvedMaterial,
                        techset,
                        texture,
                        firstTriangle,
                        writtenSurfaceIndices / 3,
                        surface));
                }
            }

            if (indices.Count == 0)
                continue;

            bool suppressTexture = groupTexCoords is not null &&
                texCoordFailures > 0 &&
                texCoordFailures / (float)groupTexCoords.Count > MaxTexturedUvFailureRatio;
            if (suppressTexture)
            {
                suppressedUvTextureGroups++;
                if (suppressedUvTextureSamples.Count < 8)
                    suppressedUvTextureSamples.Add($"{group.Key} {texCoordFailures}/{groupTexCoords!.Count}");
                groupTexCoords = null;
                textureIndex = null;
            }

            int positionAccessor = builder.AddPositions(groupPositions);
            int? texCoordAccessor = groupTexCoords is null ? null : builder.AddTexCoords(groupTexCoords);
            int material = builder.AddMaterial(
                group.Key,
                textureIndex.HasValue ? new Rgba(1.0f, 1.0f, 1.0f, 1.0f) : ColorFromString(group.Key, 0.82f),
                textureIndex,
                useTextureAlpha: texture?.HasTransparency == true);
            builder.AddPrimitive(
                mesh,
                positionAccessor,
                indices,
                GlbPrimitiveMode.Triangles,
                material,
                texCoordAccessor: texCoordAccessor);
        }

        builder.AddNode($"{NameOrStem(gfxMap.Name, stem)}_gfx", mesh);
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.gfx.glb");
        builder.Write(outputPath);
        WriteMaterialTextureReport(stem, materialRows, summary);
        WriteWorldVertexFormatReport(stem, materialRows, summary);
        WriteWorldUvCandidateReport(stem, gfxMap, uvCandidateSurfaces, summary);
        WriteWorldMaterialUvCandidateReport(stem, gfxMap, materialUvCandidateGroups, summary);
        WriteSurfaceDebugManifest(stem, surfaceDebugRows, summary);

        summary.WrittenFiles.Add(outputPath);
        summary.GfxVertexCount = positions.Count;
        summary.GfxIndexCount = gfxMap.WorldDraw.Indices.Count;
        summary.GfxSurfaceCount = gfxMap.Dpvs.Surfaces.Count;
        summary.GfxVertexLayerByteCount = gfxMap.WorldDraw.VertexLayerData.PackedLayerData.Count;
        summary.SurfaceIndexStatus = DescribeSurfaceIndexStats(gfxMap, positions.Count);
        summary.TexCoordStatus = uvLayoutSamples.Count == 0
            ? "no textured world UV groups"
            : string.Join(" | ", uvLayoutSamples.Take(12));
        summary.RuntimeMaterialCount = _assets.MaterialCount;
        summary.RuntimeImageCount = _assets.ImageCount;
        if (skippedIndices > 0)
            summary.Warnings.Add($"Skipped {skippedIndices} GfxMap index value(s) outside the decoded vertex range.");
        if (suppressedUvTextureGroups > 0)
        {
            summary.Warnings.Add(
                $"Suppressed textures for {suppressedUvTextureGroups} material group(s) with >{MaxTexturedUvFailureRatio:P0} failed UV reads " +
                $"to avoid fake (0,0) smear; examples: {string.Join("; ", suppressedUvTextureSamples)}.");
        }
    }

    private void WriteSurfaceDebugManifest(
        string stem,
        IReadOnlyList<WorldSurfaceDebugRow> rows,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.surface-debug.json");
        var manifest = new WorldSurfaceDebugManifest(
            stem,
            $"{stem}.gfx.glb",
            $"{stem}.collision-debug.glb",
            rows);

        using FileStream stream = File.Create(outputPath);
        JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions { WriteIndented = true });
        summary.WrittenFiles.Add(outputPath);
    }

    private void WriteViewerHtml(string stem, bool hasCollision, MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, "viewer.html");
        File.WriteAllText(outputPath, ViewerHtmlWriter.Build(stem, hasCollision), Encoding.UTF8);
        summary.WrittenFiles.Add(outputPath);
    }

    private WorldSurfaceDebugRow CreateSurfaceDebugRow(
        GfxWorldAsset gfxMap,
        int surfaceIndex,
        string materialKey,
        MaterialAsset? material,
        MaterialTechniqueSetAsset? techset,
        TextureBinding? texture,
        int primitiveTriangleStart,
        int primitiveTriangleCount,
        GfxSurface surface)
    {
        SrfTriangles triangles = surface.Triangles;
        IReadOnlyList<UvCandidate> uvCandidates = SelectedWorldUvCandidates(techset?.WorldVertexFormat);
        UvCandidate? selectedUv = uvCandidates.Count == 0 ? null : uvCandidates[0];
        int firstSurfaceIndex = triangles.BaseIndex >= 0 && triangles.BaseIndex < gfxMap.WorldDraw.Indices.Count
            ? gfxMap.WorldDraw.Indices[triangles.BaseIndex]
            : -1;
        int firstGlobalVertex = firstSurfaceIndex < 0 ? -1 : triangles.BaseVertex + firstSurfaceIndex;
        int firstLocalVertex = firstSurfaceIndex < 0 ? -1 : firstSurfaceIndex - SurfaceMinVertexIndex(triangles);
        int firstLayerOffset = selectedUv.HasValue && firstSurfaceIndex >= 0
            ? selectedUv.Value.GetOffset(triangles, firstSurfaceIndex, firstLocalVertex, firstGlobalVertex)
            : triangles.VertexLayerData;

        string drawSurfPacked = surfaceIndex < gfxMap.Dpvs.SurfaceMaterials.Count
            ? $"0x{gfxMap.Dpvs.SurfaceMaterials[surfaceIndex].Packed:X16}"
            : string.Empty;

        return new WorldSurfaceDebugRow(
            SurfaceIndex: surfaceIndex,
            MaterialKey: materialKey,
            PrimitiveTriangleStart: primitiveTriangleStart,
            PrimitiveTriangleCount: primitiveTriangleCount,
            MaterialName: material?.Info.Name ?? string.Empty,
            MaterialPointer: $"0x{surface.MaterialPointer.Raw:X8}",
            DrawSurfPacked: drawSurfPacked,
            CameraRegion: material is null ? string.Empty : $"0x{material.CameraRegion:X2}",
            TechniqueSet: techset?.Name ?? string.Empty,
            WorldVertexFormat: techset?.WorldVertexFormat.ToString() ?? string.Empty,
            RouteKey: TechniqueRoutesText(techset) ?? string.Empty,
            SemanticRouteKey: TechniqueRoutesSemanticText(techset) ?? string.Empty,
            SelectedUvDecoder: selectedUv?.ToString() ?? string.Empty,
            TextureSemantic: texture?.Texture.Semantic.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            TextureImage: texture?.Image.Name ?? string.Empty,
            TextureDecoded: texture?.Decoded ?? false,
            TextureDecodeReason: texture?.DecodeReason ?? string.Empty,
            LightmapIndex: surface.LightmapIndex,
            ReflectionProbeIndex: surface.ReflectionProbeIndex,
            PrimaryLightIndex: surface.PrimaryLightIndex,
            CastsSunShadow: surface.CastsSunShadow,
            VertexLayerData: triangles.VertexLayerData,
            BaseVertex: triangles.BaseVertex,
            MinVertexIndex: triangles.MinVertexIndex,
            VertexCount: triangles.VertexCount,
            TriCount: triangles.TriCount,
            BaseIndex: triangles.BaseIndex,
            FirstSurfaceIndex: firstSurfaceIndex,
            FirstGlobalVertex: firstGlobalVertex,
            FirstLayerOffset: firstLayerOffset,
            FirstIndices: FirstIndexText(gfxMap.WorldDraw.Indices, triangles.BaseIndex, Math.Min(triangles.TriCount * 3, 12)),
            LayerBaseBytes: HexSlice(gfxMap.WorldDraw.VertexLayerData.PackedLayerData, triangles.VertexLayerData, 32),
            FirstLayerBytes: HexSlice(gfxMap.WorldDraw.VertexLayerData.PackedLayerData, firstLayerOffset, 32));
    }

    private void ExportCollisionDebug(
        string stem,
        ClipMapAsset colMap,
        MapRenderSummary summary)
    {
        var builder = new GlbSceneBuilder("FastFile.Render ColMap debug export");
        int collisionMaterial = builder.AddMaterial("collision_triangles", new Rgba(1.0f, 0.22f, 0.08f, 0.45f));
        int staticModelMaterial = builder.AddMaterial("static_xmodel_bounds", new Rgba(0.15f, 0.85f, 0.35f, 1.0f));
        int triggerMaterial = builder.AddMaterial("trigger_hulls", new Rgba(1.0f, 0.85f, 0.15f, 1.0f));

        if (colMap.Verts.Count > 0 && colMap.TriIndices.Count > 0)
        {
            int positionAccessor = builder.AddPositions(colMap.Verts.Select(ConvertModel).ToList());
            int mesh = builder.AddMesh($"{NameOrStem(colMap.Name, stem)} collision triangles");
            builder.AddPrimitive(mesh, positionAccessor, colMap.TriIndices.Select(x => (uint)x).ToList(), GlbPrimitiveMode.Triangles, collisionMaterial);
            builder.AddNode("collision_triangles", mesh);
        }

        AddBoxLineMesh(
            builder,
            "static_xmodel_placement_markers",
            colMap.StaticModelList.Select(StaticModelMarkerBounds),
            staticModelMaterial);

        if (colMap.MapEnts is { } mapEnts)
        {
            AddBoxLineMesh(
                builder,
                "trigger_hulls",
                mapEnts.Trigger.Hulls.Select(x => BoundsToMinMax(x.Bounds.MidPoint, x.Bounds.HalfSize)),
                triggerMaterial);
        }

        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.collision-debug.glb");
        builder.Write(outputPath);

        summary.WrittenFiles.Add(outputPath);
        summary.CollisionVertexCount = colMap.Verts.Count;
        summary.CollisionIndexCount = colMap.TriIndices.Count;
        summary.StaticModelCount = colMap.StaticModelList.Count;
    }

    private void ExportStaticModelCsv(
        string stem,
        ClipMapAsset colMap,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.static-xmodels.csv");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine("index,modelPointerRaw,modelName,originX,originY,originZ,invScaledAxis0X,invScaledAxis0Y,invScaledAxis0Z,invScaledAxis1X,invScaledAxis1Y,invScaledAxis1Z,invScaledAxis2X,invScaledAxis2Y,invScaledAxis2Z,rawField40X,rawField40Y,rawField40Z,extentLikeField4CX,extentLikeField4CY,extentLikeField4CZ");
        for (int i = 0; i < colMap.StaticModelList.Count; i++)
        {
            ClipStaticModel model = colMap.StaticModelList[i];
            writer.WriteLine(string.Join(
                ",",
                i.ToString(CultureInfo.InvariantCulture),
                $"0x{model.XModelPointer.Raw:X8}",
                Csv(model.XModel?.Name),
                F(model.Origin.X),
                F(model.Origin.Y),
                F(model.Origin.Z),
                F(model.InvScaledAxis[0].X),
                F(model.InvScaledAxis[0].Y),
                F(model.InvScaledAxis[0].Z),
                F(model.InvScaledAxis[1].X),
                F(model.InvScaledAxis[1].Y),
                F(model.InvScaledAxis[1].Z),
                F(model.InvScaledAxis[2].X),
                F(model.InvScaledAxis[2].Y),
                F(model.InvScaledAxis[2].Z),
                F(model.AbsMin.X),
                F(model.AbsMin.Y),
                F(model.AbsMin.Z),
                F(model.AbsMax.X),
                F(model.AbsMax.Y),
                F(model.AbsMax.Z)));
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private void WriteWorldVertexFormatReport(
        string stem,
        IReadOnlyList<MaterialTextureReportRow> rows,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.world-vertex-formats.csv");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine("worldVertexFormat,materialGroups,surfaceCount,hasSelectedUvDecoder,decodedTextureGroups,topCameraRegions,topTechsets,topImages,routes,semanticRoutes");

        foreach (var group in rows.GroupBy(row => ResolveTechniqueSet(row.Material)?.WorldVertexFormat).OrderByDescending(x => x.Sum(row => row.SurfaceCount)))
        {
            MaterialWorldVertexFormat? format = group.Key;
            var groupRows = group.ToList();
            string topCameraRegions = TopText(groupRows.Select(row => CameraRegionText(row.Material)));
            string topTechsets = TopText(groupRows.Select(row => ResolveTechniqueSet(row.Material)?.Name));
            string topImages = TopText(groupRows.Select(row => row.Texture?.Image.Name));
            string routes = TopText(groupRows.Select(row => TechniqueRoutesText(ResolveTechniqueSet(row.Material))));
            string semanticRoutes = TopText(groupRows.Select(row => TechniqueRoutesSemanticText(ResolveTechniqueSet(row.Material))));
            writer.WriteLine(string.Join(
                ",",
                format?.ToString() ?? string.Empty,
                groupRows.Count.ToString(CultureInfo.InvariantCulture),
                groupRows.Sum(row => row.SurfaceCount).ToString(CultureInfo.InvariantCulture),
                (SelectedWorldUvCandidates(format).Count > 0).ToString(),
                groupRows.Count(row => row.Texture?.Decoded == true).ToString(CultureInfo.InvariantCulture),
                Csv(topCameraRegions),
                Csv(topTechsets),
                Csv(topImages),
                Csv(routes),
                Csv(semanticRoutes)));
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private void WriteWorldUvCandidateReport(
        string stem,
        GfxWorldAsset gfxMap,
        IReadOnlyDictionary<WorldUvCandidateGroupKey, List<GfxSurface>> surfaceGroups,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.world-uv-candidates.csv");
        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("worldVertexFormat,routeKey,semanticRouteKey,bucket,rank,surfaceCount,candidate,total,valid,badVertex,badOffset,badValue,over4096,maxAbs,score");

        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        foreach ((WorldUvCandidateGroupKey group, List<GfxSurface> surfaces) in surfaceGroups
            .OrderBy(x => x.Key.Format.ToString())
            .ThenByDescending(x => x.Value.Count)
            .ThenBy(x => x.Key.RouteKey, StringComparer.Ordinal))
        {
            IReadOnlyList<UvCandidate> selectedCandidates = SelectedWorldUvCandidates(group.Format);
            List<UvDecodeResult> results = UvCandidate.Candidates
                .Select(candidate => ScoreWorldTexCoordCandidate(gfxMap, candidate, vertexCount, surfaces))
                .OrderByDescending(result => result.ValidIndexedVertices)
                .ThenBy(result => result.BadValueCount)
                .ThenBy(result => result.BadOffsetCount)
                .ThenBy(result => selectedCandidates.Contains(result.Candidate) ? 0 : 1)
                .ThenBy(result => result.Candidate.Format == UvValueFormat.Float2 ? 0 : 1)
                .ThenBy(result => result.Over4096Count)
                .ThenByDescending(result => result.Score)
                .ThenBy(result => result.MaxAbsValue)
                .ToList();

            WriteUvCandidateBucket(writer, group, surfaces.Count, "overall", results.Take(8));
            WriteUvCandidateBucket(writer, group, surfaces.Count, "layer-local", results.Where(result => result.Candidate.IsLayerLocal).Take(8));
            WriteUvCandidateBucket(writer, group, surfaces.Count, "layer-stream-indexed", results.Where(result => result.Candidate.IsLayerStreamIndexed).Take(8));
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private void WriteWorldMaterialUvCandidateReport(
        string stem,
        GfxWorldAsset gfxMap,
        IReadOnlyList<WorldMaterialUvCandidateGroup> materialGroups,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.world-material-uv-candidates.csv");
        using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("materialName,techsetName,worldVertexFormat,routeKey,rank,surfaceCount,candidate,total,valid,badVertex,badOffset,badValue,over4096,maxAbs,score");

        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        foreach (WorldMaterialUvCandidateGroup group in materialGroups
            .OrderBy(x => x.Format.ToString())
            .ThenByDescending(x => x.Surfaces.Count)
            .ThenBy(x => x.MaterialName, StringComparer.Ordinal))
        {
            IReadOnlyList<UvCandidate> selectedCandidates = SelectedWorldUvCandidates(group.Format);
            List<UvDecodeResult> results = UvCandidate.Candidates
                .Where(candidate => candidate.IsLayerStreamIndexed)
                .Select(candidate => ScoreWorldTexCoordCandidate(gfxMap, candidate, vertexCount, group.Surfaces))
                .OrderByDescending(result => result.ValidIndexedVertices)
                .ThenBy(result => result.BadValueCount)
                .ThenBy(result => result.BadOffsetCount)
                .ThenBy(result => selectedCandidates.Contains(result.Candidate) ? 0 : 1)
                .ThenBy(result => result.Candidate.Format == UvValueFormat.Float2 ? 0 : 1)
                .ThenBy(result => result.Over4096Count)
                .ThenByDescending(result => result.Score)
                .ThenBy(result => result.MaxAbsValue)
                .Take(5)
                .ToList();

            foreach ((UvDecodeResult result, int rank) in results.Select((result, index) => (result, index + 1)))
            {
                writer.WriteLine(string.Join(
                    ",",
                    Csv(group.MaterialName),
                    Csv(group.TechsetName),
                    Csv(group.Format.ToString()),
                    Csv(group.RouteKey),
                    rank.ToString(CultureInfo.InvariantCulture),
                    group.Surfaces.Count.ToString(CultureInfo.InvariantCulture),
                    Csv(result.Candidate.ToString()),
                    result.TotalIndexedVertices.ToString(CultureInfo.InvariantCulture),
                    result.ValidIndexedVertices.ToString(CultureInfo.InvariantCulture),
                    result.BadVertexCount.ToString(CultureInfo.InvariantCulture),
                    result.BadOffsetCount.ToString(CultureInfo.InvariantCulture),
                    result.BadValueCount.ToString(CultureInfo.InvariantCulture),
                    result.Over4096Count.ToString(CultureInfo.InvariantCulture),
                    result.MaxAbsValue.ToString(CultureInfo.InvariantCulture),
                    result.Score.ToString(CultureInfo.InvariantCulture)));
            }
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private static void WriteUvCandidateBucket(
        StreamWriter writer,
        WorldUvCandidateGroupKey group,
        int surfaceCount,
        string bucket,
        IEnumerable<UvDecodeResult> results)
    {
        foreach ((UvDecodeResult result, int rank) in results.Select((result, index) => (result, index + 1)))
        {
            writer.WriteLine(string.Join(
                ",",
                Csv(group.Format.ToString()),
                Csv(group.RouteKey),
                Csv(group.SemanticRouteKey),
                Csv(bucket),
                rank.ToString(CultureInfo.InvariantCulture),
                surfaceCount.ToString(CultureInfo.InvariantCulture),
                Csv(result.Candidate.ToString()),
                result.TotalIndexedVertices.ToString(CultureInfo.InvariantCulture),
                result.ValidIndexedVertices.ToString(CultureInfo.InvariantCulture),
                result.BadVertexCount.ToString(CultureInfo.InvariantCulture),
                result.BadOffsetCount.ToString(CultureInfo.InvariantCulture),
                result.BadValueCount.ToString(CultureInfo.InvariantCulture),
                result.Over4096Count.ToString(CultureInfo.InvariantCulture),
                result.MaxAbsValue.ToString(CultureInfo.InvariantCulture),
                result.Score.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private (Vec3f Min, Vec3f Max) StaticModelMarkerBounds(ClipStaticModel model)
    {
        Vec3f origin = new(model.Origin.X, model.Origin.Y, model.Origin.Z);
        Vec3f halfSize = new(
            MathF.Max(MathF.Abs(model.AbsMax.X), 4.0f),
            MathF.Max(MathF.Abs(model.AbsMax.Y), 4.0f),
            MathF.Max(MathF.Abs(model.AbsMax.Z), 4.0f));

        var raw = (
            Min: new Vec3f(origin.X - halfSize.X, origin.Y - halfSize.Y, origin.Z - halfSize.Z),
            Max: new Vec3f(origin.X + halfSize.X, origin.Y + halfSize.Y, origin.Z + halfSize.Z));

        return (ConvertRaw(raw.Min), ConvertRaw(raw.Max)).Normalize();
    }

    private void ExportMapEnts(
        string stem,
        ClipMapAsset colMap,
        MapRenderSummary summary)
    {
        if (colMap.MapEnts is not { } mapEnts)
        {
            summary.Warnings.Add("ColMapMp did not contain a loaded MapEnts payload.");
            return;
        }

        string entityStringPath = Path.Combine(_options.OutputDirectory, $"{stem}.mapents.txt");
        File.WriteAllText(entityStringPath, mapEnts.EntityString ?? string.Empty, Encoding.Latin1);
        summary.WrittenFiles.Add(entityStringPath);
        summary.MapEntsCharCount = mapEnts.NumEntityChars;

        string stagesPath = Path.Combine(_options.OutputDirectory, $"{stem}.stages.csv");
        using (var writer = new StreamWriter(stagesPath, false, Encoding.UTF8))
        {
            writer.WriteLine("index,stageName,originX,originY,originZ,triggerIndex,sunPrimaryLightIndex");
            for (int i = 0; i < mapEnts.Stages.Count; i++)
            {
                Stage stage = mapEnts.Stages[i];
                writer.WriteLine(string.Join(
                    ",",
                    i.ToString(CultureInfo.InvariantCulture),
                    Csv(stage.StageName),
                    F(stage.Origin.X),
                    F(stage.Origin.Y),
                    F(stage.Origin.Z),
                    stage.TriggerIndex.ToString(CultureInfo.InvariantCulture),
                    stage.SunPrimaryLightIndex.ToString(CultureInfo.InvariantCulture)));
            }
        }

        summary.WrittenFiles.Add(stagesPath);
    }

    private static void AddBoxLineMesh(
        GlbSceneBuilder builder,
        string name,
        IEnumerable<(Vec3f Min, Vec3f Max)> boxes,
        int material)
    {
        var positions = new List<Vec3f>();
        var indices = new List<uint>();
        foreach ((Vec3f min, Vec3f max) in boxes)
            AddBoxLines(positions, indices, min, max);

        if (positions.Count == 0)
            return;

        int positionAccessor = builder.AddPositions(positions);
        int mesh = builder.AddMesh(name);
        builder.AddPrimitive(mesh, positionAccessor, indices, GlbPrimitiveMode.Lines, material);
        builder.AddNode(name, mesh);
    }

    private static void AddBoxLines(
        List<Vec3f> positions,
        List<uint> indices,
        Vec3f min,
        Vec3f max)
    {
        uint start = (uint)positions.Count;
        positions.AddRange(new[]
        {
            new Vec3f(min.X, min.Y, min.Z),
            new Vec3f(max.X, min.Y, min.Z),
            new Vec3f(max.X, max.Y, min.Z),
            new Vec3f(min.X, max.Y, min.Z),
            new Vec3f(min.X, min.Y, max.Z),
            new Vec3f(max.X, min.Y, max.Z),
            new Vec3f(max.X, max.Y, max.Z),
            new Vec3f(min.X, max.Y, max.Z)
        });

        ReadOnlySpan<uint> edgeIndices =
        [
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        ];

        foreach (uint index in edgeIndices)
            indices.Add(start + index);
    }

    private List<Vec3f> DecodeWorldPositions(IReadOnlyList<byte> packedVertices)
    {
        int vertexCount = packedVertices.Count / 0x10;
        var positions = new List<Vec3f>(vertexCount);
        for (int i = 0; i < vertexCount; i++)
        {
            int offset = i * 0x10;
            float x = ReadSingleBigEndian(packedVertices, offset);
            float y = ReadSingleBigEndian(packedVertices, offset + 4);
            float z = ReadSingleBigEndian(packedVertices, offset + 8);
            positions.Add(ConvertRaw(new Vec3f(x, y, z)));
        }

        return positions;
    }

    private WorldTexCoordDecoder? SelectWorldTexCoordDecoder(
        GfxWorldAsset gfxMap,
        IReadOnlyList<GfxSurface> surfaces,
        MaterialWorldVertexFormat? worldVertexFormat,
        List<string> layoutSamples)
    {
        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        if (layer.Count == 0 || vertexCount == 0)
            return null;

        IReadOnlyList<UvCandidate> selectedCandidates = SelectedWorldUvCandidates(worldVertexFormat);
        if (selectedCandidates.Count == 0)
        {
            if (layoutSamples.Count < 12)
                layoutSamples.Add($"{worldVertexFormat?.ToString() ?? "unknown"}:unproven");
            return null;
        }

        UvDecodeResult? best = null;
        foreach (UvCandidate candidate in selectedCandidates)
        {
            UvDecodeResult result = ScoreWorldTexCoordCandidate(gfxMap, candidate, vertexCount, surfaces);
            if (best is null || result.Score > best.Value.Score)
                best = result;
            if (result.TotalIndexedVertices > 0 &&
                result.ValidIndexedVertices == result.TotalIndexedVertices)
            {
                if (layoutSamples.Count < 12)
                    layoutSamples.Add($"{worldVertexFormat?.ToString() ?? "unknown"}:{candidate}");
                return new WorldTexCoordDecoder(layer, candidate, WorldVertexLayerMaxTexCoordMagnitude);
            }
        }

        if (layoutSamples.Count < 12)
        {
            UvDecodeResult failed = best!.Value;
            layoutSamples.Add(
                $"{worldVertexFormat?.ToString() ?? "unknown"}:{failed.Candidate} " +
                $"failed {failed.ValidIndexedVertices}/{failed.TotalIndexedVertices}");
        }
        return null;
    }

    private static IReadOnlyList<UvCandidate> SelectedWorldUvCandidates(MaterialWorldVertexFormat? worldVertexFormat)
    {
        return worldVertexFormat switch
        {
            // Source 02 -> texcoord[3] in the declaration route. The indexed draw
            // uses the same surface index for the position and layer streams,
            // with SrfTriangles.vertexLayerData supplying the layer base.
            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_1_NRM_1 =>
            [
                new UvCandidate(WorldVertexLayerStride, WorldVertexLayerTexCoordOffset, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            // Route names from IW4 declarations identify source 02 as tc0.
            // Official mp_boneyard bytes then resolve the NRM_1 layer records
            // as color + N float2 texcoords + packed normal/tangent payload.
            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_1 =>
            [
                new UvCandidate(0x24, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_3_NRM_1 =>
            [
                new UvCandidate(0x2C, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_4_NRM_1 =>
            [
                new UvCandidate(0x34, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_5_NRM_1 =>
            [
                new UvCandidate(0x3C, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            // NRM_2/NRM_3 append normal-transform payloads instead of only
            // adding texcoord slots. Keep tc0 at the declaration-proven +0x04
            // where the route includes source 02 and the official bytes agree.
            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_2 =>
            [
                new UvCandidate(0x14, 0x08, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            // TEX_3_NRM_2 uses the same route set as TEX_2_NRM_2, but
            // mp_boneyard's stream-indexed layer data validates at a 0x18
            // stride with tc0 at +0x04.
            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_3_NRM_2 =>
            [
                new UvCandidate(0x18, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_3_NRM_3 =>
            [
                new UvCandidate(0x34, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
            ],

            _ => []
        };
    }

    private static UvDecodeResult ScoreWorldTexCoordCandidate(
        GfxWorldAsset gfxMap,
        UvCandidate candidate,
        int vertexCount,
        IEnumerable<GfxSurface>? surfaces = null)
    {
        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        int total = 0;
        int valid = 0;
        int badVertex = 0;
        int badOffset = 0;
        int badValue = 0;
        int over4096 = 0;
        float maxAbs = 0.0f;

        foreach (GfxSurface surface in surfaces ?? gfxMap.Dpvs.Surfaces)
        {
            SrfTriangles triangles = surface.Triangles;
            int indexCount = checked(triangles.TriCount * 3);
            if (triangles.BaseIndex < 0 || triangles.BaseIndex + indexCount > gfxMap.WorldDraw.Indices.Count)
                continue;

            for (int i = 0; i < indexCount; i++)
            {
                int surfaceIndex = gfxMap.WorldDraw.Indices[triangles.BaseIndex + i];
                int vertex = triangles.BaseVertex + surfaceIndex;
                int local = surfaceIndex - SurfaceMinVertexIndex(triangles);
                total++;
                if (vertex < 0 || vertex >= vertexCount)
                {
                    badVertex++;
                    continue;
                }

                if (!candidate.HasValidIndex(triangles, local, vertex, vertexCount))
                {
                    badVertex++;
                    continue;
                }

                int offset = candidate.GetOffset(triangles, surfaceIndex, local, vertex);
                if (offset < 0 || offset + candidate.ByteCount > layer.Count)
                {
                    badOffset++;
                    continue;
                }

                (float u, float v) = candidate.Format == UvValueFormat.Float2
                    ? (ReadSingle(layer, offset, candidate.BigEndian), ReadSingle(layer, offset + 4, candidate.BigEndian))
                    : (ReadHalf(layer, offset, candidate.BigEndian), ReadHalf(layer, offset + 2, candidate.BigEndian));
                float candidateMaxAbs = MathF.Max(MathF.Abs(u), MathF.Abs(v));
                if (!float.IsFinite(u) || !float.IsFinite(v) || candidateMaxAbs > WorldVertexLayerMaxTexCoordMagnitude)
                {
                    badValue++;
                    continue;
                }

                if (candidateMaxAbs > 4096.0f)
                    over4096++;
                maxAbs = MathF.Max(maxAbs, candidateMaxAbs);
                valid++;
            }
        }

        int score = valid - badOffset / 32 - badValue / 16 - badVertex;
        return new UvDecodeResult(candidate, total, valid, badVertex, badOffset, badValue, over4096, maxAbs, score);
    }

    private static string DescribeSurfaceIndexStats(GfxWorldAsset gfxMap, int vertexCount)
    {
        int total = 0;
        int indexBelowSurfaceVertexCount = 0;
        int indexBelowMinVertexIndex = 0;
        int indexInRange = 0;
        var samples = new List<string>();
        for (int surfaceIndex = 0; surfaceIndex < gfxMap.Dpvs.Surfaces.Count; surfaceIndex++)
        {
            GfxSurface surface = gfxMap.Dpvs.Surfaces[surfaceIndex];
            SrfTriangles triangles = surface.Triangles;
            int indexCount = checked(triangles.TriCount * 3);
            if (triangles.BaseIndex < 0 || triangles.BaseIndex + indexCount > gfxMap.WorldDraw.Indices.Count)
                continue;

            int minIndex = int.MaxValue;
            int maxIndex = int.MinValue;
            for (int i = 0; i < indexCount; i++)
            {
                int index = gfxMap.WorldDraw.Indices[triangles.BaseIndex + i];
                minIndex = Math.Min(minIndex, index);
                maxIndex = Math.Max(maxIndex, index);
                total++;
                if (index < triangles.VertexCount)
                    indexBelowSurfaceVertexCount++;
                if (triangles.MinVertexIndex <= 0xFFFF && index < triangles.MinVertexIndex)
                    indexBelowMinVertexIndex++;
                if (triangles.BaseVertex + index >= 0 && triangles.BaseVertex + index < vertexCount)
                    indexInRange++;
            }

            if (samples.Count < 6)
            {
                    samples.Add(
                    $"#{surfaceIndex} layer=0x{triangles.VertexLayerData:X} baseVertex={triangles.BaseVertex} minVertexIndex={triangles.MinVertexIndex} vertexCount={triangles.VertexCount} tri={triangles.TriCount} baseIndex={triangles.BaseIndex} idx={minIndex}..{maxIndex}");
            }
        }

        return $"indices={total} idx<surfaceVertexCount={indexBelowSurfaceVertexCount} idx<minVertexIndex={indexBelowMinVertexIndex} idxInRange={indexInRange} samples=[{string.Join("; ", samples)}]";
    }

    private static int SurfaceMinVertexIndex(SrfTriangles triangles)
    {
        return checked((int)triangles.MinVertexIndex);
    }

    private static int SurfaceVertexCount(SrfTriangles triangles)
    {
        return triangles.VertexCount;
    }

    private static float ReadHalf(IReadOnlyList<byte> bytes, int offset, bool bigEndian)
    {
        ushort raw;
        if (bytes is byte[] array)
        {
            raw = bigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan(offset, sizeof(ushort)))
                : BinaryPrimitives.ReadUInt16LittleEndian(array.AsSpan(offset, sizeof(ushort)));
        }
        else
        {
            Span<byte> scratch = stackalloc byte[sizeof(ushort)];
            scratch[0] = bytes[offset];
            scratch[1] = bytes[offset + 1];
            raw = bigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(scratch)
                : BinaryPrimitives.ReadUInt16LittleEndian(scratch);
        }

        return (float)BitConverter.UInt16BitsToHalf(raw);
    }

    private Vec3f ConvertModel(ModelVec3 value)
    {
        return ConvertRaw(new Vec3f(value.X, value.Y, value.Z));
    }

    private Vec3f ConvertRaw(Vec3f value)
    {
        return _options.RawCoordinates
            ? value
            : new Vec3f(value.X, value.Z, -value.Y);
    }

    private (Vec3f Min, Vec3f Max) BoundsToMinMax(ModelVec3 midpoint, ModelVec3 halfSize)
    {
        var rawMin = new Vec3f(midpoint.X - halfSize.X, midpoint.Y - halfSize.Y, midpoint.Z - halfSize.Z);
        var rawMax = new Vec3f(midpoint.X + halfSize.X, midpoint.Y + halfSize.Y, midpoint.Z + halfSize.Z);
        return (ConvertRaw(rawMin), ConvertRaw(rawMax)).Normalize();
    }

    private static float ReadSingleBigEndian(IReadOnlyList<byte> bytes, int offset)
    {
        if (bytes is byte[] array)
            return BinaryPrimitives.ReadSingleBigEndian(array.AsSpan(offset, sizeof(float)));

        Span<byte> scratch = stackalloc byte[sizeof(float)];
        for (int i = 0; i < scratch.Length; i++)
            scratch[i] = bytes[offset + i];
        return BinaryPrimitives.ReadSingleBigEndian(scratch);
    }

    private static float ReadSingle(IReadOnlyList<byte> bytes, int offset, bool bigEndian)
    {
        if (bytes is byte[] array)
        {
            return bigEndian
                ? BinaryPrimitives.ReadSingleBigEndian(array.AsSpan(offset, sizeof(float)))
                : BinaryPrimitives.ReadSingleLittleEndian(array.AsSpan(offset, sizeof(float)));
        }

        Span<byte> scratch = stackalloc byte[sizeof(float)];
        for (int i = 0; i < scratch.Length; i++)
            scratch[i] = bytes[offset + i];
        return bigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(scratch)
            : BinaryPrimitives.ReadSingleLittleEndian(scratch);
    }

    private string MaterialKey(GfxSurface surface)
    {
        if (!string.IsNullOrWhiteSpace(surface.Material?.Info.Name))
            return surface.Material.Info.Name!;

        MaterialAsset? resolved = ResolveSurfaceMaterial(surface);
        if (!string.IsNullOrWhiteSpace(resolved?.Info.Name))
            return resolved.Info.Name!;

        return $"material_{surface.MaterialPointer.Raw:X8}";
    }

    private MaterialAsset? ResolveSurfaceMaterial(GfxSurface surface)
    {
        return surface.Material ?? _assets.ResolveMaterial(surface.MaterialPointer);
    }

    private TextureBinding? BindBaseColorTexture(
        GlbSceneBuilder builder,
        MaterialAsset material,
        MapRenderSummary summary)
    {
        (MaterialTextureDef Texture, GfxImageAsset Image)? selected = SelectBaseColorTexture(material);
        if (selected is not { } binding)
        {
            summary.TextureDecodeSkippedCount++;
            return null;
        }

        string cacheKey = TextureCacheKey(binding.Texture, binding.Image);
        if (_textureByImageAddress.TryGetValue(cacheKey, out CachedTexture existingTexture))
            return new TextureBinding(binding.Texture, binding.Image, existingTexture.GlbTextureIndex, null, true, null, existingTexture.HasTransparency);

        bool hasPayload = binding.Image.PayloadBytes.Count > 0;
        bool resolvedStream = false;
        IReadOnlyList<byte> payload = binding.Image.PayloadBytes;
        int width = binding.Image.Width;
        int height = binding.Image.Height;
        string reason;
        if (!hasPayload)
        {
            resolvedStream = _imageStreams.TryReadBestPayload(binding.Image, out byte[] streamPayload, out width, out height, out reason);
            if (resolvedStream)
                payload = streamPayload;
        }
        else
        {
            reason = string.Empty;
        }

        if (!resolvedStream && !hasPayload)
        {
            summary.TextureDecodeSkippedCount++;
            return new TextureBinding(binding.Texture, binding.Image, null, null, false, reason, false);
        }

        if (!GfxImageDecoder.TryDecodePng(binding.Image, payload, width, height, out DecodedGfxImage decoded, out reason))
        {
            summary.TextureDecodeSkippedCount++;
            return new TextureBinding(binding.Texture, binding.Image, null, null, false, reason, false);
        }

        string textureDirectory = Path.Combine(_options.OutputDirectory, "textures");
        Directory.CreateDirectory(textureDirectory);
        string pngPath = Path.Combine(textureDirectory, $"{SafeFileName(cacheKey)}.png");
        File.WriteAllBytes(pngPath, decoded.PngBytes);
        int glbTexture = builder.AddPngTexture(decoded.Name, decoded.PngBytes);
        _textureByImageAddress[cacheKey] = new CachedTexture(glbTexture, decoded.HasTransparency);
        summary.DecodedTextureCount++;
        summary.TextureOutputDirectory = textureDirectory;
        return new TextureBinding(binding.Texture, binding.Image, glbTexture, pngPath, true, null, decoded.HasTransparency);
    }

    private (MaterialTextureDef Texture, GfxImageAsset Image)? SelectBaseColorTexture(MaterialAsset material)
    {
        foreach (MaterialTextureDef texture in material.Textures)
        {
            if (!IsPrimaryColorTexture(texture))
                continue;

            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            if (image is not null)
                return (texture, image);
        }

        foreach (MaterialTextureDef texture in material.Textures)
        {
            if (texture.Semantic != 0x02)
                continue;

            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            if (image is not null)
                return (texture, image);
        }

        (MaterialTextureDef Texture, GfxImageAsset Image)? best = null;
        int bestScore = int.MinValue;
        foreach (MaterialTextureDef texture in material.Textures)
        {
            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            int score = TextureScore(texture, image);
            if (image is not null && score > bestScore)
            {
                best = (texture, image);
                bestScore = score;
            }
        }

        return best;
    }

    private static bool IsPrimaryColorTexture(MaterialTextureDef texture)
    {
        return texture.Semantic == 0x02 &&
            texture.NameStart == (byte)'c' &&
            texture.NameEnd == (byte)'p';
    }

    private static int TextureScore(MaterialTextureDef texture, GfxImageAsset? image)
    {
        if (image is null)
            return int.MinValue;

        string name = image.Name ?? string.Empty;
        int score = image.PayloadBytes.Count > 0 ? 100 : 0;
        score += texture.Semantic switch
        {
            0x02 => 20,
            0x05 => -20,
            0x08 => -15,
            _ => 0
        };

        if (name.Contains("color", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("diffuse", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("_col", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("_c", StringComparison.OrdinalIgnoreCase))
            score += 40;

        if (name.Contains("normal", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("nml", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("_n", StringComparison.OrdinalIgnoreCase))
            score -= 60;

        if (name.Contains("spec", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("gloss", StringComparison.OrdinalIgnoreCase))
            score -= 40;

        return score;
    }

    private static string TextureCacheKey(MaterialTextureDef texture, GfxImageAsset image)
    {
        string name = string.IsNullOrWhiteSpace(image.Name)
            ? texture.DataPointer.Raw.ToString("X8", CultureInfo.InvariantCulture)
            : image.Name!;
        return $"{name}_{image.Format:X2}_{image.BaseWidth}x{image.BaseHeight}";
    }

    private void WriteMaterialTextureReport(
        string stem,
        IReadOnlyList<MaterialTextureReportRow> rows,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.material-textures.csv");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine("materialName,surfaceCount,resolvedMaterial,cameraRegion,techsetName,worldVertexFormat,techsetPasses,textureCount,textures,selectedTextureSemantic,imageName,width,height,baseWidth,baseHeight,format,payloadBytes,streamData,decoded,hasTransparency,pngPath,decodeReason");
        foreach (MaterialTextureReportRow row in rows)
        {
            TextureBinding? texture = row.Texture;
            MaterialTechniqueSetAsset? techset = ResolveTechniqueSet(row.Material);
            writer.WriteLine(string.Join(
                ",",
                Csv(row.MaterialName),
                row.SurfaceCount.ToString(CultureInfo.InvariantCulture),
                Csv(row.Material?.Info.Name),
                CameraRegionText(row.Material) ?? string.Empty,
                Csv(techset?.Name),
                techset?.WorldVertexFormat.ToString() ?? string.Empty,
                Csv(TechniquePassText(techset)),
                row.Material?.Textures.Count.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(row.Material is null ? null : TextureListText(row.Material)),
                texture?.Texture.Semantic.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(texture?.Image.Name),
                texture?.Image.Width.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                texture?.Image.Height.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                texture?.Image.BaseWidth.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                texture?.Image.BaseHeight.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                texture is null ? string.Empty : $"0x{texture.Value.Image.Format:X2}",
                texture?.Image.PayloadBytes.Count.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(texture is null ? null : StreamDataText(texture.Value.Image)),
                texture?.Decoded.ToString() ?? "False",
                texture?.HasTransparency.ToString() ?? "False",
                Csv(texture?.PngPath),
                Csv(texture?.DecodeReason)));
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private MaterialTechniqueSetAsset? ResolveTechniqueSet(MaterialAsset? material)
    {
        if (material is null)
            return null;

        return material.TechniqueSet ?? _assets.ResolveTechniqueSet(material.TechniqueSetPointer);
    }

    private static string? CameraRegionText(MaterialAsset? material)
    {
        return material is null
            ? null
            : $"0x{material.CameraRegion:X2}";
    }

    private string TextureListText(MaterialAsset material)
    {
        return string.Join(
            "; ",
            material.Textures.Select(texture =>
            {
                GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
                string name = image?.Name ?? $"0x{texture.DataPointer.Raw:X8}";
                return $"sem=0x{texture.Semantic:X2} sampler=0x{texture.SamplerState:X2} hash=0x{texture.NameHash:X8} nameBytes={texture.NameStart:X2}-{texture.NameEnd:X2} image={name}";
            }));
    }

    private string? TechniquePassText(MaterialTechniqueSetAsset? techset)
    {
        if (techset is null)
            return null;

        return string.Join(
            "; ",
            techset.TechniqueSlots
                .Where(slot => slot.Technique is not null)
                .Select(slot =>
                {
                    string passes = string.Join(
                        "|",
                        slot.Technique!.Passes.Select((pass, index) =>
                        {
                            MaterialVertexDeclarationAsset? decl = pass.VertexDeclaration ?? _assets.ResolveVertexDeclaration(pass.VertexDeclPointer);
                            string routing = decl is null
                                ? "vd=<null>"
                                : $"vdStreams={decl.StreamCount} opt={decl.HasOptionalSource} routes={string.Join(" ", decl.Routing.Select(x => $"{x.Source:X2}->{x.Dest:X2}"))}";
                            return $"p{index}:{pass.VertexShader?.Name}/{pass.PixelShader?.Name} {routing} args={pass.PerPrimArgCount}+{pass.PerObjArgCount}+{pass.StableArgCount}";
                        }));
                    return $"slot{slot.Index}:{slot.Technique!.Name}[{passes}]";
                }));
    }

    private string? TechniqueRoutesText(MaterialTechniqueSetAsset? techset)
    {
        if (techset is null)
            return null;

        return string.Join(
            "; ",
            techset.TechniqueSlots
                .Where(slot => slot.Technique is not null)
                .SelectMany(slot => slot.Technique!.Passes)
                .Select(pass => pass.VertexDeclaration ?? _assets.ResolveVertexDeclaration(pass.VertexDeclPointer))
                .Where(decl => decl is not null)
                .Select(decl => TechniqueRouteText(decl!, includeSemanticNames: false))
                .Distinct()
                .Take(6));
    }

    private string? TechniqueRoutesSemanticText(MaterialTechniqueSetAsset? techset)
    {
        if (techset is null)
            return null;

        return string.Join(
            "; ",
            techset.TechniqueSlots
                .Where(slot => slot.Technique is not null)
                .SelectMany(slot => slot.Technique!.Passes)
                .Select(pass => pass.VertexDeclaration ?? _assets.ResolveVertexDeclaration(pass.VertexDeclPointer))
                .Where(decl => decl is not null)
                .Select(decl => TechniqueRouteText(decl!, includeSemanticNames: true))
                .Distinct()
                .Take(6));
    }

    private static string TechniqueRouteText(MaterialVertexDeclarationAsset decl, bool includeSemanticNames)
    {
        return $"streams={decl.StreamCount} routes={string.Join(" ", decl.Routing.Select((route, index) =>
        {
            if (index >= decl.StreamCount)
                return includeSemanticNames ? "--" : $"{route.Source:X2}->{route.Dest:X2}";

            if (!includeSemanticNames)
                return $"{route.Source:X2}->{route.Dest:X2}";

            return $"{RouteSourceName(route.Source)}->{RouteDestinationName(route.Dest)}";
        }))}";
    }

    private static string RouteSourceName(byte source)
    {
        return source switch
        {
            0x00 => "position",
            0x01 => "color",
            0x02 => "texcoord[0]",
            0x03 => "normal",
            0x04 => "tangent",
            0x05 => "texcoord[1]",
            0x06 => "texcoord[2]",
            0x07 => "normalTransform[0]",
            0x08 => "normalTransform[1]",
            _ => $"source[{source:X2}]"
        };
    }

    private static string RouteDestinationName(byte dest)
    {
        return dest switch
        {
            0x00 => "position",
            0x01 => "normal",
            0x02 => "color[0]",
            0x03 => "color[1]",
            0x04 => "depth",
            0x05 => "texcoord[0]",
            0x06 => "texcoord[1]",
            0x07 => "texcoord[2]",
            0x08 => "texcoord[3]",
            0x09 => "texcoord[4]",
            0x0A => "texcoord[5]",
            0x0B => "texcoord[6]",
            0x0C => "texcoord[7]",
            _ => $"dest[{dest:X2}]"
        };
    }

    private static string TopText(IEnumerable<string?> values)
    {
        return string.Join(
            "; ",
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value!)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(6)
                .Select(group => $"{group.Key} ({group.Count()})"));
    }

    private static string StreamDataText(GfxImageAsset image)
    {
        return string.Join(
            ";",
            image.StreamData.Select(x => $"{x.Width}x{x.Height}:0x{x.LevelSizeAndOffset:X8}"));
    }

    private static string SafeFileName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
            builder.Append(Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch);
        return builder.Length == 0 ? "image" : builder.ToString();
    }

    private static Rgba ColorFromString(string key, float alpha)
    {
        uint hash = 2166136261;
        foreach (char ch in key)
        {
            hash ^= ch;
            hash *= 16777619;
        }

        float r = 0.25f + ((hash >> 16) & 0xff) / 255.0f * 0.65f;
        float g = 0.25f + ((hash >> 8) & 0xff) / 255.0f * 0.65f;
        float b = 0.25f + (hash & 0xff) / 255.0f * 0.65f;
        return new Rgba(r, g, b, alpha);
    }

    private static bool IsShadowCasterMaterial(string key)
    {
        return key.Contains("shadowcaster", StringComparison.OrdinalIgnoreCase);
    }

    private static string NameOrStem(string? value, string stem)
    {
        return string.IsNullOrWhiteSpace(value) ? stem : value;
    }

    private static string F(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string HexSlice(IReadOnlyList<byte> bytes, int offset, int count)
    {
        if (offset < 0 || offset >= bytes.Count || count <= 0)
            return string.Empty;

        int end = Math.Min(bytes.Count, offset + count);
        var builder = new StringBuilder((end - offset) * 2);
        for (int i = offset; i < end; i++)
            builder.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static string FirstIndexText(IReadOnlyList<ushort> indices, int offset, int count)
    {
        if (offset < 0 || offset >= indices.Count || count <= 0)
            return string.Empty;

        int end = Math.Min(indices.Count, offset + count);
        return string.Join(" ", Enumerable.Range(offset, end - offset).Select(i => indices[i].ToString(CultureInfo.InvariantCulture)));
    }
}

internal sealed class MapRenderSummary
{
    public MapRenderSummary(string inputPath, string outputDirectory)
    {
        InputPath = inputPath;
        OutputDirectory = outputDirectory;
    }

    public string InputPath { get; }
    public string OutputDirectory { get; }
    public List<string> WrittenFiles { get; } = [];
    public List<string> Warnings { get; } = [];
    public int GfxVertexCount { get; set; }
    public int GfxIndexCount { get; set; }
    public int GfxSurfaceCount { get; set; }
    public int GfxVertexLayerByteCount { get; set; }
    public string? SurfaceIndexStatus { get; set; }
    public int RuntimeMaterialCount { get; set; }
    public int RuntimeImageCount { get; set; }
    public int DecodedTextureCount { get; set; }
    public int TextureDecodeSkippedCount { get; set; }
    public string? TextureOutputDirectory { get; set; }
    public string? TexCoordStatus { get; set; }
    public int CollisionVertexCount { get; set; }
    public int CollisionIndexCount { get; set; }
    public int StaticModelCount { get; set; }
    public int MapEntsCharCount { get; set; }
}

internal readonly record struct Vec3f(float X, float Y, float Z)
{
    public static Vec3f Min(Vec3f left, Vec3f right)
    {
        return new Vec3f(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y),
            MathF.Min(left.Z, right.Z));
    }

    public static Vec3f Max(Vec3f left, Vec3f right)
    {
        return new Vec3f(
            MathF.Max(left.X, right.X),
            MathF.Max(left.Y, right.Y),
            MathF.Max(left.Z, right.Z));
    }
}

internal readonly record struct Vec2f(float U, float V);

internal readonly record struct IndexedGfxSurface(
    int Index,
    GfxSurface Surface);

internal sealed record WorldSurfaceDebugManifest(
    string Map,
    string GfxGlb,
    string CollisionGlb,
    IReadOnlyList<WorldSurfaceDebugRow> Surfaces);

internal sealed record WorldSurfaceDebugRow(
    int SurfaceIndex,
    string MaterialKey,
    int PrimitiveTriangleStart,
    int PrimitiveTriangleCount,
    string MaterialName,
    string MaterialPointer,
    string DrawSurfPacked,
    string CameraRegion,
    string TechniqueSet,
    string WorldVertexFormat,
    string RouteKey,
    string SemanticRouteKey,
    string SelectedUvDecoder,
    string TextureSemantic,
    string TextureImage,
    bool TextureDecoded,
    string TextureDecodeReason,
    byte LightmapIndex,
    byte ReflectionProbeIndex,
    byte PrimaryLightIndex,
    byte CastsSunShadow,
    int VertexLayerData,
    int BaseVertex,
    uint MinVertexIndex,
    ushort VertexCount,
    ushort TriCount,
    int BaseIndex,
    int FirstSurfaceIndex,
    int FirstGlobalVertex,
    int FirstLayerOffset,
    string FirstIndices,
    string LayerBaseBytes,
    string FirstLayerBytes);

internal readonly record struct TextureBinding(
    MaterialTextureDef Texture,
    GfxImageAsset Image,
    int? GlbTextureIndex,
    string? PngPath,
    bool Decoded,
    string? DecodeReason,
    bool HasTransparency);

internal readonly record struct CachedTexture(
    int GlbTextureIndex,
    bool HasTransparency);

internal readonly record struct MaterialTextureReportRow(
    string MaterialName,
    int SurfaceCount,
    MaterialAsset? Material,
    TextureBinding? Texture);

internal readonly record struct WorldUvCandidateGroupKey(
    MaterialWorldVertexFormat Format,
    string RouteKey,
    string SemanticRouteKey);

internal readonly record struct WorldMaterialUvCandidateGroup(
    string MaterialName,
    string TechsetName,
    MaterialWorldVertexFormat Format,
    string RouteKey,
    IReadOnlyList<GfxSurface> Surfaces);

internal sealed class WorldTexCoordDecoder
{
    private readonly IReadOnlyList<byte> _layer;
    private readonly UvCandidate _candidate;
    private readonly float _maxMagnitude;

    public WorldTexCoordDecoder(IReadOnlyList<byte> layer, UvCandidate candidate, float maxMagnitude)
    {
        _layer = layer;
        _candidate = candidate;
        _maxMagnitude = maxMagnitude;
    }

    public bool TryRead(GfxSurface surface, int surfaceIndex, int globalVertex, out Vec2f texCoord)
    {
        texCoord = default;
        int localVertex = checked(surfaceIndex - SurfaceMinVertexIndex(surface.Triangles));
        if (!_candidate.HasValidIndex(surface.Triangles, localVertex, globalVertex, int.MaxValue))
            return false;

        int offset = _candidate.GetOffset(surface.Triangles, surfaceIndex, localVertex, globalVertex);
        if (offset < 0 || offset + _candidate.ByteCount > _layer.Count)
            return false;

        (float u, float v) = _candidate.Format == UvValueFormat.Float2
            ? (ReadSingle(_layer, offset, _candidate.BigEndian), ReadSingle(_layer, offset + 4, _candidate.BigEndian))
            : (ReadHalf(_layer, offset, _candidate.BigEndian), ReadHalf(_layer, offset + 2, _candidate.BigEndian));
        // Real world UVs can tile well past 4096, but float-max sentinels smear entire surfaces.
        if (!float.IsFinite(u) || !float.IsFinite(v) || MathF.Abs(u) > _maxMagnitude || MathF.Abs(v) > _maxMagnitude)
            return false;

        texCoord = new Vec2f(u, v);
        return true;
    }

    private static float ReadHalf(IReadOnlyList<byte> bytes, int offset, bool bigEndian)
    {
        ushort raw;
        if (bytes is byte[] array)
        {
            raw = bigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan(offset, sizeof(ushort)))
                : BinaryPrimitives.ReadUInt16LittleEndian(array.AsSpan(offset, sizeof(ushort)));
        }
        else
        {
            Span<byte> scratch = stackalloc byte[sizeof(ushort)];
            scratch[0] = bytes[offset];
            scratch[1] = bytes[offset + 1];
            raw = bigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(scratch)
                : BinaryPrimitives.ReadUInt16LittleEndian(scratch);
        }

        return (float)BitConverter.UInt16BitsToHalf(raw);
    }

    private static float ReadSingle(IReadOnlyList<byte> bytes, int offset, bool bigEndian)
    {
        if (bytes is byte[] array)
        {
            return bigEndian
                ? BinaryPrimitives.ReadSingleBigEndian(array.AsSpan(offset, sizeof(float)))
                : BinaryPrimitives.ReadSingleLittleEndian(array.AsSpan(offset, sizeof(float)));
        }

        Span<byte> scratch = stackalloc byte[sizeof(float)];
        for (int i = 0; i < scratch.Length; i++)
            scratch[i] = bytes[offset + i];
        return bigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(scratch)
            : BinaryPrimitives.ReadSingleLittleEndian(scratch);
    }

    private static int SurfaceMinVertexIndex(SrfTriangles triangles)
    {
        return checked((int)triangles.MinVertexIndex);
    }
}

internal readonly record struct UvCandidate(
    int Stride,
    int Offset,
    bool BigEndian,
    int BaseScale,
    UvIndexMode IndexMode,
    UvValueFormat Format = UvValueFormat.Half2)
{
    public static readonly IReadOnlyList<UvCandidate> Candidates = BuildCandidates();

    public int ByteCount => Format == UvValueFormat.Float2 ? 8 : 4;

    public bool IsWorldVertexLayerTexCoord =>
        Stride == 0x1C &&
        Offset == 0x04 &&
        BigEndian &&
        BaseScale == 1 &&
        IndexMode == UvIndexMode.SurfaceIndex &&
        Format == UvValueFormat.Float2;

    public bool IsLayerLocal =>
        BaseScale == 1 &&
        IndexMode == UvIndexMode.Local;

    public bool IsLayerStreamIndexed =>
        BaseScale == 1 &&
        IndexMode == UvIndexMode.SurfaceIndex;

    public bool HasValidIndex(SrfTriangles triangles, int localVertex, int globalVertex, int vertexCount)
    {
        return IndexMode switch
        {
            UvIndexMode.Local => localVertex >= 0 && localVertex < triangles.VertexCount,
            UvIndexMode.GlobalVertex => globalVertex >= 0 && globalVertex < vertexCount,
            _ => true
        };
    }

    public int GetOffset(SrfTriangles triangles, int surfaceIndex, int localVertex, int globalVertex)
    {
        int layerIndex = IndexMode switch
        {
            UvIndexMode.Local => localVertex,
            UvIndexMode.SurfaceIndex => surfaceIndex,
            UvIndexMode.GlobalVertex => globalVertex,
            _ => localVertex
        };

        return checked(triangles.VertexLayerData * BaseScale + layerIndex * Stride + Offset);
    }

    public override string ToString()
    {
        return $"stride={Stride} offset={Offset} endian={(BigEndian ? "BE" : "LE")} baseScale={BaseScale} index={IndexMode} format={Format}";
    }

    private static IReadOnlyList<UvCandidate> BuildCandidates()
    {
        var candidates = new List<UvCandidate>();
        foreach (int stride in new[] { 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64 })
        {
            foreach (int baseScale in new[] { 1 })
            {
                for (int offset = 0; offset <= stride - 4; offset += 2)
                {
                    foreach (UvIndexMode indexMode in new[] { UvIndexMode.Local, UvIndexMode.SurfaceIndex })
                    {
                        candidates.Add(new UvCandidate(stride, offset, true, baseScale, indexMode));
                        candidates.Add(new UvCandidate(stride, offset, false, baseScale, indexMode));
                    }
                }

                for (int offset = 0; offset <= stride - 8; offset += 4)
                {
                    foreach (UvIndexMode indexMode in new[] { UvIndexMode.Local, UvIndexMode.SurfaceIndex })
                    {
                        candidates.Add(new UvCandidate(stride, offset, true, baseScale, indexMode, UvValueFormat.Float2));
                        candidates.Add(new UvCandidate(stride, offset, false, baseScale, indexMode, UvValueFormat.Float2));
                    }
                }
            }
        }

        candidates.Add(new UvCandidate(
            0x1C,
            0x04,
            true,
            1,
            UvIndexMode.SurfaceIndex,
            UvValueFormat.Float2));

        return candidates;
    }
}

internal enum UvIndexMode
{
    Local,
    SurfaceIndex,
    GlobalVertex
}

internal enum UvValueFormat
{
    Half2,
    Float2
}

internal readonly record struct UvDecodeResult(
    UvCandidate Candidate,
    int TotalIndexedVertices,
    int ValidIndexedVertices,
    int BadVertexCount,
    int BadOffsetCount,
    int BadValueCount,
    int Over4096Count,
    float MaxAbsValue,
    int Score);

internal static class Vec3fTupleExtensions
{
    public static (Vec3f Min, Vec3f Max) Normalize(this (Vec3f Min, Vec3f Max) value)
    {
        return (Vec3f.Min(value.Min, value.Max), Vec3f.Max(value.Min, value.Max));
    }
}

internal readonly record struct Rgba(float R, float G, float B, float A);
