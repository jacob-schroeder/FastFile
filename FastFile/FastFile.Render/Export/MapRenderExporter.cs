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
using FastFile.Models.Zone;
using FastFile.Render.Glb;
using ModelVec3 = FastFile.Models.Math.Vec3;

namespace FastFile.Render.Export;

internal sealed class MapRenderExporter
{
    private const int WorldVertexLayerStride = 0x1C;
    private const int WorldVertexLayerTexCoordOffset = 0x04;
    private const int WorldVertexLayerTexCoordSize = 0x08;
    private const float WorldVertexLayerMaxTexCoordMagnitude = 1_000_000f;
    private const float CompactWorldVertexLayerMaxTexCoordMagnitude = 16.0f;
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
        var uvCandidateSurfaces = _options.WriteUvCandidateReports
            ? new Dictionary<WorldUvCandidateGroupKey, List<GfxSurface>>()
            : null;
        var materialUvCandidateGroups = _options.WriteUvCandidateReports
            ? new List<WorldMaterialUvCandidateGroup>()
            : null;
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
            MaterialTechniqueSetAsset? techset = ResolveTechniqueSet(resolvedMaterial);
            TextureBinding? texture = resolvedMaterial is null ? null : BindPreviewColorTexture(builder, resolvedMaterial, techset, summary);
            TextureBinding? layerTexture = resolvedMaterial is null ? null : BindLayerBlendColorTexture(builder, resolvedMaterial, techset, summary);
            materialRows.Add(new MaterialTextureReportRow(group.Key, group.Count(), resolvedMaterial, texture));
            int? textureIndex = texture?.GlbTextureIndex;
            if (techset is not null && _options.WriteUvCandidateReports)
            {
                var candidateGroup = new WorldUvCandidateGroupKey(
                    techset.WorldVertexFormat,
                    TechniqueRoutesText(techset) ?? string.Empty,
                    TechniqueRoutesSemanticText(techset) ?? string.Empty);
                if (!uvCandidateSurfaces!.TryGetValue(candidateGroup, out List<GfxSurface>? formatSurfaces))
                {
                    formatSurfaces = [];
                    uvCandidateSurfaces.Add(candidateGroup, formatSurfaces);
                }

                formatSurfaces.AddRange(surfaces);
                materialUvCandidateGroups!.Add(new WorldMaterialUvCandidateGroup(
                    group.Key,
                    techset.Name ?? string.Empty,
                    techset.WorldVertexFormat,
                    TechniqueRoutesText(techset) ?? string.Empty,
                    surfaces));
            }

            WorldTexCoordDecoder? texCoordDecoder = textureIndex.HasValue
                ? SelectWorldTexCoordDecoder(gfxMap, surfaces, techset?.WorldVertexFormat, texture?.Texture, uvLayoutSamples)
                : null;
            bool usesLayerBlend = layerTexture is { Decoded: true, PngPath: not null };
            WorldTexCoordDecoder? layerTexCoordDecoder = usesLayerBlend
                ? SelectLayerBlendTexCoordDecoder(gfxMap, surfaces, techset?.WorldVertexFormat, uvLayoutSamples)
                : null;
            if (textureIndex.HasValue && texCoordDecoder is null)
                textureIndex = null;
            bool hasTexture = textureIndex.HasValue;
            RenderStateDescriptor renderState = DescribeRenderState(resolvedMaterial, techset);
            WorldLayerColorDecoder? colorDecoder = renderState is { Blend: "additive", Alpha: "intensity", UsesVertexColor: true } || usesLayerBlend
                ? SelectWorldColorDecoder(gfxMap, techset, usesLayerBlend)
                : null;

            var groupPositions = new List<Vec3f>();
            List<Vec2f>? groupTexCoords = hasTexture ? [] : null;
            List<Vec2f>? groupLayerTexCoords = usesLayerBlend ? [] : null;
            List<Rgba>? groupColors = colorDecoder is null ? null : [];
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
                        if (groupLayerTexCoords is not null)
                        {
                            if (layerTexCoordDecoder is not null && layerTexCoordDecoder.TryRead(surface, surfaceIndex, vertex, out Vec2f layerTexCoord))
                                groupLayerTexCoords.Add(layerTexCoord);
                            else
                                groupLayerTexCoords.Add(groupTexCoords is { Count: > 0 } ? groupTexCoords[^1] : default);
                        }
                        if (groupColors is not null)
                            groupColors.Add(colorDecoder!.Read(surface, surfaceIndex));
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
                        layerTexture,
                        texCoordDecoder,
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
            int? texCoord1Accessor = groupLayerTexCoords is null ? null : builder.AddTexCoords(groupLayerTexCoords);
            int? colorAccessor = groupColors is null ? null : builder.AddColors(groupColors);
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
                texCoordAccessor: texCoordAccessor,
                texCoord1Accessor: texCoord1Accessor,
                colorAccessor: colorAccessor);
        }

        builder.AddNode($"{NameOrStem(gfxMap.Name, stem)}_gfx", mesh);
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.gfx.glb");
        builder.Write(outputPath);
        WriteMaterialTextureReport(stem, materialRows, summary);
        WriteMaterialShaderReport(stem, materialRows, summary);
        WriteWorldVertexFormatReport(stem, materialRows, summary);
        if (_options.WriteUvCandidateReports)
        {
            WriteWorldUvCandidateReport(stem, gfxMap, uvCandidateSurfaces!, summary);
            WriteWorldMaterialUvCandidateReport(stem, gfxMap, materialUvCandidateGroups!, summary);
        }
        IReadOnlyList<SkyImageDebugRow> skyImages = ExportSkyImages(gfxMap, summary);
        WriteSurfaceDebugManifest(stem, surfaceDebugRows, skyImages, summary);

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
        IReadOnlyList<SkyImageDebugRow> skyImages,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.surface-debug.json");
        var manifest = new WorldSurfaceDebugManifest(
            stem,
            $"{stem}.gfx.glb",
            $"{stem}.collision-debug.glb",
            skyImages,
            rows);

        using FileStream stream = File.Create(outputPath);
        JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions { WriteIndented = true });
        summary.WrittenFiles.Add(outputPath);
    }

    private IReadOnlyList<SkyImageDebugRow> ExportSkyImages(GfxWorldAsset gfxMap, MapRenderSummary summary)
    {
        var seedImages = new List<GfxImageAsset>();
        AddSkyImage(seedImages, gfxMap.WorldDraw.SkyImage);
        foreach (GfxSky sky in gfxMap.Skies)
            AddSkyImage(seedImages, sky.SkyImage);

        Dictionary<XBlockAddress, byte> samplerBySkyImage = CollectSamplerStatesForImages(gfxMap, seedImages);

        string? prefix = seedImages
            .Select(image => SkyPrefix(image.Name))
            .FirstOrDefault(prefix => !string.IsNullOrWhiteSpace(prefix));
        if (prefix is not null)
        {
            foreach (GfxImageAsset image in _assets.Images.Where(image => image.Name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true))
                AddSkyImage(seedImages, image);
        }

        var rows = new List<SkyImageDebugRow>();
        foreach (GfxImageAsset image in seedImages
            .Where(image => !string.IsNullOrWhiteSpace(image.Name))
            .GroupBy(image => image.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(image => image.Name, StringComparer.OrdinalIgnoreCase))
        {
            string samplerState = image.RuntimeAddress is { } address && samplerBySkyImage.TryGetValue(address, out byte sampler)
                ? $"0x{sampler:X2}"
                : string.Empty;
            if (TryWriteStandalonePng(image, out string relativePath, out string reason, summary))
            {
                rows.Add(new SkyImageDebugRow(image.Name!, SkyFace(image.Name!), samplerState, relativePath, true, string.Empty));
            }
            else
            {
                rows.Add(new SkyImageDebugRow(image.Name!, SkyFace(image.Name!), samplerState, string.Empty, false, reason));
            }
        }

        return rows;
    }

    private Dictionary<XBlockAddress, byte> CollectSamplerStatesForImages(
        GfxWorldAsset gfxMap,
        IReadOnlyList<GfxImageAsset> images)
    {
        var targetImages = images
            .Select(image => image.RuntimeAddress)
            .Where(address => address is not null)
            .Select(address => address!.Value)
            .ToHashSet();
        var result = new Dictionary<XBlockAddress, byte>();
        if (targetImages.Count == 0)
            return result;

        foreach (MaterialAsset material in SurfaceMaterials(gfxMap))
        {
            foreach (MaterialTextureDef texture in material.Textures)
            {
                GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
                if (image?.RuntimeAddress is { } address && targetImages.Contains(address))
                    result.TryAdd(address, texture.SamplerState);
            }
        }

        return result;
    }

    private IEnumerable<MaterialAsset> SurfaceMaterials(GfxWorldAsset gfxMap)
    {
        foreach (MaterialMemory row in gfxMap.MaterialMemory)
        {
            MaterialAsset? material = row.Material ?? _assets.ResolveMaterial(row.MaterialPointer);
            if (material is not null)
                yield return material;
        }

        foreach (GfxSurface surface in gfxMap.Dpvs.Surfaces)
        {
            MaterialAsset? material = ResolveSurfaceMaterial(surface);
            if (material is not null)
                yield return material;
        }
    }

    private static void AddSkyImage(List<GfxImageAsset> images, GfxImageAsset? image)
    {
        if (image is not null)
            images.Add(image);
    }

    private bool TryWriteStandalonePng(
        GfxImageAsset image,
        out string relativePath,
        out string reason,
        MapRenderSummary summary)
    {
        relativePath = string.Empty;
        bool hasPayload = image.PayloadBytes.Count > 0;
        bool resolvedStream = false;
        IReadOnlyList<byte> payload = image.PayloadBytes;
        int width = image.Width;
        int height = image.Height;
        if (!hasPayload)
        {
            resolvedStream = _imageStreams.TryReadBestPayload(image, out byte[] streamPayload, out width, out height, out reason);
            if (resolvedStream)
                payload = streamPayload;
        }
        else
        {
            reason = string.Empty;
        }

        if (!resolvedStream && !hasPayload)
            return false;

        if (!GfxImageDecoder.TryDecodePng(image, payload, width, height, out DecodedGfxImage decoded, out reason))
            return false;

        string textureDirectory = Path.Combine(_options.OutputDirectory, "textures");
        Directory.CreateDirectory(textureDirectory);
        string fileName = $"{SafeFileName(ImageCacheKey(image))}.png";
        File.WriteAllBytes(Path.Combine(textureDirectory, fileName), decoded.PngBytes);
        summary.DecodedTextureCount++;
        summary.TextureOutputDirectory = textureDirectory;
        relativePath = $"textures/{fileName}";
        return true;
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
        TextureBinding? layerTexture,
        WorldTexCoordDecoder? texCoordDecoder,
        int primitiveTriangleStart,
        int primitiveTriangleCount,
        GfxSurface surface)
    {
        SrfTriangles triangles = surface.Triangles;
        IReadOnlyList<UvCandidate> uvCandidates = SelectedWorldUvCandidates(techset?.WorldVertexFormat, texture?.Texture);
        UvCandidate? selectedUv = uvCandidates.Count == 0 ? null : uvCandidates[0];
        int firstSurfaceIndex = triangles.BaseIndex >= 0 && triangles.BaseIndex < gfxMap.WorldDraw.Indices.Count
            ? gfxMap.WorldDraw.Indices[triangles.BaseIndex]
            : -1;
        int firstGlobalVertex = firstSurfaceIndex < 0 ? -1 : triangles.BaseVertex + firstSurfaceIndex;
        int firstLocalVertex = firstSurfaceIndex < 0 ? -1 : firstSurfaceIndex - SurfaceMinVertexIndex(triangles);
        int firstLayerBaseIndex = texCoordDecoder?.LayerBaseIndex(triangles) ?? SurfaceMinVertexIndex(triangles);
        int firstLayerOffset = selectedUv.HasValue && firstSurfaceIndex >= 0
            ? selectedUv.Value.GetOffset(triangles, firstSurfaceIndex, firstLocalVertex, firstLayerBaseIndex, firstGlobalVertex)
            : triangles.VertexLayerData;

        string drawSurfPacked = surfaceIndex < gfxMap.Dpvs.SurfaceMaterials.Count
            ? $"0x{gfxMap.Dpvs.SurfaceMaterials[surfaceIndex].Packed:X16}"
            : string.Empty;
        RenderStateDescriptor renderState = DescribeRenderState(material, techset);

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
            RenderBlend: renderState.Blend,
            RenderAlpha: renderState.Alpha,
            UsesVertexColor: renderState.UsesVertexColor,
            SelectedUvDecoder: selectedUv?.ToString() ?? string.Empty,
            TextureSemantic: texture?.Texture.Semantic.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            TextureSlot: texture is null ? string.Empty : TextureSlotText(texture.Value.Texture),
            TextureSamplerState: texture is null ? string.Empty : $"0x{texture.Value.Texture.SamplerState:X2}",
            TextureImage: texture?.Image.Name ?? string.Empty,
            TextureDecoded: texture?.Decoded ?? false,
            TextureDecodeReason: texture?.DecodeReason ?? string.Empty,
            LayerTextureSlot: layerTexture is null ? string.Empty : TextureSlotText(layerTexture.Value.Texture),
            LayerTextureSamplerState: layerTexture is null ? string.Empty : $"0x{layerTexture.Value.Texture.SamplerState:X2}",
            LayerTextureImage: layerTexture?.Image.Name ?? string.Empty,
            LayerTexturePath: RelativeTexturePath(layerTexture?.PngPath),
            LayerTextureDecoded: layerTexture?.Decoded ?? false,
            LayerTextureDecodeReason: layerTexture?.DecodeReason ?? string.Empty,
            LayerBaseTint: LayerBlendTintText(material, techset, texture?.Texture, layerTexture?.Texture, tintIndex: 0),
            LayerTextureTint: LayerBlendTintText(material, techset, texture?.Texture, layerTexture?.Texture, tintIndex: 1),
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
        MaterialTextureDef? texture,
        List<string> layoutSamples)
    {
        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        if (layer.Count == 0 || vertexCount == 0)
            return null;

        IReadOnlyList<UvCandidate> selectedCandidates = SelectedWorldUvCandidates(worldVertexFormat, texture);
        if (selectedCandidates.Count == 0)
        {
            if (layoutSamples.Count < 12)
                layoutSamples.Add($"{worldVertexFormat?.ToString() ?? "unknown"}:unproven");
            return null;
        }

        IReadOnlyDictionary<LayerStreamKey, int> layerBaseIndices = BuildLayerBaseIndices(gfxMap.Dpvs.Surfaces);
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
                return new WorldTexCoordDecoder(layer, layerBaseIndices, candidate, MaxTexCoordMagnitude(candidate));
            }
        }

        if (layoutSamples.Count < 12)
        {
            UvDecodeResult failed = best!.Value;
            layoutSamples.Add(
                $"{worldVertexFormat?.ToString() ?? "unknown"}:{failed.Candidate} " +
                $"failed {failed.ValidIndexedVertices}/{failed.TotalIndexedVertices}");
        }
        if (best is { } candidateBest &&
            candidateBest.TotalIndexedVertices > 0 &&
            candidateBest.ValidIndexedVertices / (float)candidateBest.TotalIndexedVertices >= 1.0f - MaxTexturedUvFailureRatio)
            return new WorldTexCoordDecoder(layer, layerBaseIndices, candidateBest.Candidate, MaxTexCoordMagnitude(candidateBest.Candidate));
        return null;
    }

    private static IReadOnlyList<UvCandidate> SelectedWorldUvCandidates(
        MaterialWorldVertexFormat? worldVertexFormat,
        MaterialTextureDef? texture = null)
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

            // TEX_2_NRM_2 interleaves paired blend data in a 0x28-byte
            // logical vertex. The decoded RSX fragment program samples c1/n1/s1
            // from texcoord6.zw, which lands at +0x18/+0x1C in this record.
            MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_2 =>
                SelectedTex2Nrm2UvCandidates(),

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

    private static IReadOnlyList<UvCandidate> SelectedTex2Nrm2UvCandidates()
    {
        return
        [
            new UvCandidate(0x28, 0x04, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2)
        ];
    }

    private static WorldTexCoordDecoder? SelectLayerBlendTexCoordDecoder(
        GfxWorldAsset gfxMap,
        IReadOnlyList<GfxSurface> surfaces,
        MaterialWorldVertexFormat? worldVertexFormat,
        List<string> layoutSamples)
    {
        if (worldVertexFormat != MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_2)
            return null;

        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        var candidate = new UvCandidate(0x28, 0x18, true, 1, UvIndexMode.SurfaceIndex, UvValueFormat.Float2);
        UvDecodeResult result = ScoreWorldTexCoordCandidate(gfxMap, candidate, vertexCount, surfaces);
        if (result.TotalIndexedVertices > 0 &&
            result.ValidIndexedVertices / (float)result.TotalIndexedVertices >= 1.0f - MaxTexturedUvFailureRatio)
            return new WorldTexCoordDecoder(layer, BuildLayerBaseIndices(gfxMap.Dpvs.Surfaces), candidate, MaxTexCoordMagnitude(candidate));

        if (layoutSamples.Count < 12)
            layoutSamples.Add($"{worldVertexFormat}:layer:{candidate} failed {result.ValidIndexedVertices}/{result.TotalIndexedVertices}");
        return null;
    }

    private static WorldLayerColorDecoder? SelectWorldColorDecoder(
        GfxWorldAsset gfxMap,
        MaterialTechniqueSetAsset? techset,
        bool usesLayerBlend)
    {
        if (techset?.WorldVertexFormat == MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_1_NRM_1)
        {
            return new WorldLayerColorDecoder(
                gfxMap.WorldDraw.VertexLayerData.PackedLayerData,
                BuildLayerBaseIndices(gfxMap.Dpvs.Surfaces),
                WorldVertexLayerStride,
                0x18,
                WorldColorPacking.RawRgba,
                invert: false);
        }

        if (usesLayerBlend && techset?.WorldVertexFormat == MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_2)
        {
            return new WorldLayerColorDecoder(
                gfxMap.WorldDraw.VertexLayerData.PackedLayerData,
                BuildLayerBaseIndices(gfxMap.Dpvs.Surfaces),
                0x28,
                0x00,
                WorldColorPacking.D3DArgb,
                invert: false);
        }

        return null;
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
        float maxMagnitude = MaxTexCoordMagnitude(candidate);
        Dictionary<LayerStreamKey, int> layerBaseIndices = BuildLayerBaseIndices(gfxMap.Dpvs.Surfaces);

        foreach (GfxSurface surface in surfaces ?? gfxMap.Dpvs.Surfaces)
        {
            SrfTriangles triangles = surface.Triangles;
            int layerBaseIndex = LayerBaseIndex(layerBaseIndices, triangles);
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

                if (!candidate.HasValidIndex(triangles, surfaceIndex, local, layerBaseIndex, vertex, vertexCount))
                {
                    badVertex++;
                    continue;
                }

                int offset = candidate.GetOffset(triangles, surfaceIndex, local, layerBaseIndex, vertex);
                if (offset < 0 || offset + candidate.ByteCount > layer.Count)
                {
                    badOffset++;
                    continue;
                }

                (float u, float v) = candidate.Format == UvValueFormat.Float2
                    ? (ReadSingle(layer, offset, candidate.BigEndian), ReadSingle(layer, offset + 4, candidate.BigEndian))
                    : (ReadHalf(layer, offset, candidate.BigEndian), ReadHalf(layer, offset + 2, candidate.BigEndian));
                float candidateMaxAbs = MathF.Max(MathF.Abs(u), MathF.Abs(v));
                if (!float.IsFinite(u) || !float.IsFinite(v) || candidateMaxAbs > maxMagnitude)
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

    private static float MaxTexCoordMagnitude(UvCandidate candidate)
    {
        return candidate is { Stride: 0x14, IndexMode: UvIndexMode.LayerData, Format: UvValueFormat.Float2 }
            ? CompactWorldVertexLayerMaxTexCoordMagnitude
            : WorldVertexLayerMaxTexCoordMagnitude;
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

    private static Dictionary<LayerStreamKey, int> BuildLayerBaseIndices(IEnumerable<GfxSurface> surfaces)
    {
        var result = new Dictionary<LayerStreamKey, int>();
        foreach (GfxSurface surface in surfaces)
        {
            SrfTriangles triangles = surface.Triangles;
            var key = new LayerStreamKey(triangles.VertexLayerData, triangles.BaseVertex);
            int minVertexIndex = SurfaceMinVertexIndex(triangles);
            if (!result.TryGetValue(key, out int current) || minVertexIndex < current)
                result[key] = minVertexIndex;
        }

        return result;
    }

    private static int LayerBaseIndex(
        IReadOnlyDictionary<LayerStreamKey, int> layerBaseIndices,
        SrfTriangles triangles)
    {
        return layerBaseIndices.TryGetValue(new LayerStreamKey(triangles.VertexLayerData, triangles.BaseVertex), out int baseIndex)
            ? baseIndex
            : SurfaceMinVertexIndex(triangles);
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

    private TextureBinding? BindPreviewColorTexture(
        GlbSceneBuilder builder,
        MaterialAsset material,
        MaterialTechniqueSetAsset? techset,
        MapRenderSummary summary)
    {
        (MaterialTextureDef Texture, GfxImageAsset Image)? selected = SelectPreviewColorTexture(material, techset);
        if (selected is not { } binding)
        {
            summary.TextureDecodeSkippedCount++;
            return null;
        }

        return BindTexture(builder, binding.Texture, binding.Image, summary);
    }

    private TextureBinding? BindLayerBlendColorTexture(
        GlbSceneBuilder builder,
        MaterialAsset material,
        MaterialTechniqueSetAsset? techset,
        MapRenderSummary summary)
    {
        if (techset?.WorldVertexFormat != MaterialWorldVertexFormat.MTL_WORLDVERT_TEX_2_NRM_2)
            return null;

        if (SelectLayerBlendColorTexture(material, techset) is { } shaderBinding)
            return BindTexture(builder, shaderBinding.Texture, shaderBinding.Image, summary);

        foreach (MaterialTextureDef texture in material.Textures)
        {
            if (!IsNumberedColorTexture(texture))
                continue;

            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            if (image is not null)
                return BindTexture(builder, texture, image, summary);
        }

        return null;
    }

    private TextureBinding BindTexture(
        GlbSceneBuilder builder,
        MaterialTextureDef texture,
        GfxImageAsset image,
        MapRenderSummary summary)
    {
        string cacheKey = TextureCacheKey(texture, image);
        if (_textureByImageAddress.TryGetValue(cacheKey, out CachedTexture existingTexture))
            return new TextureBinding(texture, image, existingTexture.GlbTextureIndex, existingTexture.PngPath, true, null, existingTexture.HasTransparency);

        bool hasPayload = image.PayloadBytes.Count > 0;
        bool resolvedStream = false;
        IReadOnlyList<byte> payload = image.PayloadBytes;
        int width = image.Width;
        int height = image.Height;
        string reason;
        if (!hasPayload)
        {
            resolvedStream = _imageStreams.TryReadBestPayload(image, out byte[] streamPayload, out width, out height, out reason);
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
            return new TextureBinding(texture, image, null, null, false, reason, false);
        }

        if (!GfxImageDecoder.TryDecodePng(image, payload, width, height, out DecodedGfxImage decoded, out reason))
        {
            summary.TextureDecodeSkippedCount++;
            return new TextureBinding(texture, image, null, null, false, reason, false);
        }

        string textureDirectory = Path.Combine(_options.OutputDirectory, "textures");
        Directory.CreateDirectory(textureDirectory);
        string pngPath = Path.Combine(textureDirectory, $"{SafeFileName(cacheKey)}.png");
        File.WriteAllBytes(pngPath, decoded.PngBytes);
        int glbTexture = builder.AddPngTexture(decoded.Name, decoded.PngBytes, texture.SamplerState);
        _textureByImageAddress[cacheKey] = new CachedTexture(glbTexture, pngPath, decoded.HasTransparency);
        summary.DecodedTextureCount++;
        summary.TextureOutputDirectory = textureDirectory;
        return new TextureBinding(texture, image, glbTexture, pngPath, true, null, decoded.HasTransparency);
    }

    private (MaterialTextureDef Texture, GfxImageAsset Image)? SelectPreviewColorTexture(
        MaterialAsset material,
        MaterialTechniqueSetAsset? techset)
    {
        IReadOnlyList<(MaterialTextureDef Texture, GfxImageAsset Image)> shaderColorTextures =
            SelectShaderTextures(material, techset, semantic: 0x02);
        if (shaderColorTextures.Count > 0)
            return shaderColorTextures[0];

        int colorTextureCount = material.Textures.Count(texture => texture.Semantic == 0x02);
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
            if (colorTextureCount > 1 && IsNumberedColorTexture(texture))
                continue;

            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            if (image is not null)
                return (texture, image);
        }

        (MaterialTextureDef Texture, GfxImageAsset Image)? best = null;
        int bestScore = int.MinValue;
        foreach (MaterialTextureDef texture in material.Textures)
        {
            if (colorTextureCount > 1 && IsNumberedColorTexture(texture))
                continue;
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

    private (MaterialTextureDef Texture, GfxImageAsset Image)? SelectLayerBlendColorTexture(
        MaterialAsset material,
        MaterialTechniqueSetAsset? techset)
    {
        IReadOnlyList<(MaterialTextureDef Texture, GfxImageAsset Image)> shaderColorTextures =
            SelectShaderTextures(material, techset, semantic: 0x02);
        return shaderColorTextures.Count > 1
            ? shaderColorTextures[1]
            : null;
    }

    private IReadOnlyList<(MaterialTextureDef Texture, GfxImageAsset Image)> SelectShaderTextures(
        MaterialAsset material,
        MaterialTechniqueSetAsset? techset,
        byte semantic)
    {
        if (techset is null)
            return [];

        var texturesByHash = material.Textures
            .Where(texture => texture.Semantic == semantic)
            .GroupBy(texture => texture.NameHash)
            .ToDictionary(group => group.Key, group => group.First());
        var result = new List<(MaterialTextureDef Texture, GfxImageAsset Image)>();
        var seen = new HashSet<uint>();

        foreach (MaterialShaderArgumentAsset arg in techset.TechniqueSlots
            .Where(slot => slot.Technique is not null)
            .SelectMany(slot => slot.Technique!.Passes)
            .SelectMany(pass => pass.Args))
        {
            if (arg.Type != MaterialShaderArgumentType.MaterialPixelSampler ||
                !texturesByHash.TryGetValue(unchecked((uint)arg.ArgumentRaw), out MaterialTextureDef? texture))
                continue;
            if (!seen.Add(texture.NameHash))
                continue;

            GfxImageAsset? image = texture.Image ?? _assets.ResolveImage(texture.DataPointer);
            if (image is not null)
                result.Add((texture, image));
        }

        return result;
    }

    private static string LayerBlendTintText(
        MaterialAsset? material,
        MaterialTechniqueSetAsset? techset,
        MaterialTextureDef? baseTexture,
        MaterialTextureDef? layerTexture,
        int tintIndex)
    {
        if (material is null || techset is null || baseTexture is null || layerTexture is null)
            return string.Empty;

        MaterialPassAsset? pass = FindPassBindingTextures(techset, baseTexture.NameHash, layerTexture.NameHash);
        if (pass is null)
            return string.Empty;

        string targetName = tintIndex == 0 ? "colorTint" : "colorTint1";
        Dictionary<uint, MaterialConstantDef> constantsByHash = material.Constants
            .GroupBy(constant => constant.NameHash)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (MaterialShaderArgumentAsset arg in pass.Args)
        {
            if (arg.Type != MaterialShaderArgumentType.MaterialPixelConst ||
                !constantsByHash.TryGetValue(unchecked((uint)arg.ArgumentRaw), out MaterialConstantDef? constant) ||
                !string.Equals(ConstantName(constant), targetName, StringComparison.Ordinal))
                continue;

            return Vec4Text(constant.Literal);
        }

        return string.Empty;
    }

    private static MaterialPassAsset? FindPassBindingTextures(
        MaterialTechniqueSetAsset techset,
        uint baseTextureHash,
        uint layerTextureHash)
    {
        foreach (MaterialPassAsset pass in techset.TechniqueSlots
            .Where(slot => slot.Technique is not null)
            .SelectMany(slot => slot.Technique!.Passes))
        {
            bool hasBase = false;
            bool hasLayer = false;
            foreach (MaterialShaderArgumentAsset arg in pass.Args)
            {
                if (arg.Type != MaterialShaderArgumentType.MaterialPixelSampler)
                    continue;

                uint hash = unchecked((uint)arg.ArgumentRaw);
                hasBase |= hash == baseTextureHash;
                hasLayer |= hash == layerTextureHash;
            }

            if (hasBase && hasLayer)
                return pass;
        }

        return null;
    }

    private static string Vec4Text(MaterialVec4 value)
    {
        return string.Join(
            " ",
            value.X.ToString("R", CultureInfo.InvariantCulture),
            value.Y.ToString("R", CultureInfo.InvariantCulture),
            value.Z.ToString("R", CultureInfo.InvariantCulture),
            value.W.ToString("R", CultureInfo.InvariantCulture));
    }

    private static string ConstantName(MaterialConstantDef constant)
    {
        int length = constant.NameBytes.TakeWhile(value => value != 0).Count();
        return Encoding.ASCII.GetString(constant.NameBytes.Take(length).ToArray());
    }

    private static bool IsPrimaryColorTexture(MaterialTextureDef texture)
    {
        return texture.Semantic == 0x02 &&
            texture.NameStart == (byte)'c' &&
            texture.NameEnd == (byte)'p';
    }

    private static bool IsNumberedColorTexture(MaterialTextureDef texture)
    {
        return texture.Semantic == 0x02 &&
            texture.NameStart == (byte)'c' &&
            texture.NameEnd >= (byte)'0' &&
            texture.NameEnd <= (byte)'9';
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
        return $"{name}_{image.Format:X2}_{image.BaseWidth}x{image.BaseHeight}_{texture.SamplerState:X2}";
    }

    private static string ImageCacheKey(GfxImageAsset image)
    {
        string name = string.IsNullOrWhiteSpace(image.Name)
            ? "image"
            : image.Name!;
        return $"{name}_{image.Format:X2}_{image.BaseWidth}x{image.BaseHeight}";
    }

    private string RelativeTexturePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetRelativePath(_options.OutputDirectory, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string? SkyPrefix(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (string suffix in new[] { "_ft", "_bk", "_lf", "_rt", "_up", "_dn" })
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name[..^suffix.Length];
        }

        return name;
    }

    private static string SkyFace(string name)
    {
        foreach (string suffix in new[] { "ft", "bk", "lf", "rt", "up", "dn" })
        {
            if (name.EndsWith($"_{suffix}", StringComparison.OrdinalIgnoreCase))
                return suffix;
        }

        return string.Empty;
    }

    private static string TextureSlotText(MaterialTextureDef texture)
    {
        return $"{TextureNameByteText(texture.NameStart)}-{TextureNameByteText(texture.NameEnd)}";
    }

    private static string TextureNameByteText(byte value)
    {
        return value >= 0x20 && value <= 0x7E
            ? ((char)value).ToString()
            : $"0x{value:X2}";
    }

    private void WriteMaterialTextureReport(
        string stem,
        IReadOnlyList<MaterialTextureReportRow> rows,
        MapRenderSummary summary)
    {
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.material-textures.csv");
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine("materialName,surfaceCount,resolvedMaterial,cameraRegion,techsetName,worldVertexFormat,techsetPasses,stateBits,textureCount,textures,constantCount,constants,selectedTextureSemantic,selectedTextureSlot,imageName,width,height,baseWidth,baseHeight,format,payloadBytes,streamData,decoded,hasTransparency,pngPath,decodeReason");
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
                Csv(row.Material is null ? null : StateBitsText(row.Material)),
                row.Material?.Textures.Count.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(row.Material is null ? null : TextureListText(row.Material)),
                row.Material?.Constants.Count.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(row.Material is null ? null : ConstantListText(row.Material)),
                texture?.Texture.Semantic.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                texture is null ? string.Empty : TextureSlotText(texture.Value.Texture),
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

    private void WriteMaterialShaderReport(
        string stem,
        IReadOnlyList<MaterialTextureReportRow> rows,
        MapRenderSummary summary)
    {
        string shaderDirectory = Path.Combine(_options.OutputDirectory, "shaders");
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.material-shaders.csv");
        var writtenShaders = new HashSet<string>(StringComparer.Ordinal);
        using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
        writer.WriteLine("materialName,techsetName,techniqueSlot,techniqueName,passIndex,shaderKind,shaderName,dataSize,bytecodePath,rsxReportPath,liveRenderSafe,bytecodePrefix,bytecodeHeader,shaderProgramRoot,shaderSymbols,rsxDecode,vertexDecl,backendStreamVertexInputProof,argRegisterContract,args");

        foreach (MaterialTextureReportRow row in rows)
        {
            MaterialTechniqueSetAsset? techset = ResolveTechniqueSet(row.Material);
            if (techset is null)
                continue;

            foreach (MaterialTechniqueSlot slot in techset.TechniqueSlots.Where(slot => slot.Technique is not null))
            {
                for (int passIndex = 0; passIndex < slot.Technique!.Passes.Count; passIndex++)
                {
                    MaterialPassAsset pass = slot.Technique.Passes[passIndex];
                    MaterialVertexDeclarationAsset? decl = pass.VertexDeclaration ?? _assets.ResolveVertexDeclaration(pass.VertexDeclPointer);
                    string vertexDecl = decl is null
                        ? string.Empty
                        : TechniqueRouteText(decl, includeSemanticNames: true);
                    string args = ShaderArgsText(pass, row.Material);
                    string argContract = ShaderArgRegisterContractText(pass, row.Material);
                    WriteShaderReportRow(writer, shaderDirectory, writtenShaders, row.MaterialName, row.Material, techset, slot, passIndex, pass, pass.VertexShader, decl, vertexDecl, argContract, args);
                    WriteShaderReportRow(writer, shaderDirectory, writtenShaders, row.MaterialName, row.Material, techset, slot, passIndex, pass, pass.PixelShader, decl, vertexDecl, argContract, args);
                }
            }
        }

        summary.WrittenFiles.Add(outputPath);
    }

    private static void WriteShaderReportRow(
        StreamWriter writer,
        string shaderDirectory,
        HashSet<string> writtenShaders,
        string materialName,
        MaterialAsset? material,
        MaterialTechniqueSetAsset techset,
        MaterialTechniqueSlot slot,
        int passIndex,
        MaterialPassAsset pass,
        MaterialShaderAsset? shader,
        MaterialVertexDeclarationAsset? decl,
        string vertexDecl,
        string argContract,
        string args)
    {
        if (shader is null)
            return;

        string bytecodePath = string.Empty;
        string rsxReportPath = string.Empty;
        string liveRenderSafe = string.Empty;
        string backendProof = decl is null ? string.Empty : BackendStreamVertexInputProofText(techset, decl);
        byte[]? data = shader.Data;
        if (data is { Length: > 0 })
        {
            Directory.CreateDirectory(shaderDirectory);
            string fileName = $"{SafeFileName($"{shader.Kind}_{shader.Name}_{shader.DataSize:X}_{Fnv1A32(data):X8}")}.bin";
            string absolutePath = Path.Combine(shaderDirectory, fileName);
            if (writtenShaders.Add(absolutePath))
                File.WriteAllBytes(absolutePath, data);
            bytecodePath = Path.Combine("shaders", fileName).Replace('\\', '/');

            if (shader.Kind == MaterialShaderKind.Pixel && TryBuildRsxFragmentReport(data, pass, material, out string report, out bool safeForLive))
            {
                string reportFileName = Path.ChangeExtension(fileName, ".rsx.txt");
                string reportAbsolutePath = Path.Combine(shaderDirectory, reportFileName);
                if (writtenShaders.Add(reportAbsolutePath))
                    File.WriteAllText(reportAbsolutePath, report, Encoding.UTF8);
                rsxReportPath = Path.Combine("shaders", reportFileName).Replace('\\', '/');
                liveRenderSafe = safeForLive.ToString(CultureInfo.InvariantCulture);
            }
            else if (shader.Kind == MaterialShaderKind.Vertex && TryBuildRsxVertexReport(data, decl, out report))
            {
                string reportFileName = Path.ChangeExtension(fileName, ".rsx.txt");
                string reportAbsolutePath = Path.Combine(shaderDirectory, reportFileName);
                if (writtenShaders.Add(reportAbsolutePath))
                    File.WriteAllText(reportAbsolutePath, report, Encoding.UTF8);
                rsxReportPath = Path.Combine("shaders", reportFileName).Replace('\\', '/');
            }
        }

        writer.WriteLine(string.Join(
            ",",
            Csv(materialName),
            Csv(techset.Name),
            slot.Index.ToString(CultureInfo.InvariantCulture),
            Csv(slot.Technique?.Name),
            passIndex.ToString(CultureInfo.InvariantCulture),
            shader.Kind.ToString(),
            Csv(shader.Name),
            shader.DataSize.ToString(CultureInfo.InvariantCulture),
            Csv(bytecodePath),
            Csv(rsxReportPath),
            Csv(liveRenderSafe),
            Csv(data is { Length: > 0 } ? HexBytes(data, Math.Min(64, data.Length)) : string.Empty),
            Csv(data is { Length: > 0 } ? ShaderHeaderText(data) : string.Empty),
            Csv(ShaderProgramRootText(shader)),
            Csv(data is { Length: > 0 } ? ShaderSymbolsText(data) : string.Empty),
            Csv(data is { Length: > 0 } ? RsxShaderDecodeText(shader.Kind, data) : string.Empty),
            Csv(vertexDecl),
            Csv(backendProof),
            Csv(argContract),
            Csv(args)));
    }

    private static string ShaderProgramRootText(MaterialShaderAsset shader)
    {
        if (shader.Kind != MaterialShaderKind.Pixel || shader.ProgramBytes.Length == 0)
            return string.Empty;

        string bytes = HexBytes(shader.ProgramBytes, shader.ProgramBytes.Length);
        string serializedTail = string.Empty;
        if (shader.ProgramBytes.Length >= 0x0c)
        {
            uint root0C = BinaryPrimitives.ReadUInt32BigEndian(shader.ProgramBytes.AsSpan(0, 4));
            ushort root12 = BinaryPrimitives.ReadUInt16BigEndian(shader.ProgramBytes.AsSpan(6, 2));
            uint root14 = BinaryPrimitives.ReadUInt32BigEndian(shader.ProgramBytes.AsSpan(8, 4));
            serializedTail = $" serializedRoot+0x0c=0x{root0C:X8} serializedRoot+0x12=0x{root12:X4} serializedRoot+0x14=0x{root14:X8}";
        }

        string runtime = "runtimeRoot=UNMODELED";
        if (shader.Data is { Length: > 0 } data &&
            TryReadRsxShaderRuntimeInfo(data, out RsxShaderRuntimeInfo runtimeInfo))
        {
            runtime = $"runtimeRoot=REPORT_ONLY +0x04=patchTablePtr +0x08=commandListOffset +0x0c=0x{RsxCopiedCommandDwordCount(data, runtimeInfo):X} +0x10=patchTableHalfwords +0x12=0x{runtimeInfo.UploadSize:X} +0x14=raw+0x{runtimeInfo.UploadOffset:X}";
        }

        return $"serializedRootTail=0x{bytes}{serializedTail} {runtime} evidence=EBOOT.elf:0xfb128 loads the 0x14 MaterialPixelShaderProgram root; 0xfaff8 loads the 0x08 GfxPixelShaderLoadDef and calls 0x004c7428->0x00361f38; 0x00361f38 writes runtime fields +0x04/+0x08/+0x0c/+0x10/+0x12/+0x14; default_mp.elf:00396e48 consumes runtime +0x04/+0x12/+0x14 during fragment upload";
    }

    private static string ShaderArgRegisterContractText(MaterialPassAsset pass, MaterialAsset? material)
    {
        Dictionary<uint, MaterialTextureDef> texturesByHash = material?.Textures
            .GroupBy(texture => texture.NameHash)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];
        Dictionary<uint, MaterialConstantDef> constantsByHash = material?.Constants
            .GroupBy(constant => constant.NameHash)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        var parts = new List<string>(pass.Args.Count + 1)
        {
            "PS3_PROVEN default_mp.elf:003ac268 applies types0-4, 003a7738 bundles type5, 003a7608 bundles types6-7, 00396e48 patches fragment constants into the pixel shader upload buffer"
        };

        for (int i = 0; i < pass.Args.Count; i++)
        {
            MaterialShaderArgumentAsset arg = pass.Args[i];
            uint raw = unchecked((uint)arg.ArgumentRaw);
            string partition = ArgPartition(i, pass);
            string prefix = $"#{i}:{partition}:type={(ushort)arg.Type}/0x{(ushort)arg.Type:X4}:dest=0x{arg.Dest:X4}:raw=0x{raw:X8}:";
            parts.Add(prefix + (arg.Type switch
            {
                MaterialShaderArgumentType.MaterialVertexConst =>
                    $"rsxVertexConst[{arg.Dest}] <- material.constants[hash=0x{raw:X8}]+0x10 via method 0x1efc {MaterialConstantText(constantsByHash, raw)}",
                MaterialShaderArgumentType.LiteralVertexConst =>
                    $"rsxVertexConst[{arg.Dest}] <- literal float4 via method 0x1efc {LiteralConstantText(arg.LiteralConstant)}",
                MaterialShaderArgumentType.MaterialPixelSampler =>
                    $"rsxTextureUnit[{arg.Dest}] <- material.textures[hash=0x{raw:X8}] via FUN_00376620 methods 0x1a00/0x1a18/0x1840/0x1a10 + unit*stride {MaterialTextureText(texturesByHash, raw)}",
                MaterialShaderArgumentType.CodeVertexConst =>
                    $"rsxVertexConst[{arg.Dest}..+{Math.Max(0, CodeConstVectorCount(raw) - 1)}] <- codeConst[index=0x{CodeConstIndex(raw):X4},count={CodeConstVectorCount(raw)}] via method 0x1efc",
                MaterialShaderArgumentType.CodePixelSampler =>
                    $"rsxTextureUnit[{arg.Dest}] <- codeTexture[index=0x{raw:X8}] texture=context+0x12b0+index*4 sampler=context+0x131c+index via FUN_00375db8/FUN_00376620",
                MaterialShaderArgumentType.CodePixelConst =>
                    $"rsxFragmentConstPatchDest[{arg.Dest}] <- codeConst[index=0x{CodeConstIndex(raw):X4}] from context+0xe00+index*0x10; 00396e48 patches every shader patch-list entry for this dest",
                MaterialShaderArgumentType.MaterialPixelConst =>
                    $"rsxFragmentConstPatchDest[{arg.Dest}] <- material.constants[hash=0x{raw:X8}]+0x10; 00396e48 patches every shader patch-list entry for this dest {MaterialConstantText(constantsByHash, raw)}",
                MaterialShaderArgumentType.LiteralPixelConst =>
                    $"rsxFragmentConstPatchDest[{arg.Dest}] <- literal float4; 00396e48 patches every shader patch-list entry for this dest {LiteralConstantText(arg.LiteralConstant)}",
                _ => "unhandled arg type in C# enum"
            }));
        }

        return string.Join(" | ", parts);
    }

    private static string ArgPartition(int index, MaterialPassAsset pass)
    {
        int perObjStart = pass.PerPrimArgCount;
        int stableStart = pass.PerPrimArgCount + pass.PerObjArgCount;
        if (index < perObjStart)
            return "perPrim";
        return index < stableStart ? "perObj" : "stable";
    }

    private static int CodeConstIndex(uint raw) => (int)((raw >> 16) & 0xffff);

    private static int CodeConstVectorCount(uint raw) => (int)(raw & 0xff);

    private static string MaterialTextureText(Dictionary<uint, MaterialTextureDef> texturesByHash, uint hash)
    {
        if (!texturesByHash.TryGetValue(hash, out MaterialTextureDef? texture))
            return string.Empty;

        string imageName = texture.Image?.Name ?? "<null-image>";
        return $"slot={TextureSlotText(texture)} sampler=0x{texture.SamplerState:X2} image={imageName}";
    }

    private static string MaterialConstantText(Dictionary<uint, MaterialConstantDef> constantsByHash, uint hash)
    {
        return constantsByHash.TryGetValue(hash, out MaterialConstantDef? constant)
            ? $"const={ConstantName(constant)}({Vec4Text(constant.Literal).Replace(' ', '/')})"
            : string.Empty;
    }

    private static string LiteralConstantText(MaterialShaderLiteralConstant? literal)
    {
        return literal is { } value
            ? $"literal=({value.X.ToString("R", CultureInfo.InvariantCulture)}/{value.Y.ToString("R", CultureInfo.InvariantCulture)}/{value.Z.ToString("R", CultureInfo.InvariantCulture)}/{value.W.ToString("R", CultureInfo.InvariantCulture)})"
            : "literal=<offset-or-null>";
    }

    private static string ShaderHeaderText(byte[] data)
    {
        int count = Math.Min(data.Length / 4, 16);
        return string.Join(" ", Enumerable.Range(0, count)
            .Select(index => $"0x{BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(index * 4, 4)):X8}"));
    }

    private static string ShaderSymbolsText(byte[] data)
    {
        var symbols = new List<string>();
        int start = -1;
        for (int i = 0; i <= data.Length; i++)
        {
            bool printable = i < data.Length && data[i] >= 0x20 && data[i] <= 0x7E;
            if (printable)
            {
                if (start < 0)
                    start = i;
                continue;
            }

            if (start >= 0 && i - start >= 4)
                symbols.Add(Encoding.ASCII.GetString(data, start, i - start));
            start = -1;
        }

        return string.Join(" ", symbols.Distinct(StringComparer.Ordinal).Take(96));
    }

    private static string RsxShaderDecodeText(MaterialShaderKind kind, byte[] data)
    {
        int offset = RsxShaderUploadOffset(data);
        if (offset < 0)
            return string.Empty;

        return kind == MaterialShaderKind.Pixel
            ? RsxFragmentDecodeText(data, offset)
            : RsxVertexDecodeText(data, offset);
    }

    private static bool TryBuildRsxFragmentReport(byte[] data, MaterialPassAsset pass, MaterialAsset? material, out string report, out bool safeForLive)
    {
        report = string.Empty;
        safeForLive = false;
        int offset = RsxShaderUploadOffset(data);
        if (offset < 0)
            return false;

        List<RsxFragmentInstruction> instructions = DecodeRsxFragmentInstructions(data, offset, maxInstructions: 512);
        if (instructions.Count == 0)
            return false;
        TryReadRsxShaderRuntimeInfo(data, out RsxShaderRuntimeInfo runtimeInfo);
        TryBuildRsxRuntimeUploadModel(data, pass, material, runtimeInfo, out RsxRuntimeUploadModel uploadModel);

        var opCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var unresolved = new SortedSet<string>(StringComparer.Ordinal);
        var textureOps = new List<string>();
        foreach (RsxFragmentInstruction instruction in instructions)
        {
            opCounts[instruction.OpName] = opCounts.TryGetValue(instruction.OpName, out int count) ? count + 1 : 1;
            AddRsxFragmentUnresolvedNotes(instruction, unresolved);
            if (instruction.IsTexture)
            {
                textureOps.Add($"{instruction.Index}@0x{instruction.Offset:X}:{instruction.OpName} unit={instruction.TextureUnit} input={RsxFragmentInputName(instruction.SourceAttribute)} dest={RsxFragmentDestinationText(instruction)}");
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine("SAFE_FOR_LIVE_RENDERING=false");
        builder.AppendLine("reason=decoded RSX fragment report only; no GLSL is emitted or wired into viewer.html until every opcode, control-flow, output, scale, and patch-table store semantic is proven");
        builder.AppendLine("evidence=default_mp.elf:00396e48 consumes pixel shader data+0x04/+0x12/+0x14 and applies type5/type6/type7 fragment constant patch lists; PSL1GHT nvfx_shader.h documents the RSX fragment instruction bit fields used here");
        builder.AppendLine($"dataSize=0x{data.Length:X}");
        builder.AppendLine($"headerDwords={ShaderHeaderText(data)}");
        builder.AppendLine($"runtimeModel={RsxRuntimeModelText(data, runtimeInfo)}");
        builder.AppendLine($"uploadCodeOffset=0x{offset:X}");
        builder.AppendLine($"instructionCount={instructions.Count}");
        builder.AppendLine($"ops={string.Join(" ", opCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"))}");
        builder.AppendLine($"textureOps={string.Join(" | ", textureOps)}");
        builder.AppendLine($"fragmentCcSemantics={RsxFragmentCcSemanticsText()}");
        builder.AppendLine($"fragmentInterpolationContract={RsxFragmentInterpolationContractText(instructions)}");
        builder.AppendLine($"fragmentConstPatchDests={RsxFragmentPatchTableText(pass, data, runtimeInfo)}");
        builder.AppendLine($"runtimeUploadCode={RsxRuntimeUploadModelText(uploadModel)}");
        builder.AppendLine($"unresolved={string.Join(" | ", unresolved)}");
        builder.AppendLine("instructions:");
        foreach (RsxFragmentInstruction instruction in instructions)
            builder.AppendLine(RsxFragmentInstructionDetailText(instruction));

        report = builder.ToString();
        return true;
    }

    private static void AddRsxFragmentUnresolvedNotes(RsxFragmentInstruction instruction, SortedSet<string> unresolved)
    {
        if (instruction.Branch)
            unresolved.Add("branch/control-flow translation is decoded but not lowered to GLSL");
        if (!instruction.Branch && instruction.Scale == 4)
            unresolved.Add("scale code 4 is not defined by the RSX reference used here");
        if (!instruction.Branch &&
            (instruction.NoDest || instruction.WriteMask == 0) &&
            !instruction.CondWriteEnabled &&
            !IsRsxFragmentReservedNoOp(instruction))
        {
            unresolved.Add("no-destination/write-mask instructions must be classified as control or side-effect ops before live rendering");
        }
        if (!instruction.Branch && instruction.Opcode is 0x13 or 0x14 or 0x19 or 0x24 or 0x25 or 0x27 or 0x28 or 0x29 or 0x2a)
            unresolved.Add($"{instruction.OpName} needs exact GLSL lowering");
        if (!instruction.Branch && (instruction.Opcode is 0x3d or 0x3e) && !IsRsxFragmentReservedNoOp(instruction))
            unresolved.Add($"opcode 0x{instruction.Opcode:X2} does not match the official-fastfile reserved no-op pattern");
        if (!instruction.Branch && (RsxFragmentSourceRegisterType(instruction.Src0) == 3 ||
            RsxFragmentSourceRegisterType(instruction.Src1) == 3 ||
            RsxFragmentSourceRegisterType(instruction.Src2) == 3))
        {
            unresolved.Add("source register type 3 is not mapped");
        }
    }

    private static int RsxShaderUploadOffset(byte[] data)
    {
        if (data.Length < 0x20)
            return -1;

        uint size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x18, 4));
        uint offset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x1c, 4));
        return offset >= 0x40 &&
            offset + 16 <= data.Length &&
            offset + size <= data.Length &&
            (offset & 0x0f) == 0
            ? (int)offset
            : -1;
    }

    private static string RsxFragmentDecodeText(byte[] data, int offset)
    {
        List<RsxFragmentInstruction> instructions = DecodeRsxFragmentInstructions(data, offset, maxInstructions: 256);
        var opCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var texOps = new List<string>();
        var detail = new List<string>();
        foreach (RsxFragmentInstruction instruction in instructions)
        {
            string opName = instruction.OpName;
            opCounts[opName] = opCounts.TryGetValue(opName, out int count) ? count + 1 : 1;
            detail.Add(RsxFragmentInstructionText(instruction));
            if (instruction.IsTexture)
            {
                texOps.Add($"{instruction.Index}@0x{instruction.Offset:X}:{opName} t{instruction.TextureUnit} {RsxFragmentInputName(instruction.SourceAttribute)}->R{instruction.DestRegister}.{WriteMaskText(instruction.WriteMask)}");
            }
        }

        string ops = string.Join(" ", opCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(16).Select(x => $"{x.Key}:{x.Value}"));
        return $"fp@0x{offset:X} instructions={instructions.Count} ops=[{ops}] tex=[{string.Join(" ", texOps.Take(24))}] code=[{string.Join(" | ", detail.Take(96))}]";
    }

    private static bool TryBuildRsxVertexReport(byte[] data, MaterialVertexDeclarationAsset? decl, out string report)
    {
        report = string.Empty;
        int offset = RsxShaderUploadOffset(data);
        if (offset < 0)
            return false;

        List<RsxVertexInstruction> instructions = DecodeRsxVertexInstructions(data, offset, maxInstructions: 512);
        if (instructions.Count == 0)
            return false;

        var builder = new StringBuilder();
        builder.AppendLine("SAFE_FOR_LIVE_RENDERING=false");
        builder.AppendLine("reason=vertex bytecode/declaration report only; not wired into viewer.html");
        builder.AppendLine("evidence=EBOOT.elf:0xfb498/0xfb128 load Material*ShaderProgram roots; techset loader 0xe8b90 proves MaterialVertexDeclaration as 0x1c bytes; PSL1GHT nvfx_shader.h documents RSX vertex input indices/opcodes; Mesa/Nouveau nv40_vertprog.h documents NV40 vertex instruction source/dest/output bit fields");
        builder.AppendLine($"dataSize=0x{data.Length:X}");
        builder.AppendLine($"headerDwords={ShaderHeaderText(data)}");
        builder.AppendLine($"uploadCodeOffset=0x{offset:X}");
        builder.AppendLine($"instructionCount={instructions.Count}");
        builder.AppendLine($"symbols={ShaderSymbolsText(data)}");
        builder.AppendLine($"vertexDecl={(decl is null ? "none" : TechniqueRouteText(decl, includeSemanticNames: true))}");
        builder.AppendLine($"vertexInputContract={(decl is null ? "none" : RsxVertexInputContractText(decl))}");
        builder.AppendLine($"vertexOutputFragmentContract={RsxVertexOutputFragmentContractText(instructions)}");
        builder.AppendLine($"vertexGlslSemantics={RsxVertexGlslSemanticsText()}");
        builder.AppendLine($"rsxDecode={RsxVertexDecodeText(instructions, offset)}");
        builder.AppendLine($"unresolved={RsxVertexUnresolvedText(instructions)}");
        builder.AppendLine("instructions:");
        foreach (RsxVertexInstruction instruction in instructions)
            builder.AppendLine(RsxVertexInstructionDetailText(instruction));
        report = builder.ToString();
        return true;
    }

    private static string RsxVertexInputContractText(MaterialVertexDeclarationAsset decl)
    {
        var routes = decl.Routing
            .Take(decl.StreamCount)
            .Select(route =>
                $"{RouteSourceName(route.Source)}->{RouteDestinationName(route.Dest)}" +
                $":rsxVpInput={RsxVertexDeclarationDestinationText(route.Dest)}" +
                $":fragmentInput={RsxFragmentInputFromVertexDestinationText(route.Dest)}");
        return string.Join(" ", routes) +
            " | evidence=techset loader 0xe8b90 proves the 0x1c MaterialVertexDeclaration route table; PSL1GHT nvfx_shader.h documents RSX vertex input indices and RSX fragment input aliases";
    }

    private static string BackendStreamVertexInputProofText(MaterialTechniqueSetAsset techset, MaterialVertexDeclarationAsset decl)
    {
        string routes = string.Join(
            " ",
            decl.Routing
                .Take(decl.StreamCount)
                .Select(route =>
                    $"{RouteSourceName(route.Source)}->{RouteDestinationName(route.Dest)}" +
                    $":rsxVpInput={RsxVertexDeclarationDestinationText(route.Dest)}"));
        return $"OPEN format={techset.WorldVertexFormat} routes={routes} | requires exhaustive official-fastfile byte validation of each backend stream source record before live GLSL/reader use";
    }

    private static string RsxVertexOutputFragmentContractText(IReadOnlyList<RsxVertexInstruction> instructions)
    {
        string routes = string.Join(
            " ",
            instructions
                .SelectMany(RsxVertexWrittenOutputs)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(output => $"{output}->{RsxVertexOutputFragmentName(output)}"));
        if (routes.Length == 0)
            routes = "outputs=none";

        return $"{routes} | evidence=Khronos NV_vertex_program2 defines COL0/COL1 and TEX0..TEX7 vertex result attributes as fragment-interpolated color/texture-coordinate varyings; report aliases match the decoded RSX fragment input names";
    }

    private static string RsxVertexGlslSemanticsText()
    {
        return "vertexWrite applies vector/scalar slot write masks per component; scalar slot operands are read from src2 in the observed NV40 encoding; predicate uses the same FL/LT/EQ/LE/GT/NE/GE/TR ccTest rules and ccWrite updates written components from result sign/zero/NaN | evidence=Khronos NV_vertex_program2 TestCC/GenerateCC pseudocode plus Mesa/Nouveau NV40 vertex bit-field decode";
    }

    private static string RsxVertexOutputFragmentName(string output)
    {
        return output switch
        {
            "o0/POS" => "gl_Position",
            "o1/COL0" => "color0",
            "o2/COL1" => "color1",
            "o5/FOGC" => "fog",
            "o6/PSZ" => "gl_PointSize",
            _ when output.Contains("/TEX", StringComparison.Ordinal) =>
                $"texcoord{output[(output.IndexOf("/TEX", StringComparison.Ordinal) + 4)..]}",
            _ => "unmapped"
        };
    }

    private static string RsxVertexDeclarationDestinationText(byte dest)
    {
        return dest switch
        {
            0x00 => "v0/POS",
            0x01 => "v2/NORMAL",
            0x02 => "v3/COL0",
            0x03 or 0x0d => "v4/COL1",
            0x04 => "depth",
            >= 0x05 and <= 0x0c => $"v{8 + dest - 0x05}/TEX{dest - 0x05}",
            _ => $"dest[{dest:X2}]"
        };
    }

    private static string RsxFragmentInputFromVertexDestinationText(byte dest)
    {
        return dest switch
        {
            0x00 => "position",
            0x02 => "color0",
            0x03 or 0x0d => "color1",
            >= 0x05 and <= 0x0c => $"texcoord{dest - 0x05}",
            _ => "not-fragment-varying"
        };
    }

    private static string RsxFragmentPatchTableText(
        MaterialPassAsset pass,
        byte[] data,
        RsxShaderRuntimeInfo runtimeInfo)
    {
        int stableStart = pass.PerPrimArgCount + pass.PerObjArgCount;
        var patchArgs = pass.Args
            .Select((arg, index) => (arg, index))
            .Where(item => item.index >= stableStart && item.arg.Type is
                MaterialShaderArgumentType.CodePixelConst or
                MaterialShaderArgumentType.MaterialPixelConst or
                MaterialShaderArgumentType.LiteralPixelConst)
            .GroupBy(item => item.arg.Dest)
            .OrderBy(group => group.Key)
            .ToList();

        if (patchArgs.Count == 0)
            return "none";

        var parts = new List<string>(patchArgs.Count);
        foreach (IGrouping<ushort, (MaterialShaderArgumentAsset arg, int index)> group in patchArgs)
        {
            string argRefs = string.Join("/", group.Select(item => $"#{item.index}:{item.arg.Type}:raw=0x{unchecked((uint)item.arg.ArgumentRaw):X8}"));
            string patchTable = TryReadRsxPatchEntry(data, runtimeInfo, group.Key, out RsxPatchEntry patchEntry)
                ? $"runtimePatchCount={patchEntry.PatchOffsets.Count}:entry=0x{patchEntry.EntryOffset:X}:defaultConst=0x{patchEntry.DefaultConstantOffset:X}:patchList=0x{patchEntry.PatchListOffset:X}:byteOffsets={PatchOffsetsText(patchEntry)}"
                : "runtimePatchTable=UNRESOLVED_FOR_DEST";
            parts.Add($"dest=0x{group.Key:X4}:args={argRefs}:{patchTable}");
        }

        return string.Join(" | ", parts) +
            " | evidence=EBOOT.elf:00361f38 builds runtime *(MaterialPixelShader+0x04) as count/offset halfword pairs from raw parameter entries; 0045e4b0 reads entry+0x18 patch-list count; 0045e4e0 reads raw patch offsets; default_mp.elf:00396e48 indexes dest*4, loads patch count and byte offsets, and stores a 16-byte constant at runtimeUpload+offset";
    }

    private static bool TryBuildRsxRuntimeUploadModel(
        byte[] data,
        MaterialPassAsset pass,
        MaterialAsset? material,
        RsxShaderRuntimeInfo runtimeInfo,
        out RsxRuntimeUploadModel model)
    {
        model = default;
        if (runtimeInfo.Tag == 0 ||
            runtimeInfo.UploadSize > int.MaxValue ||
            runtimeInfo.UploadOffset + runtimeInfo.UploadSize > data.Length)
        {
            return false;
        }

        byte[] upload = data.AsSpan((int)runtimeInfo.UploadOffset, (int)runtimeInfo.UploadSize).ToArray();
        var notes = new List<string>();
        int defaultPatches = ApplyDefaultRsxConstantPatches(data, runtimeInfo, upload);
        int argPatches = 0;
        int externalPatches = 0;

        Dictionary<uint, MaterialConstantDef> constantsByHash = material?.Constants
            .GroupBy(constant => constant.NameHash)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        int stableStart = pass.PerPrimArgCount + pass.PerObjArgCount;
        foreach (MaterialShaderArgumentAsset arg in pass.Args.Skip(stableStart))
        {
            if (arg.Type is not (MaterialShaderArgumentType.CodePixelConst or MaterialShaderArgumentType.MaterialPixelConst or MaterialShaderArgumentType.LiteralPixelConst) ||
                !TryReadRsxPatchEntry(data, runtimeInfo, arg.Dest, out RsxPatchEntry entry))
            {
                continue;
            }

            uint raw = unchecked((uint)arg.ArgumentRaw);
            switch (arg.Type)
            {
                case MaterialShaderArgumentType.CodePixelConst:
                    externalPatches += entry.PatchOffsets.Count;
                    notes.Add($"dest=0x{arg.Dest:X4}:codeConst[index=0x{CodeConstIndex(raw):X4},count={CodeConstVectorCount(raw)}]:externalContextPatches={entry.PatchOffsets.Count}");
                    break;
                case MaterialShaderArgumentType.MaterialPixelConst when constantsByHash.TryGetValue(raw, out MaterialConstantDef? constant):
                    argPatches += ApplyRsxConstantPatch(upload, entry, constant.Literal);
                    notes.Add($"dest=0x{arg.Dest:X4}:materialConst=0x{raw:X8}:patches={entry.PatchOffsets.Count}");
                    break;
                case MaterialShaderArgumentType.LiteralPixelConst when arg.LiteralConstant is { } literal:
                    argPatches += ApplyRsxConstantPatch(upload, entry, literal);
                    notes.Add($"dest=0x{arg.Dest:X4}:literal:patches={entry.PatchOffsets.Count}");
                    break;
                default:
                    notes.Add($"dest=0x{arg.Dest:X4}:{arg.Type}:valueUnavailable");
                    break;
            }
        }

        model = new RsxRuntimeUploadModel(upload, defaultPatches, argPatches, externalPatches, notes);
        return true;
    }

    private static int ApplyDefaultRsxConstantPatches(byte[] data, RsxShaderRuntimeInfo runtimeInfo, byte[] upload)
    {
        int writes = 0;
        for (ushort dest = 0; dest < runtimeInfo.ParameterCount; dest++)
        {
            if (!TryReadRsxPatchEntry(data, runtimeInfo, dest, out RsxPatchEntry entry) ||
                entry.DefaultConstantOffset == 0 ||
                entry.DefaultConstantOffset + 0x10 > data.Length)
            {
                continue;
            }

            foreach (ushort patchOffset in entry.PatchOffsets)
            {
                if (patchOffset + 0x10 > upload.Length)
                    continue;

                data.AsSpan((int)entry.DefaultConstantOffset, 0x10).CopyTo(upload.AsSpan(patchOffset, 0x10));
                RotateRsxConstantWords(upload, patchOffset);
                writes++;
            }
        }

        return writes;
    }

    private static int ApplyRsxConstantPatch(byte[] upload, RsxPatchEntry entry, MaterialVec4 value)
    {
        return ApplyRsxConstantPatch(upload, entry, value.X, value.Y, value.Z, value.W);
    }

    private static int ApplyRsxConstantPatch(byte[] upload, RsxPatchEntry entry, MaterialShaderLiteralConstant value)
    {
        return ApplyRsxConstantPatch(upload, entry, value.X, value.Y, value.Z, value.W);
    }

    private static int ApplyRsxConstantPatch(byte[] upload, RsxPatchEntry entry, float x, float y, float z, float w)
    {
        int writes = 0;
        Span<byte> encoded = stackalloc byte[0x10];
        WriteRsxFragmentConstantFloat(encoded, 0x00, x);
        WriteRsxFragmentConstantFloat(encoded, 0x04, y);
        WriteRsxFragmentConstantFloat(encoded, 0x08, z);
        WriteRsxFragmentConstantFloat(encoded, 0x0c, w);
        foreach (ushort patchOffset in entry.PatchOffsets)
        {
            if (patchOffset + 0x10 > upload.Length)
                continue;

            encoded.CopyTo(upload.AsSpan(patchOffset, 0x10));
            writes++;
        }

        return writes;
    }

    private static void WriteRsxFragmentConstantFloat(Span<byte> destination, int offset, float value)
    {
        uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        BinaryPrimitives.WriteUInt32BigEndian(destination[offset..], RsxFragmentWord(bits));
    }

    private static void RotateRsxConstantWords(byte[] upload, int patchOffset)
    {
        for (int i = 0; i < 0x10; i += 4)
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(upload.AsSpan(patchOffset + i, 4));
            BinaryPrimitives.WriteUInt32BigEndian(upload.AsSpan(patchOffset + i, 4), RsxFragmentWord(value));
        }
    }

    private static string RsxRuntimeUploadModelText(RsxRuntimeUploadModel model)
    {
        if (model.UploadCode is null)
            return "unavailable";

        string notes = model.Notes.Count == 0
            ? "none"
            : string.Join("/", model.Notes.Take(16));
        return string.Join(
            " ",
            $"bytes=0x{model.UploadCode.Length:X}",
            $"fnv1a=0x{Fnv1A32(model.UploadCode):X8}",
            $"defaultConstantPatches={model.DefaultConstantPatches}",
            $"materialOrLiteralPatches={model.ArgConstantPatches}",
            $"externalCodeConstPatches={model.ExternalCodeConstantPatches}",
            $"notes={notes}",
            "evidence=EBOOT.elf:0045e7f0 copies default constants into raw upload patch offsets with 16-bit word rotation; default_mp.elf:00396e48 applies code/material/literal pixel constants to runtime upload+patchOffset using the same patch table");
    }

    private static bool TryReadRsxShaderRuntimeInfo(byte[] data, out RsxShaderRuntimeInfo info)
    {
        info = default;
        if (data.Length < 0x20)
            return false;

        uint tag = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0, 4));
        if (!IsKnownRsxShaderTag(tag))
            return false;

        uint parameterCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x0c, 4));
        uint parameterTableOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x10, 4));
        uint descriptorOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x14, 4));
        uint uploadSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x18, 4));
        uint uploadOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x1c, 4));
        ulong parameterTableEnd = (ulong)parameterTableOffset + parameterCount * 0x30UL;
        ulong uploadEnd = (ulong)uploadOffset + uploadSize;
        if (parameterTableEnd > (ulong)data.Length || uploadEnd > (ulong)data.Length)
            return false;

        info = new RsxShaderRuntimeInfo(
            tag,
            parameterCount,
            parameterTableOffset,
            descriptorOffset,
            uploadSize,
            uploadOffset);
        return true;
    }

    private static bool IsKnownRsxShaderTag(uint tag)
    {
        return tag is 0x1807 or 0x1b59 or 0x1b5b or 0x1b5c or 0x1b5d or 0x1b5e;
    }

    private static string RsxRuntimeModelText(byte[] data, RsxShaderRuntimeInfo runtimeInfo)
    {
        if (runtimeInfo.Tag == 0)
            return "unparsed";

        uint tableHalfwords = runtimeInfo.ParameterCount * 2;
        for (ushort dest = 0; dest < runtimeInfo.ParameterCount; dest++)
        {
            if (TryReadRsxPatchEntry(data, runtimeInfo, dest, out RsxPatchEntry entry))
                tableHalfwords += (uint)entry.PatchOffsets.Count;
        }

        return string.Join(
            " ",
            $"tag=0x{runtimeInfo.Tag:X}",
            $"parameterCount=0x{runtimeInfo.ParameterCount:X}",
            $"parameterTableOffset=0x{runtimeInfo.ParameterTableOffset:X}",
            $"descriptorOffset=0x{runtimeInfo.DescriptorOffset:X}",
            $"uploadCodeOffset=0x{runtimeInfo.UploadOffset:X}",
            $"uploadCodeSize=0x{runtimeInfo.UploadSize:X}",
            $"commandList={RsxCommandListModelText(data, runtimeInfo)}",
            $"runtimePatchTableHalfwords=0x{tableHalfwords:X}",
            "evidence=EBOOT.elf:0045e7f0 normalizes the raw shader and patches default constants into upload code; 00361f38 stores runtime +0x04 patch table, +0x08 command-list offset, +0x0c command dword count, +0x10 patch-table halfword count, +0x12 upload byte size, +0x14 upload pointer; 0045e028 returns raw+0x1c and raw+0x18");
    }

    private static string RsxCommandListModelText(byte[] data, RsxShaderRuntimeInfo runtimeInfo)
    {
        if (runtimeInfo.DescriptorOffset + 0x16 > data.Length)
            return "unparsed";

        int descriptorOffset = (int)runtimeInfo.DescriptorOffset;
        uint descriptorWord04 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(descriptorOffset + 0x04, 4));
        ushort colorMask = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(descriptorOffset + 0x0c, 2));
        ushort controlMask0E = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(descriptorOffset + 0x0e, 2));
        ushort controlMask10 = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(descriptorOffset + 0x10, 2));
        byte byte12 = data[descriptorOffset + 0x12];
        byte byte13 = data[descriptorOffset + 0x13];
        byte byte14 = data[descriptorOffset + 0x14];
        byte byte15 = data[descriptorOffset + 0x15];

        return string.Join(
            "/",
            $"descriptor=0x{runtimeInfo.DescriptorOffset:X}",
            $"method0x041ff4Value=0x{descriptorWord04 | 0x20:X8}",
            $"rtMask=0x{colorMask:X4}",
            $"rtMethodCount={PopCount(colorMask)}",
            $"control0E=0x{controlMask0E:X4}",
            $"control10=0x{controlMask10:X4}",
            $"bytes12_15={byte12:X2}{byte13:X2}{byte14:X2}{byte15:X2}",
            $"copiedDwordsExcludingTerminator=0x{RsxCopiedCommandDwordCount(data, runtimeInfo):X}",
            "evidence=EBOOT.elf:00460098 calls 00460b28(builder,raw,0,0) then 00460cd8(builder,raw,1); 00361f38 appends 0x00020000, copies from builder+8, and stores +0x0c as totalDwords-3");
    }

    private static int RsxCopiedCommandDwordCount(byte[] data, RsxShaderRuntimeInfo runtimeInfo)
    {
        if (runtimeInfo.DescriptorOffset + 0x0e > data.Length)
            return 0;

        ushort colorMask = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)runtimeInfo.DescriptorOffset + 0x0c, 2));
        int methodPairs = 3 + PopCount(colorMask);
        return methodPairs * 2 - 2;
    }

    private static int PopCount(ushort value)
    {
        int count = 0;
        uint remaining = value;
        while (remaining != 0)
        {
            remaining &= remaining - 1;
            count++;
        }

        return count;
    }

    private static bool TryReadRsxPatchEntry(
        byte[] data,
        RsxShaderRuntimeInfo runtimeInfo,
        ushort dest,
        out RsxPatchEntry entry)
    {
        entry = default;
        if (runtimeInfo.Tag == 0 || dest >= runtimeInfo.ParameterCount)
            return false;

        uint entryOffset = runtimeInfo.ParameterTableOffset + dest * 0x30u;
        if (entryOffset + 0x1c > data.Length)
            return false;

        uint defaultConstantOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)entryOffset + 0x14, 4));
        uint patchListOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)entryOffset + 0x18, 4));
        if (patchListOffset == 0)
        {
            entry = new RsxPatchEntry(entryOffset, defaultConstantOffset, patchListOffset, []);
            return true;
        }

        if (patchListOffset + 4 > data.Length)
            return false;

        uint patchCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)patchListOffset, 4));
        if (patchCount > ushort.MaxValue || patchListOffset + 4 + patchCount * 4UL > (ulong)data.Length)
            return false;

        var patchOffsets = new List<ushort>((int)patchCount);
        for (int index = 0; index < patchCount; index++)
        {
            uint rawOffset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)patchListOffset + 4 + index * 4, 4));
            if (rawOffset > ushort.MaxValue || rawOffset + 0x10 > runtimeInfo.UploadSize)
                return false;
            patchOffsets.Add((ushort)rawOffset);
        }

        entry = new RsxPatchEntry(entryOffset, defaultConstantOffset, patchListOffset, patchOffsets);
        return true;
    }

    private static string PatchOffsetsText(RsxPatchEntry entry)
    {
        return entry.PatchOffsets.Count == 0
            ? "none"
            : string.Join("/", entry.PatchOffsets.Select(offset => $"0x{offset:X4}"));
    }

    private static string RsxFragmentInstructionDetailText(RsxFragmentInstruction instruction)
    {
        string fields = string.Join(
            " ",
            $"{instruction.Index:000}@0x{instruction.Offset:X}",
            $"raw=dst:{instruction.Dst:X8}/s0:{instruction.Src0:X8}/s1:{instruction.Src1:X8}/s2:{instruction.Src2:X8}",
            $"op={instruction.OpName}(0x{instruction.Opcode:X2})",
            $"branch={instruction.Branch}",
            $"end={instruction.End}",
            $"dest={RsxFragmentDestinationText(instruction)}",
            $"output={RsxFragmentOutputAliasText(instruction)}",
            $"sat={instruction.Saturate}",
            $"scale={RsxFragmentScaleText(instruction.Scale)}",
            $"precision={RsxFragmentPrecisionText(instruction.Precision)}",
            $"condWrite={instruction.CondWriteEnabled}",
            $"cond={RsxFragmentConditionText(instruction.ConditionCode)}.{RsxFragmentConditionSwizzleText(instruction)}",
            $"conditionEffect={RsxFragmentConditionEffectText(instruction)}",
            $"texUnit={instruction.TextureUnit}",
            $"texExpand={instruction.TextureExpand}",
            $"inputSrc={RsxFragmentInputName(instruction.SourceAttribute)}",
            $"disablePc={instruction.DisablePerspectiveCorrect}",
            $"interpolation={RsxFragmentInterpolationText(instruction)}");

        if (instruction.Branch)
        {
            fields += $" branchSrc1=0x{instruction.BranchSrc1Payload:X8} branchSrc2=0x{instruction.BranchSrc2Payload:X8}";
        }

        if (!instruction.Branch)
        {
            fields += $" src0={RsxFragmentSourceText(instruction, instruction.Src0, 0, instruction.Constant)}";
            fields += $" src1={RsxFragmentSourceText(instruction, instruction.Src1, 1, instruction.Constant)}";
            fields += $" src2={RsxFragmentSourceText(instruction, instruction.Src2, 2, instruction.Constant)}";
        }
        if (instruction.Constant is { } constant)
            fields += $" inlineConst={constant.ToGlsl()}";
        fields += $" lowering={RsxFragmentLoweringText(instruction)}";
        fields += $" glslWrite={RsxFragmentGlslWriteText(instruction)}";
        return fields;
    }

    private static string RsxFragmentDestinationText(RsxFragmentInstruction instruction)
    {
        return instruction.NoDest
            ? "-"
            : $"{(instruction.DestFp16 ? "H" : "R")}{instruction.DestRegister}.{WriteMaskText(instruction.WriteMask)}";
    }

    private static string RsxFragmentOutputAliasText(RsxFragmentInstruction instruction)
    {
        if (instruction.NoDest || instruction.WriteMask == 0)
            return "-";
        if (instruction.DestRegister == 0)
            return $"result.color.{WriteMaskText(instruction.WriteMask)}";
        if (instruction.DestRegister == 1 && (instruction.WriteMask & 0x4) != 0)
            return "result.depth.z";
        return "-";
    }

    private static string RsxFragmentScaleText(int scale)
    {
        return scale switch
        {
            0 => "1x",
            1 => "2x",
            2 => "4x",
            3 => "8x",
            5 => "1/2x",
            6 => "1/4x",
            7 => "1/8x",
            _ => $"unmapped({scale})"
        };
    }

    private static string RsxFragmentPrecisionText(int precision)
    {
        return precision switch
        {
            0 => "fp32",
            1 => "fp16",
            2 => "fx12",
            3 => "fx8",
            _ => $"precision{precision}"
        };
    }

    private static string RsxFragmentConditionText(int condition)
    {
        return condition switch
        {
            0 => "FL",
            1 => "LT",
            2 => "EQ",
            3 => "LE",
            4 => "GT",
            5 => "NE",
            6 => "GE",
            7 => "TR",
            _ => $"cond{condition}"
        };
    }

    private static string RsxFragmentConditionSwizzleText(RsxFragmentInstruction instruction)
    {
        Span<char> chars = stackalloc char[4];
        chars[0] = RsxFragmentSwizzleChar((int)((instruction.Src0 >> 21) & 0x3));
        chars[1] = RsxFragmentSwizzleChar((int)((instruction.Src0 >> 23) & 0x3));
        chars[2] = RsxFragmentSwizzleChar((int)((instruction.Src0 >> 25) & 0x3));
        chars[3] = RsxFragmentSwizzleChar((int)((instruction.Src0 >> 27) & 0x3));
        return new string(chars);
    }

    private static string RsxFragmentConditionEffectText(RsxFragmentInstruction instruction)
    {
        string predicate = instruction.ConditionCode == 7
            ? "execute=always"
            : $"execute=if {RsxFragmentPredicateText(instruction)}";
        string write = instruction.CondWriteEnabled
            ? "ccWrite=enabled-from-result"
            : "ccWrite=none";
        return $"{predicate};{write}";
    }

    private static string RsxFragmentCcSemanticsText()
    {
        return "rsxWrite applies the instruction write mask per component; predicate ccTest uses FL/LT/EQ/LE/GT/NE/GE/TR against cc.xyzw with swizzle, TR always true and FL always false; NE also passes unordered/NaN; cc initializes EQ per fragment; ccWrite updates only written components from result sign/zero/NaN and leaves masked components unchanged | evidence=Khronos NV_fragment_program condition-code vector plus NV_vertex_program2 TestCC/GenerateCC pseudocode";
    }

    private static string RsxFragmentPredicateText(RsxFragmentInstruction instruction)
    {
        return instruction.ConditionCode == 7
            ? "TR.xyzw"
            : $"cc.{RsxFragmentConditionSwizzleText(instruction)} {RsxFragmentConditionText(instruction.ConditionCode)}";
    }

    private static string RsxFragmentInterpolationContractText(IReadOnlyList<RsxFragmentInstruction> instructions)
    {
        string inputs = string.Join(
            " ",
            instructions
                .Where(instruction => !instruction.Branch && instruction.DisablePerspectiveCorrect)
                .Select(instruction => RsxFragmentInputName(instruction.SourceAttribute))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(input => $"{input}=noperspective"));
        if (inputs.Length == 0)
            inputs = "disablePcInputs=none";

        return $"default=smooth {inputs} | evidence=PSL1GHT cgcomp emits the NV40 fragment disable_pc bit only while encoding texture-coordinate inputs; GLSL noperspective is the non-perspective-correct interpolation qualifier";
    }

    private static string RsxFragmentInterpolationText(RsxFragmentInstruction instruction)
    {
        return instruction.DisablePerspectiveCorrect
            ? $"{RsxFragmentInputName(instruction.SourceAttribute)}:noperspective"
            : "default:smooth";
    }

    private static List<RsxFragmentInstruction> DecodeRsxFragmentInstructions(byte[] data, int offset, int maxInstructions)
    {
        var instructions = new List<RsxFragmentInstruction>();
        int pc = offset;
        for (int index = 0; index < maxInstructions && pc + 16 <= data.Length; index++)
        {
            uint dst = RsxFragmentWord(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc, 4)));
            uint src0 = RsxFragmentWord(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 4, 4)));
            uint src1 = RsxFragmentWord(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 8, 4)));
            uint src2 = RsxFragmentWord(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 12, 4)));
            byte opcode = (byte)((dst >> 24) & 0x3f);
            bool branch = (src1 & 0x80000000u) != 0;
            int operandCount = branch ? 0 : RsxFragmentOperandCount(opcode);
            bool hasInlineConstant =
                (operandCount > 0 && RsxFragmentSourceRegisterType(src0) == 2) ||
                (operandCount > 1 && RsxFragmentSourceRegisterType(src1) == 2) ||
                (operandCount > 2 && RsxFragmentSourceRegisterType(src2) == 2);
            RsxFragmentConstant? constant = null;
            int byteCount = hasInlineConstant ? 32 : 16;
            if (hasInlineConstant && pc + byteCount <= data.Length)
            {
                constant = new RsxFragmentConstant(
                    RsxFragmentConstantFloat(data, pc + 16),
                    RsxFragmentConstantFloat(data, pc + 20),
                    RsxFragmentConstantFloat(data, pc + 24),
                    RsxFragmentConstantFloat(data, pc + 28));
            }

            instructions.Add(new RsxFragmentInstruction(index, pc, dst, src0, src1, src2, opcode, byteCount, constant));
            pc += byteCount;
            if ((dst & 0x1) != 0)
                break;
        }

        return instructions;
    }

    private static float RsxFragmentConstantFloat(byte[] data, int offset)
    {
        uint value = RsxFragmentWord(BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4)));
        return BitConverter.Int32BitsToSingle(unchecked((int)value));
    }

    private static string RsxFragmentInstructionText(RsxFragmentInstruction instruction)
    {
        string dest = RsxFragmentDestinationText(instruction);
        string source0 = RsxFragmentSourceText(instruction, instruction.Src0, 0, instruction.Constant);
        string source1 = RsxFragmentSourceText(instruction, instruction.Src1, 1, instruction.Constant);
        string source2 = RsxFragmentSourceText(instruction, instruction.Src2, 2, instruction.Constant);
        string sources = (instruction.Branch ? 0 : RsxFragmentOperandCount(instruction.Opcode)) switch
        {
            0 => string.Empty,
            1 => source0,
            2 => $"{source0},{source1}",
            _ => $"{source0},{source1},{source2}"
        };
        string flags = string.Concat(
            instruction.Saturate ? "_sat" : string.Empty,
            instruction.DestFp16 ? "_h" : string.Empty,
            instruction.End ? "_end" : string.Empty,
            instruction.ByteCount == 32 ? "_const" : string.Empty);
        return $"{instruction.Index:000}@0x{instruction.Offset:X}:{instruction.OpName}{flags} {dest} <- {sources}";
    }

    private static string RsxFragmentLoweringText(RsxFragmentInstruction instruction)
    {
        if (instruction.Branch)
            return "control-flow";
        if (IsRsxFragmentReservedNoOp(instruction))
            return "reserved-nop";
        if (instruction.Opcode is 0x3d or 0x3e)
            return "unmapped-opcode";

        string dest = RsxFragmentDestinationText(instruction);
        string expression = RsxFragmentExpressionText(instruction);
        return instruction.NoDest || instruction.WriteMask == 0
            ? expression
            : $"{dest}={expression}";
    }

    private static string RsxFragmentGlslWriteText(RsxFragmentInstruction instruction)
    {
        if (instruction.Branch)
            return "control-flow";
        if (IsRsxFragmentReservedNoOp(instruction))
            return "reserved-nop";
        if (instruction.Opcode is 0x3d or 0x3e)
            return "unmapped-opcode";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"rsxWrite(dest={RsxFragmentDestinationText(instruction)},mask={WriteMaskText(instruction.WriteMask)},predicate={RsxFragmentPredicateText(instruction)},ccWrite={instruction.CondWriteEnabled},value={RsxFragmentExpressionText(instruction)})");
    }

    private static string RsxFragmentExpressionText(RsxFragmentInstruction instruction)
    {
        string source0 = RsxFragmentSourceText(instruction, instruction.Src0, 0, instruction.Constant);
        string source1 = RsxFragmentSourceText(instruction, instruction.Src1, 1, instruction.Constant);
        string source2 = RsxFragmentSourceText(instruction, instruction.Src2, 2, instruction.Constant);
        string expression = instruction.Opcode switch
        {
            0x00 => "nop",
            0x01 => source0,
            0x02 => $"{source0} * {source1}",
            0x03 => $"{source0} + {source1}",
            0x04 => $"{source0} * {source1} + {source2}",
            0x05 => $"dot3({source0}, {source1})",
            0x06 => $"dot4({source0}, {source1})",
            0x08 => $"min({source0}, {source1})",
            0x09 => $"max({source0}, {source1})",
            0x0a => $"lessThan({source0}, {source1})",
            0x0b => $"greaterThanEqual({source0}, {source1})",
            0x0c => $"lessThanEqual({source0}, {source1})",
            0x0d => $"greaterThan({source0}, {source1})",
            0x0e => $"notEqual({source0}, {source1})",
            0x0f => $"equal({source0}, {source1})",
            0x10 => $"fract({source0})",
            0x11 => $"floor({source0})",
            0x12 => $"kill({source0})",
            0x15 => $"dFdx({source0})",
            0x16 => $"dFdy({source0})",
            0x17 => RsxTextureLoweringText("texture", instruction, source0, null),
            0x18 => RsxTextureLoweringText("textureProj", instruction, source0, null),
            0x1a => $"1.0 / {source0}",
            0x1b => $"inversesqrt({source0})",
            0x1c => $"exp2({source0})",
            0x1d => $"log2({source0})",
            0x22 => $"cos({source0})",
            0x23 => $"sin({source0})",
            0x2f => RsxTextureLoweringText("textureLod", instruction, source0, $"{source0}.w"),
            0x31 => RsxTextureLoweringText("textureBias", instruction, source0, $"{source0}.w"),
            0x38 => $"dot2({source0}, {source1})",
            0x39 => $"normalize({source0})",
            0x3a => $"{source0} / {source1}",
            0x3b => $"{source0} * inversesqrt({source1})",
            0x3c => $"litExp2({source0})",
            _ => $"{instruction.OpName.ToLowerInvariant()}({source0}, {source1}, {source2})"
        };

        if (instruction.Saturate)
            expression = $"saturate({expression})";
        string scale = RsxFragmentScaleFactorText(instruction.Scale);
        if (scale != "1")
            expression = $"{expression} * {scale}";

        return expression;
    }

    private static string RsxTextureLoweringText(string op, RsxFragmentInstruction instruction, string coord, string? lod)
    {
        string sampler = $"sampler{instruction.TextureUnit}";
        return lod is null
            ? $"{op}({sampler}, {coord})"
            : $"{op}({sampler}, {coord}, {lod})";
    }

    private static string RsxFragmentScaleFactorText(int scale)
    {
        return scale switch
        {
            0 => "1",
            1 => "2",
            2 => "4",
            3 => "8",
            5 => "0.5",
            6 => "0.25",
            7 => "0.125",
            _ => $"scale{scale}"
        };
    }

    private static string RsxFragmentSourceText(RsxFragmentInstruction instruction, uint source, int sourceIndex, RsxFragmentConstant? constant)
    {
        string value = RsxFragmentSourceBase(instruction, source, constant);
        string suffix = RsxFragmentSwizzleSuffix(source);
        if (!string.IsNullOrEmpty(suffix))
            value += suffix;
        if (RsxFragmentSourceAbs(source, sourceIndex))
            value = $"|{value}|";
        if (RsxFragmentSourceNeg(source))
            value = $"-{value}";
        return value;
    }

    private static string RsxFragmentSourceBase(RsxFragmentInstruction instruction, uint source, RsxFragmentConstant? constant)
    {
        return RsxFragmentSourceRegisterType(source) switch
        {
            0 => $"{(RsxFragmentSourceFp16(source) ? "H" : "R")}{RsxFragmentSourceRegisterIndex(source)}",
            1 => RsxFragmentInputName(instruction.SourceAttribute),
            2 => constant is { } value ? $"C{instruction.Index}({value.ToGlsl()})" : $"C{instruction.Index}",
            _ => $"sourceType{RsxFragmentSourceRegisterType(source)}"
        };
    }

    private static string RsxFragmentSwizzleSuffix(uint source, bool simplify = true)
    {
        Span<char> chars = stackalloc char[4];
        chars[0] = RsxFragmentSwizzleChar((int)((source >> 9) & 0x3));
        chars[1] = RsxFragmentSwizzleChar((int)((source >> 11) & 0x3));
        chars[2] = RsxFragmentSwizzleChar((int)((source >> 13) & 0x3));
        chars[3] = RsxFragmentSwizzleChar((int)((source >> 15) & 0x3));
        if (!simplify)
            return chars is ['x', 'y', 'z', 'w'] ? string.Empty : $".{new string(chars)}";

        return chars switch
        {
            ['x', 'y', 'z', 'w'] => string.Empty,
            ['x', 'x', 'x', 'x'] => ".x",
            ['y', 'y', 'y', 'y'] => ".y",
            ['z', 'z', 'z', 'z'] => ".z",
            ['w', 'w', 'w', 'w'] => ".w",
            _ => $".{new string(chars)}"
        };
    }

    private static char RsxFragmentSwizzleChar(int value)
    {
        return value switch
        {
            0 => 'x',
            1 => 'y',
            2 => 'z',
            _ => 'w'
        };
    }

    private static int RsxFragmentSourceRegisterType(uint source) => (int)(source & 0x3);

    private static int RsxFragmentSourceRegisterIndex(uint source) => (int)((source >> 2) & 0x3f);

    private static bool RsxFragmentSourceFp16(uint source) => ((source >> 8) & 0x1) != 0;

    private static bool RsxFragmentSourceNeg(uint source) => ((source >> 17) & 0x1) != 0;

    private static bool RsxFragmentSourceAbs(uint source, int sourceIndex)
    {
        return sourceIndex == 0
            ? ((source >> 29) & 0x1) != 0
            : ((source >> 18) & 0x1) != 0;
    }

    private static int RsxFragmentOperandCount(byte opcode)
    {
        return opcode switch
        {
            0x00 or 0x20 or 0x21 or 0x3d or 0x3e or 0x40 or 0x41 or 0x42 or 0x43 or 0x44 or 0x45 => 0,
            0x01 or 0x10 or 0x11 or 0x12 or 0x13 or 0x14 or 0x15 or 0x16 or 0x17 or 0x18 or 0x1a or 0x1b or 0x1c or 0x1d or 0x1e or 0x22 or 0x23 or 0x24 or 0x25 or 0x27 or 0x28 or 0x29 or 0x2a or 0x2c or 0x2d or 0x2f or 0x31 or 0x39 or 0x3c => 1,
            0x02 or 0x03 or 0x05 or 0x06 or 0x07 or 0x08 or 0x09 or 0x0a or 0x0b or 0x0c or 0x0d or 0x0e or 0x0f or 0x36 or 0x38 or 0x3a or 0x3b => 2,
            _ => 3
        };
    }

    private static bool IsRsxFragmentReservedNoOp(RsxFragmentInstruction instruction)
    {
        return !instruction.Branch &&
            (instruction.Opcode is 0x3d or 0x3e) &&
            (instruction.Dst & 0xff00ffffu) == ((0x40u | instruction.Opcode) << 24 | 0x001e7eu) &&
            instruction.Src0 == 0x1c9dc800u &&
            instruction.Src1 == 0x0001c800u &&
            instruction.Src2 == 0x0001c800u &&
            instruction.NoDest &&
            !instruction.End &&
            !instruction.Saturate &&
            instruction.Scale == 0 &&
            !instruction.CondWriteEnabled &&
            instruction.ConditionCode == 7;
    }

    private readonly record struct RsxShaderRuntimeInfo(
        uint Tag,
        uint ParameterCount,
        uint ParameterTableOffset,
        uint DescriptorOffset,
        uint UploadSize,
        uint UploadOffset);

    private readonly record struct RsxPatchEntry(
        uint EntryOffset,
        uint DefaultConstantOffset,
        uint PatchListOffset,
        IReadOnlyList<ushort> PatchOffsets);

    private readonly record struct RsxRuntimeUploadModel(
        byte[]? UploadCode,
        int DefaultConstantPatches,
        int ArgConstantPatches,
        int ExternalCodeConstantPatches,
        IReadOnlyList<string> Notes);

    private readonly record struct RsxFragmentInstruction(
        int Index,
        int Offset,
        uint Dst,
        uint Src0,
        uint Src1,
        uint Src2,
        byte Opcode,
        int ByteCount,
        RsxFragmentConstant? Constant)
    {
        public bool End => (Dst & 0x1) != 0;
        public int DestRegister => (int)((Dst >> 1) & 0x3f);
        public bool DestFp16 => ((Dst >> 7) & 0x1) != 0;
        public int WriteMask => (int)((Dst >> 9) & 0x0f);
        public int SourceAttribute => (int)((Dst >> 13) & 0x0f);
        public int TextureUnit => (int)((Dst >> 17) & 0x0f);
        public bool TextureExpand => ((Dst >> 21) & 0x1) != 0;
        public int Precision => (int)((Dst >> 22) & 0x3);
        public bool NoDest => ((Dst >> 30) & 0x1) != 0;
        public bool Saturate => ((Dst >> 31) & 0x1) != 0;
        public bool CondWriteEnabled => ((Dst >> 8) & 0x1) != 0;
        public int ConditionCode => (int)((Src0 >> 18) & 0x7);
        public bool Branch => (Src1 & 0x80000000u) != 0;
        public int Scale => (int)((Src1 >> 28) & 0x7);
        public uint BranchSrc1Payload => Src1 & 0x7fffffffu;
        public uint BranchSrc2Payload => Src2 & 0x7fffffffu;
        public bool DisablePerspectiveCorrect => (Src2 & 0x80000000u) != 0;
        public bool IsTexture => !Branch && Opcode is 0x17 or 0x18 or 0x19 or 0x2f or 0x31;
        public string OpName => Branch ? RsxFragmentBranchOpcodeName(Opcode) : RsxFragmentOpcodeName(Opcode);
    }

    private readonly record struct RsxFragmentConstant(float X, float Y, float Z, float W)
    {
        public string ToGlsl()
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"vec4({X:R}, {Y:R}, {Z:R}, {W:R})");
        }
    }

    private static string RsxVertexDecodeText(byte[] data, int offset)
    {
        List<RsxVertexInstruction> instructions = DecodeRsxVertexInstructions(data, offset, maxInstructions: 512);
        return RsxVertexDecodeText(instructions, offset);
    }

    private static string RsxVertexDecodeText(IReadOnlyList<RsxVertexInstruction> instructions, int offset)
    {
        var vecCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var scaCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var outputs = new SortedSet<string>(StringComparer.Ordinal);
        var inputs = new SortedSet<string>(StringComparer.Ordinal);
        var constants = new SortedSet<string>(StringComparer.Ordinal);
        foreach (RsxVertexInstruction instruction in instructions)
        {
            string vec = instruction.VecOpName;
            string sca = instruction.ScaOpName;
            vecCounts[vec] = vecCounts.TryGetValue(vec, out int vecCount) ? vecCount + 1 : 1;
            scaCounts[sca] = scaCounts.TryGetValue(sca, out int scaCount) ? scaCount + 1 : 1;
            foreach (string output in RsxVertexWrittenOutputs(instruction))
                outputs.Add(output);
            foreach (string input in RsxVertexReadInputs(instruction))
                inputs.Add(input);
            foreach (string constant in RsxVertexReadConstants(instruction))
                constants.Add(constant);
        }

        string vecOps = string.Join(" ", vecCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(12).Select(x => $"{x.Key}:{x.Value}"));
        string scaOps = string.Join(" ", scaCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(12).Select(x => $"{x.Key}:{x.Value}"));
        string detail = string.Join(" | ", instructions.Take(96).Select(RsxVertexInstructionText));
        return $"vp@0x{offset:X} instructions={instructions.Count} vec=[{vecOps}] sca=[{scaOps}] outputs=[{string.Join(" ", outputs)}] inputs=[{string.Join(" ", inputs)}] constants=[{string.Join(" ", constants)}] code=[{detail}]";
    }

    private static string RsxVertexUnresolvedText(IReadOnlyList<RsxVertexInstruction> instructions)
    {
        var unresolved = new SortedSet<string>(StringComparer.Ordinal);
        foreach (RsxVertexInstruction instruction in instructions)
        {
            if (instruction.HasControlFlow)
                unresolved.Add("vertex branch/control-flow is decoded but not lowered to GLSL");
            if (instruction.CondTestEnabled || instruction.CondUpdateEnabled || instruction.ConditionCode != 7)
                unresolved.Add("vertex condition-code write/test semantics must be lowered exactly in final GLSL");
            if (instruction.VecOpName.StartsWith("vop", StringComparison.Ordinal) ||
                instruction.ScaOpName.StartsWith("sop", StringComparison.Ordinal))
            {
                unresolved.Add("vertex opcode is not mapped");
            }
        }

        return unresolved.Count == 0 ? "none" : string.Join(" | ", unresolved);
    }

    private static List<RsxVertexInstruction> DecodeRsxVertexInstructions(byte[] data, int offset, int maxInstructions)
    {
        var instructions = new List<RsxVertexInstruction>();
        int uploadEnd = data.Length;
        if (data.Length >= 0x20)
        {
            uint uploadSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x18, 4));
            if (uploadSize <= int.MaxValue && offset + uploadSize <= data.Length)
                uploadEnd = offset + (int)uploadSize;
        }

        int pc = offset;
        for (int index = 0; index < maxInstructions && pc + 16 <= uploadEnd; index++, pc += 16)
        {
            uint w0 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc, 4));
            uint w1 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 4, 4));
            uint w2 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 8, 4));
            uint w3 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pc + 12, 4));
            instructions.Add(new RsxVertexInstruction(index, pc, w0, w1, w2, w3));
            if ((w3 & 0x1) != 0)
                break;
        }

        return instructions;
    }

    private static string RsxVertexInstructionText(RsxVertexInstruction instruction)
    {
        return string.Join(
            " ",
            $"{instruction.Index:000}@0x{instruction.Offset:X}",
            $"vec={RsxVertexSlotText(instruction, scalar: false)}",
            $"sca={RsxVertexSlotText(instruction, scalar: true)}",
            $"src0={RsxVertexSourceText(instruction, instruction.Source0, 0)}",
            $"src1={RsxVertexSourceText(instruction, instruction.Source1, 1)}",
            $"src2={RsxVertexSourceText(instruction, instruction.Source2, 2)}");
    }

    private static string RsxVertexInstructionDetailText(RsxVertexInstruction instruction)
    {
        string branch = instruction.HasControlFlow ? $" branchTarget={instruction.BranchTarget}" : string.Empty;
        return string.Join(
            " ",
            $"{instruction.Index:000}@0x{instruction.Offset:X}",
            $"raw=w0:{instruction.Word0:X8}/w1:{instruction.Word1:X8}/w2:{instruction.Word2:X8}/w3:{instruction.Word3:X8}",
            $"last={instruction.Last}",
            $"vec={RsxVertexSlotText(instruction, scalar: false)}",
            $"sca={RsxVertexSlotText(instruction, scalar: true)}",
            $"resultIndex={instruction.ResultIndex}:{RsxVertexOutputName(instruction.ResultIndex)}",
            $"inputSrc={instruction.InputSource}:{RsxVertexInputName(instruction.InputSource)}",
            $"constSrc={instruction.ConstSource}",
            $"indexInput={instruction.IndexInput}",
            $"indexConst={instruction.IndexConst}",
            $"saturate={instruction.Saturate}",
            $"srcAbs={instruction.Source0Abs}/{instruction.Source1Abs}/{instruction.Source2Abs}",
            $"condTest={instruction.CondTestEnabled}",
            $"condUpdate={instruction.CondUpdateEnabled}",
            $"cond={RsxFragmentConditionText(instruction.ConditionCode)}.{RsxVertexConditionSwizzleText(instruction)}",
            $"addrSwz={RsxFragmentSwizzleChar(instruction.AddressSwizzle)}{branch}",
            $"src0={RsxVertexSourceText(instruction, instruction.Source0, 0)}",
            $"src1={RsxVertexSourceText(instruction, instruction.Source1, 1)}",
            $"src2={RsxVertexSourceText(instruction, instruction.Source2, 2)}",
            $"glsl={RsxVertexInstructionGlslText(instruction)}");
    }

    private static string RsxVertexSlotText(RsxVertexInstruction instruction, bool scalar)
    {
        string op = scalar ? instruction.ScaOpName : instruction.VecOpName;
        if (op == "NOP")
            return "NOP";

        string dest = RsxVertexDestinationText(instruction, scalar);
        string mask = VertexWriteMaskText(scalar ? instruction.ScaWriteMask : instruction.VecWriteMask);
        return $"{op} {dest}.{mask}";
    }

    private static string RsxVertexInstructionGlslText(RsxVertexInstruction instruction)
    {
        var parts = new List<string>(2);
        string vector = RsxVertexSlotGlslText(instruction, scalar: false);
        string scalar = RsxVertexSlotGlslText(instruction, scalar: true);
        if (vector != "nop")
            parts.Add(vector);
        if (scalar != "nop")
            parts.Add(scalar);
        return parts.Count == 0 ? "nop" : string.Join(";", parts);
    }

    private static string RsxVertexSlotGlslText(RsxVertexInstruction instruction, bool scalar)
    {
        byte opcode = scalar ? instruction.ScaOpcode : instruction.VecOpcode;
        int writeMask = scalar ? instruction.ScaWriteMask : instruction.VecWriteMask;
        if (opcode == 0 || writeMask == 0)
            return "nop";

        string expression = RsxVertexSlotExpressionText(instruction, scalar);
        if (instruction.Saturate)
            expression = $"saturate({expression})";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"vertexWrite(dest={RsxVertexDestinationText(instruction, scalar)},mask={VertexWriteMaskText(writeMask)},predicate={RsxVertexPredicateText(instruction)},ccWrite={instruction.CondUpdateEnabled},value={expression})");
    }

    private static string RsxVertexPredicateText(RsxVertexInstruction instruction)
    {
        return instruction.CondTestEnabled
            ? $"cc.{RsxVertexConditionSwizzleText(instruction)} {RsxFragmentConditionText(instruction.ConditionCode)}"
            : "TR.xyzw";
    }

    private static string RsxVertexSlotExpressionText(RsxVertexInstruction instruction, bool scalar)
    {
        if (scalar)
        {
            string source = RsxVertexSourceText(instruction, instruction.Source2, 2);
            return instruction.ScaOpcode switch
            {
                0x01 => source,
                0x02 => $"1.0 / {source}",
                0x03 => $"rcpClamped({source})",
                0x04 => $"inversesqrt({source})",
                0x05 => $"expApprox({source})",
                0x06 => $"logApprox({source})",
                0x07 => $"lit({source})",
                0x0d => $"log2({source})",
                0x0e => $"exp2({source})",
                0x0f => $"sin({source})",
                0x10 => $"cos({source})",
                _ => $"{instruction.ScaOpName.ToLowerInvariant()}({source})"
            };
        }

        string source0 = RsxVertexSourceText(instruction, instruction.Source0, 0);
        string source1 = RsxVertexSourceText(instruction, instruction.Source1, 1);
        string source2 = RsxVertexSourceText(instruction, instruction.Source2, 2);
        return instruction.VecOpcode switch
        {
            0x01 => source0,
            0x02 => $"{source0} * {source1}",
            0x03 => $"{source0} + {source1}",
            0x04 => $"{source0} * {source1} + {source2}",
            0x05 => $"splat(dot3({source0}, {source1}))",
            0x06 => $"splat(dph({source0}, {source1}))",
            0x07 => $"splat(dot4({source0}, {source1}))",
            0x08 => $"dst({source0}, {source1})",
            0x09 => $"min({source0}, {source1})",
            0x0a => $"max({source0}, {source1})",
            0x0b => $"lessThan({source0}, {source1})",
            0x0c => $"greaterThanEqual({source0}, {source1})",
            0x0d => $"addressFloor({source0})",
            0x0e => $"fract({source0})",
            0x0f => $"floor({source0})",
            0x10 => $"equal({source0}, {source1})",
            0x11 => "vec4(false)",
            0x12 => $"greaterThan({source0}, {source1})",
            0x13 => $"lessThanEqual({source0}, {source1})",
            0x14 => $"notEqual({source0}, {source1})",
            0x15 => "vec4(true)",
            0x16 => $"sign({source0})",
            0x17 => $"addressRound({source0})",
            0x18 => $"address({source0})",
            0x19 => $"textureLod(vertexSampler, {source0})",
            _ => $"{instruction.VecOpName.ToLowerInvariant()}({source0}, {source1}, {source2})"
        };
    }

    private static string RsxVertexDestinationText(RsxVertexInstruction instruction, bool scalar)
    {
        bool result = scalar ? instruction.ScaResult : instruction.VecResult;
        int temp = scalar ? instruction.ScaDestTemp : instruction.VecDestTemp;
        if (!result || instruction.ResultIndex == 0x1f)
            return $"R{temp}";

        return RsxVertexOutputName(instruction.ResultIndex);
    }

    private static IEnumerable<string> RsxVertexWrittenOutputs(RsxVertexInstruction instruction)
    {
        if (instruction.VecOpcode != 0 && instruction.VecWriteMask != 0 && instruction.VecResult && instruction.ResultIndex != 0x1f)
            yield return RsxVertexOutputName(instruction.ResultIndex);
        if (instruction.ScaOpcode != 0 && instruction.ScaWriteMask != 0 && instruction.ScaResult && instruction.ResultIndex != 0x1f)
            yield return RsxVertexOutputName(instruction.ResultIndex);
    }

    private static IEnumerable<string> RsxVertexReadInputs(RsxVertexInstruction instruction)
    {
        foreach ((uint source, int sourceIndex) in RsxVertexActiveSources(instruction))
        {
            if (RsxVertexSourceRegisterType(source) == 2)
                yield return $"{sourceIndex}:{RsxVertexInputName(instruction.InputSource)}";
        }
    }

    private static IEnumerable<string> RsxVertexReadConstants(RsxVertexInstruction instruction)
    {
        foreach ((uint source, int sourceIndex) in RsxVertexActiveSources(instruction))
        {
            if (RsxVertexSourceRegisterType(source) == 3)
                yield return $"{sourceIndex}:C{instruction.ConstSource}{(instruction.IndexConst ? "[indexed]" : string.Empty)}";
        }
    }

    private static IEnumerable<(uint Source, int SourceIndex)> RsxVertexActiveSources(RsxVertexInstruction instruction)
    {
        int vectorSourceCount = RsxVertexVectorOperandCount(instruction.VecOpcode);
        if (vectorSourceCount > 0) yield return (instruction.Source0, 0);
        if (vectorSourceCount > 1) yield return (instruction.Source1, 1);
        if (vectorSourceCount > 2) yield return (instruction.Source2, 2);
        if (RsxVertexScalarOperandCount(instruction.ScaOpcode) > 0)
            yield return (instruction.Source2, 2);
    }

    private static string RsxVertexSourceText(RsxVertexInstruction instruction, uint source, int sourceIndex)
    {
        string value = RsxVertexSourceBase(instruction, source);
        string suffix = RsxVertexSwizzleSuffix(source);
        if (!string.IsNullOrEmpty(suffix))
            value += suffix;
        if (sourceIndex switch
            {
                0 => instruction.Source0Abs,
                1 => instruction.Source1Abs,
                _ => instruction.Source2Abs
            })
        {
            value = $"|{value}|";
        }
        if ((source & 0x10000u) != 0)
            value = $"-{value}";
        return value;
    }

    private static string RsxVertexSourceBase(RsxVertexInstruction instruction, uint source)
    {
        return RsxVertexSourceRegisterType(source) switch
        {
            1 => $"R{(source >> 2) & 0x1f}",
            2 => RsxVertexInputName(instruction.InputSource),
            3 => $"C{instruction.ConstSource}{(instruction.IndexConst ? "[indexed]" : string.Empty)}",
            _ => $"sourceType{RsxVertexSourceRegisterType(source)}[{(source >> 2) & 0x1f}]"
        };
    }

    private static int RsxVertexSourceRegisterType(uint source) => (int)(source & 0x3);

    private static string RsxVertexSwizzleSuffix(uint source)
    {
        Span<char> chars = stackalloc char[4];
        chars[0] = RsxFragmentSwizzleChar((int)((source >> 14) & 0x3));
        chars[1] = RsxFragmentSwizzleChar((int)((source >> 12) & 0x3));
        chars[2] = RsxFragmentSwizzleChar((int)((source >> 10) & 0x3));
        chars[3] = RsxFragmentSwizzleChar((int)((source >> 8) & 0x3));
        return chars switch
        {
            ['x', 'y', 'z', 'w'] => string.Empty,
            ['x', 'x', 'x', 'x'] => ".x",
            ['y', 'y', 'y', 'y'] => ".y",
            ['z', 'z', 'z', 'z'] => ".z",
            ['w', 'w', 'w', 'w'] => ".w",
            _ => $".{new string(chars)}"
        };
    }

    private static string RsxVertexConditionSwizzleText(RsxVertexInstruction instruction)
    {
        Span<char> chars = stackalloc char[4];
        chars[0] = RsxFragmentSwizzleChar((int)((instruction.Word0 >> 8) & 0x3));
        chars[1] = RsxFragmentSwizzleChar((int)((instruction.Word0 >> 6) & 0x3));
        chars[2] = RsxFragmentSwizzleChar((int)((instruction.Word0 >> 4) & 0x3));
        chars[3] = RsxFragmentSwizzleChar((int)((instruction.Word0 >> 2) & 0x3));
        return new string(chars);
    }

    private static string VertexWriteMaskText(int mask)
    {
        if (mask == 0)
            return "-";

        Span<char> chars = stackalloc char[4];
        int count = 0;
        if ((mask & 0x8) != 0) chars[count++] = 'x';
        if ((mask & 0x4) != 0) chars[count++] = 'y';
        if ((mask & 0x2) != 0) chars[count++] = 'z';
        if ((mask & 0x1) != 0) chars[count++] = 'w';
        return new string(chars[..count]);
    }

    private static int RsxVertexVectorOperandCount(byte opcode)
    {
        return opcode switch
        {
            0x00 => 0,
            0x01 or 0x0d or 0x0e or 0x0f or 0x17 or 0x18 or 0x19 => 1,
            0x04 => 3,
            _ => 2
        };
    }

    private static int RsxVertexScalarOperandCount(byte opcode)
    {
        return opcode switch
        {
            0x00 or 0x09 or 0x0b or 0x0c or 0x13 or 0x14 => 0,
            _ => 1
        };
    }

    private static string RsxVertexInputName(int input)
    {
        return input switch
        {
            0x0 => "v0/POS",
            0x1 => "v1/WEIGHT",
            0x2 => "v2/NORMAL",
            0x3 => "v3/COL0",
            0x4 => "v4/COL1",
            0x5 => "v5/FOGC",
            >= 0x8 and <= 0xf => $"v{input}/TEX{input - 0x8}",
            _ => $"v{input}/input"
        };
    }

    private static string RsxVertexOutputName(int output)
    {
        return output switch
        {
            0x00 => "o0/POS",
            0x01 => "o1/COL0",
            0x02 => "o2/COL1",
            0x03 => "o3/BFC0",
            0x04 => "o4/BFC1",
            0x05 => "o5/FOGC",
            0x06 => "o6/PSZ",
            >= 0x07 and <= 0x0e => $"o{output}/TEX{output - 0x07}",
            0x1f => "temp",
            _ => $"o{output}/output"
        };
    }

    private readonly record struct RsxVertexInstruction(
        int Index,
        int Offset,
        uint Word0,
        uint Word1,
        uint Word2,
        uint Word3)
    {
        public bool Last => (Word3 & 0x1) != 0;
        public bool VecResult => (Word0 & 0x40000000u) != 0;
        public bool CondUpdateEnabled => (Word0 & 0x20004000u) != 0;
        public bool IndexInput => (Word0 & 0x08000000u) != 0;
        public bool Saturate => (Word0 & 0x04000000u) != 0;
        public bool Source2Abs => (Word0 & 0x00800000u) != 0;
        public bool Source1Abs => (Word0 & 0x00400000u) != 0;
        public bool Source0Abs => (Word0 & 0x00200000u) != 0;
        public int VecDestTemp => (int)((Word0 >> 15) & 0x3f);
        public bool CondTestEnabled => (Word0 & 0x00002000u) != 0;
        public int ConditionCode => (int)((Word0 >> 10) & 0x7);
        public int AddressSwizzle => (int)(Word0 & 0x3);
        public byte VecOpcode => (byte)((Word1 >> 22) & 0x1f);
        public byte ScaOpcode => (byte)((Word1 >> 27) & 0x1f);
        public int ConstSource => (int)((Word1 >> 12) & 0xff);
        public int InputSource => (int)((Word1 >> 8) & 0x0f);
        public uint Source0 => ((Word1 & 0xffu) << 9) | ((Word2 >> 23) & 0x1ffu);
        public uint Source1 => (Word2 >> 6) & 0x1ffffu;
        public uint Source2 => ((Word2 & 0x3fu) << 11) | ((Word3 >> 21) & 0x7ffu);
        public int BranchTarget => (int)(((Word2 & 0x3fu) << 3) | ((Word3 >> 29) & 0x7u));
        public int ScaWriteMask => (int)((Word3 >> 17) & 0x0f);
        public int VecWriteMask => (int)((Word3 >> 13) & 0x0f);
        public bool ScaResult => (Word3 & 0x00001000u) != 0;
        public int ScaDestTemp => (int)((Word3 >> 7) & 0x1f);
        public int ResultIndex => (int)((Word3 >> 2) & 0x1f);
        public bool IndexConst => (Word3 & 0x2) != 0;
        public bool HasControlFlow => ScaOpcode is 0x09 or 0x0b or 0x0c;
        public string VecOpName => RsxVertexVectorOpcodeName(VecOpcode);
        public string ScaOpName => RsxVertexScalarOpcodeName(ScaOpcode);
    }

    // PSL1GHT cgcomp writes fragment ucode with this byte-lane transform; reverse it before decoding fields.
    private static uint RsxFragmentWord(uint value)
    {
        return ((value & 0x000000ffu) << 16)
            | ((value & 0x0000ff00u) << 16)
            | ((value & 0x00ff0000u) >> 16)
            | ((value & 0xff000000u) >> 16);
    }

    private static string RsxFragmentInputName(int input)
    {
        return input switch
        {
            0x0 => "position",
            0x1 => "color0",
            0x2 => "color1",
            0x3 => "fog",
            >= 0x4 and <= 0xb => $"texcoord{input - 0x4}",
            0xe => "facing",
            _ => $"input{input:X}"
        };
    }

    private static string WriteMaskText(int mask)
    {
        if (mask == 0)
            return "-";

        Span<char> chars = stackalloc char[4];
        int count = 0;
        if ((mask & 0x1) != 0) chars[count++] = 'x';
        if ((mask & 0x2) != 0) chars[count++] = 'y';
        if ((mask & 0x4) != 0) chars[count++] = 'z';
        if ((mask & 0x8) != 0) chars[count++] = 'w';
        return new string(chars[..count]);
    }

    private static string RsxFragmentOpcodeName(byte opcode)
    {
        return opcode switch
        {
            0x00 => "NOP",
            0x01 => "MOV",
            0x02 => "MUL",
            0x03 => "ADD",
            0x04 => "MAD",
            0x05 => "DP3",
            0x06 => "DP4",
            0x07 => "DST",
            0x08 => "MIN",
            0x09 => "MAX",
            0x0a => "SLT",
            0x0b => "SGE",
            0x0c => "SLE",
            0x0d => "SGT",
            0x0e => "SNE",
            0x0f => "SEQ",
            0x10 => "FRC",
            0x11 => "FLR",
            0x12 => "KIL",
            0x13 => "PK4B",
            0x14 => "UP4B",
            0x15 => "DDX",
            0x16 => "DDY",
            0x17 => "TEX",
            0x18 => "TXP",
            0x19 => "TXD",
            0x1a => "RCP",
            0x1b => "RSQ",
            0x1c => "EX2",
            0x1d => "LG2",
            0x20 => "STR",
            0x21 => "SFL",
            0x22 => "COS",
            0x23 => "SIN",
            0x24 => "PK2H",
            0x25 => "UP2H",
            0x27 => "PK4UB",
            0x28 => "UP4UB",
            0x29 => "PK2US",
            0x2a => "UP2US",
            0x2e => "DP2A",
            0x2f => "TXL",
            0x31 => "TXB",
            0x38 => "DP2",
            0x39 => "NRM",
            0x3a => "DIV",
            0x3b => "DIVRSQ",
            0x3c => "LITEX2",
            0x3d => "op3D",
            0x3e => "op3E",
            _ => $"op{opcode:X2}"
        };
    }

    private static string RsxFragmentBranchOpcodeName(byte opcode)
    {
        return opcode switch
        {
            0x00 => "BRK",
            0x01 => "CAL",
            0x02 => "IF",
            0x03 => "LOOP",
            0x04 => "REP",
            0x05 => "RET",
            _ => $"branchOp{opcode:X2}"
        };
    }

    private static string RsxVertexVectorOpcodeName(byte opcode)
    {
        return opcode switch
        {
            0x00 => "NOP",
            0x01 => "MOV",
            0x02 => "MUL",
            0x03 => "ADD",
            0x04 => "MAD",
            0x05 => "DP3",
            0x06 => "DPH",
            0x07 => "DP4",
            0x08 => "DST",
            0x09 => "MIN",
            0x0a => "MAX",
            0x0b => "SLT",
            0x0c => "SGE",
            0x0d => "ARL",
            0x0e => "FRC",
            0x0f => "FLR",
            0x10 => "SEQ",
            0x11 => "SFL",
            0x12 => "SGT",
            0x13 => "SLE",
            0x14 => "SNE",
            0x15 => "STR",
            0x16 => "SSG",
            0x17 => "ARR",
            0x18 => "ARA",
            0x19 => "TXL",
            _ => $"vop{opcode:X2}"
        };
    }

    private static string RsxVertexScalarOpcodeName(byte opcode)
    {
        return opcode switch
        {
            0x00 => "NOP",
            0x01 => "MOV",
            0x02 => "RCP",
            0x03 => "RCC",
            0x04 => "RSQ",
            0x05 => "EXP",
            0x06 => "LOG",
            0x07 => "LIT",
            0x09 => "BRA",
            0x0b => "CAL",
            0x0c => "RET",
            0x0d => "LG2",
            0x0e => "EX2",
            0x0f => "SIN",
            0x10 => "COS",
            0x13 => "PUSHA",
            0x14 => "POPA",
            _ => $"sop{opcode:X2}"
        };
    }

    private string StateBitsText(MaterialAsset material)
    {
        string entries = string.Join(" ", material.StateBitsEntries.Select(x => $"{x.TechniqueSlot}:{x.StateBitsIndex}"));
        string inline = string.Join(" ", material.InlineTechniqueSlotStateBits.Select((x, i) => x == 0 ? null : $"{i}:0x{x:X4}").Where(x => x is not null));
        string runtime = string.Join(" ", material.RuntimeTechniqueSlotStateBits.Select((x, i) => x == 0 ? null : $"{i}:0x{x:X4}").Where(x => x is not null));
        string bits = string.Join(" ", material.StateBits.Select((x, i) => $"{i}:load=[{string.Join("/", _assets.ResolveStateLoadBits(x).Select(y => $"0x{y:X8}"))}] tail=0x{x.Tail:X8}"));
        return $"entries=[{entries}] inline=[{inline}] runtime=[{runtime}] bits=[{bits}]";
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

    private static string ConstantListText(MaterialAsset material)
    {
        return string.Join(
            "; ",
            material.Constants.Select(constant =>
                $"hash=0x{constant.NameHash:X8} nameBytes={HexBytes(constant.NameBytes, constant.NameBytes.Count)} value=({Vec4Text(constant.Literal).Replace(' ', '/')})"));
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
                            return $"p{index}:{ShaderText(pass.VertexShader)}/{ShaderText(pass.PixelShader)} {routing} args={pass.PerPrimArgCount}+{pass.PerObjArgCount}+{pass.StableArgCount} [{ShaderArgsText(pass, null)}]";
                        }));
                    return $"slot{slot.Index}:{slot.Technique!.Name}[{passes}]";
                }));
    }

    private static string ShaderText(MaterialShaderAsset? shader)
    {
        if (shader is null)
            return string.Empty;

        string prefix = shader.Data is { Length: > 0 } data
            ? HexBytes(data, Math.Min(16, data.Length))
            : string.Empty;
        return $"{shader.Name}#bytes={shader.DataSize}:0x{prefix}";
    }

    private static string ShaderArgsText(MaterialPassAsset pass, MaterialAsset? material)
    {
        Dictionary<uint, MaterialTextureDef> texturesByHash = material?.Textures
            .GroupBy(texture => texture.NameHash)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];
        Dictionary<uint, MaterialConstantDef> constantsByHash = material?.Constants
            .GroupBy(constant => constant.NameHash)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        return string.Join(" ", pass.Args.Select((arg, index) =>
        {
            uint raw = unchecked((uint)arg.ArgumentRaw);
            string value = $"#{index}:t={arg.Type}/0x{(ushort)arg.Type:X4},d=0x{arg.Dest:X4},v=0x{arg.ArgumentRaw:X8}";
            if (arg.Type == MaterialShaderArgumentType.MaterialPixelSampler &&
                texturesByHash.TryGetValue(raw, out MaterialTextureDef? texture))
                value += $",tex={TextureSlotText(texture)}";
            if (arg.Type == MaterialShaderArgumentType.MaterialPixelConst &&
                constantsByHash.TryGetValue(raw, out MaterialConstantDef? constant))
                value += $",const={ConstantName(constant)}({Vec4Text(constant.Literal).Replace(' ', '/')})";
            return arg.LiteralConstant is { } literal
                ? $"{value},lit=({literal.X.ToString("G4", CultureInfo.InvariantCulture)}/{literal.Y.ToString("G4", CultureInfo.InvariantCulture)}/{literal.Z.ToString("G4", CultureInfo.InvariantCulture)}/{literal.W.ToString("G4", CultureInfo.InvariantCulture)})"
                : value;
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

    private RenderStateDescriptor DescribeRenderState(MaterialAsset? material, MaterialTechniqueSetAsset? techset)
    {
        bool additive = material is not null && MaterialSlotUsesLoadBits(material, techniqueSlot: 4, firstLoadWord: 0x00000550);
        bool falloff = TechniqueUsesRoute(techset, source: 0x03, dest: 0x02);
        bool vertexColor = TechniqueUsesRoute(techset, source: 0x01, dest: 0x03);

        return new RenderStateDescriptor(
            additive ? "additive" : "opaque",
            additive && falloff ? "intensity" : "one",
            vertexColor);
    }

    private bool MaterialSlotUsesLoadBits(MaterialAsset material, int techniqueSlot, uint firstLoadWord)
    {
        if (techniqueSlot < 0 || techniqueSlot >= material.StateBitsEntries.Count)
            return false;

        byte stateIndex = material.StateBitsEntries[techniqueSlot].StateBitsIndex;
        if (stateIndex == byte.MaxValue || stateIndex >= material.StateBits.Count)
            return false;

        IReadOnlyList<uint> loadBits = _assets.ResolveStateLoadBits(material.StateBits[stateIndex]);
        return loadBits.Count > 0 && loadBits[0] == firstLoadWord;
    }

    private bool TechniqueUsesRoute(MaterialTechniqueSetAsset? techset, byte source, byte dest)
    {
        if (techset is null)
            return false;

        return techset.TechniqueSlots
            .Where(slot => slot.Technique is not null)
            .SelectMany(slot => slot.Technique!.Passes)
            .Select(pass => pass.VertexDeclaration ?? _assets.ResolveVertexDeclaration(pass.VertexDeclPointer))
            .Where(decl => decl is not null)
            .Any(decl => decl!.Routing.Take(decl.StreamCount).Any(route => route.Source == source && route.Dest == dest));
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
            0x0D => "color[1]",
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

    private static uint Fnv1A32(IReadOnlyList<byte> bytes)
    {
        uint hash = 2166136261;
        foreach (byte value in bytes)
        {
            hash ^= value;
            hash *= 16777619;
        }

        return hash;
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

    private static string HexBytes(IReadOnlyList<byte> bytes, int count)
    {
        return HexSlice(bytes, 0, count);
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
    IReadOnlyList<SkyImageDebugRow> SkyImages,
    IReadOnlyList<WorldSurfaceDebugRow> Surfaces);

internal sealed record SkyImageDebugRow(
    string ImageName,
    string Face,
    string SamplerState,
    string Path,
    bool Decoded,
    string DecodeReason);

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
    string RenderBlend,
    string RenderAlpha,
    bool UsesVertexColor,
    string SelectedUvDecoder,
    string TextureSemantic,
    string TextureSlot,
    string TextureSamplerState,
    string TextureImage,
    bool TextureDecoded,
    string TextureDecodeReason,
    string LayerTextureSlot,
    string LayerTextureSamplerState,
    string LayerTextureImage,
    string LayerTexturePath,
    bool LayerTextureDecoded,
    string LayerTextureDecodeReason,
    string LayerBaseTint,
    string LayerTextureTint,
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
    string PngPath,
    bool HasTransparency);

internal readonly record struct RenderStateDescriptor(
    string Blend,
    string Alpha,
    bool UsesVertexColor);

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

internal readonly record struct LayerStreamKey(
    int VertexLayerData,
    int BaseVertex);

internal enum WorldColorPacking
{
    RawRgba,
    D3DArgb
}

internal sealed class WorldLayerColorDecoder
{
    private readonly IReadOnlyList<byte> _layer;
    private readonly IReadOnlyDictionary<LayerStreamKey, int> _layerBaseIndices;
    private readonly int _stride;
    private readonly int _offset;
    private readonly WorldColorPacking _packing;
    private readonly bool _invert;

    public WorldLayerColorDecoder(
        IReadOnlyList<byte> layer,
        IReadOnlyDictionary<LayerStreamKey, int> layerBaseIndices,
        int stride,
        int offset,
        WorldColorPacking packing,
        bool invert)
    {
        _layer = layer;
        _layerBaseIndices = layerBaseIndices;
        _stride = stride;
        _offset = offset;
        _packing = packing;
        _invert = invert;
    }

    public Rgba Read(GfxSurface surface, int surfaceIndex)
    {
        int baseIndex = _layerBaseIndices.TryGetValue(new LayerStreamKey(surface.Triangles.VertexLayerData, surface.Triangles.BaseVertex), out int value)
            ? value
            : checked((int)surface.Triangles.MinVertexIndex);
        int offset = checked(surface.Triangles.VertexLayerData + (surfaceIndex - baseIndex) * _stride + _offset);
        if (offset < 0 || offset + 4 > _layer.Count)
            return new Rgba(1, 1, 1, 1);

        float r;
        float g;
        float b;
        float a;
        if (_packing == WorldColorPacking.D3DArgb)
        {
            a = _layer[offset] / 255.0f;
            r = _layer[offset + 1] / 255.0f;
            g = _layer[offset + 2] / 255.0f;
            b = _layer[offset + 3] / 255.0f;
        }
        else
        {
            r = _layer[offset] / 255.0f;
            g = _layer[offset + 1] / 255.0f;
            b = _layer[offset + 2] / 255.0f;
            a = _layer[offset + 3] / 255.0f;
        }

        if (_invert)
        {
            r = 1.0f - r;
            g = 1.0f - g;
            b = 1.0f - b;
            a = 1.0f - a;
        }

        return new Rgba(
            r,
            g,
            b,
            a);
    }
}

internal sealed class WorldTexCoordDecoder
{
    private readonly IReadOnlyList<byte> _layer;
    private readonly IReadOnlyDictionary<LayerStreamKey, int> _layerBaseIndices;
    private readonly UvCandidate _candidate;
    private readonly float _maxMagnitude;

    public WorldTexCoordDecoder(
        IReadOnlyList<byte> layer,
        IReadOnlyDictionary<LayerStreamKey, int> layerBaseIndices,
        UvCandidate candidate,
        float maxMagnitude)
    {
        _layer = layer;
        _layerBaseIndices = layerBaseIndices;
        _candidate = candidate;
        _maxMagnitude = maxMagnitude;
    }

    public bool TryRead(GfxSurface surface, int surfaceIndex, int globalVertex, out Vec2f texCoord)
    {
        texCoord = default;
        int localVertex = checked(surfaceIndex - SurfaceMinVertexIndex(surface.Triangles));
        int layerBaseIndex = LayerBaseIndex(surface.Triangles);
        if (!_candidate.HasValidIndex(surface.Triangles, surfaceIndex, localVertex, layerBaseIndex, globalVertex, int.MaxValue))
            return false;

        int offset = _candidate.GetOffset(surface.Triangles, surfaceIndex, localVertex, layerBaseIndex, globalVertex);
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

    public int LayerBaseIndex(SrfTriangles triangles)
    {
        return _layerBaseIndices.TryGetValue(new LayerStreamKey(triangles.VertexLayerData, triangles.BaseVertex), out int baseIndex)
            ? baseIndex
            : SurfaceMinVertexIndex(triangles);
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
        (IndexMode == UvIndexMode.Local || IndexMode == UvIndexMode.LayerData);

    public bool IsLayerStreamIndexed =>
        BaseScale == 1 &&
        IndexMode == UvIndexMode.SurfaceIndex;

    public bool HasValidIndex(
        SrfTriangles triangles,
        int surfaceIndex,
        int localVertex,
        int layerBaseIndex,
        int globalVertex,
        int vertexCount)
    {
        return IndexMode switch
        {
            UvIndexMode.Local => localVertex >= 0 && localVertex < triangles.VertexCount,
            UvIndexMode.LayerData => surfaceIndex >= layerBaseIndex,
            UvIndexMode.GlobalVertex => globalVertex >= 0 && globalVertex < vertexCount,
            _ => true
        };
    }

    public int GetOffset(
        SrfTriangles triangles,
        int surfaceIndex,
        int localVertex,
        int layerBaseIndex,
        int globalVertex)
    {
        int layerIndex = IndexMode switch
        {
            UvIndexMode.Local => localVertex,
            UvIndexMode.LayerData => checked(surfaceIndex - layerBaseIndex),
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
                    foreach (UvIndexMode indexMode in new[] { UvIndexMode.Local, UvIndexMode.LayerData, UvIndexMode.SurfaceIndex })
                    {
                        candidates.Add(new UvCandidate(stride, offset, true, baseScale, indexMode));
                        candidates.Add(new UvCandidate(stride, offset, false, baseScale, indexMode));
                    }
                }

                for (int offset = 0; offset <= stride - 8; offset += 4)
                {
                    foreach (UvIndexMode indexMode in new[] { UvIndexMode.Local, UvIndexMode.LayerData, UvIndexMode.SurfaceIndex })
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
    LayerData,
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
