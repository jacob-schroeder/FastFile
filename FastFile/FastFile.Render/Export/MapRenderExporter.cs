using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using FastFile.Models.Assets.ColMap;
using FastFile.Models.Assets.GfxMap;
using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;
using FastFile.Render.Glb;
using ModelVec3 = FastFile.Models.Math.Vec3;

namespace FastFile.Render.Export;

internal sealed class MapRenderExporter
{
    private readonly RenderOptions _options;
    private readonly RenderAssetLookup _assets;
    private readonly GfxImageStreamResolver _imageStreams;
    private readonly Dictionary<string, int> _textureByImageAddress = new();

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

        return summary;
    }

    private void ExportGfxMap(
        string stem,
        GfxWorldAsset gfxMap,
        MapRenderSummary summary)
    {
        List<Vec3f> positions = DecodeWorldPositions(gfxMap.WorldDraw.VertexData.PackedVertices);
        List<Vec2f>? texCoords = DecodeWorldTexCoords(gfxMap, summary);
        var builder = new GlbSceneBuilder("FastFile.Render GfxMap export");
        int positionAccessor = builder.AddPositions(positions);
        int? texCoordAccessor = texCoords is null ? null : builder.AddTexCoords(texCoords);
        int mesh = builder.AddMesh($"{NameOrStem(gfxMap.Name, stem)} GfxMap");
        var materialRows = new List<MaterialTextureReportRow>();

        int skippedIndices = 0;
        foreach (IGrouping<string, GfxSurface> group in gfxMap.Dpvs.Surfaces.GroupBy(MaterialKey).OrderBy(x => x.Key))
        {
            List<uint> indices = new();
            foreach (GfxSurface surface in group)
            {
                SrfTriangles triangles = surface.Triangles;
                int indexCount = checked(triangles.TriCount * 3);
                if (triangles.BaseIndex < 0 || triangles.BaseIndex + indexCount > gfxMap.WorldDraw.Indices.Count)
                {
                    skippedIndices += indexCount;
                    continue;
                }

                for (int i = 0; i < indexCount; i++)
                {
                    uint index = checked((uint)(triangles.FirstVertex + gfxMap.WorldDraw.Indices[triangles.BaseIndex + i]));
                    if (index < positions.Count)
                        indices.Add(index);
                    else
                        skippedIndices++;
                }
            }

            if (indices.Count == 0)
                continue;

            MaterialAsset? resolvedMaterial = group.Select(ResolveSurfaceMaterial).FirstOrDefault(x => x is not null);
            TextureBinding? texture = resolvedMaterial is null ? null : BindBaseColorTexture(builder, resolvedMaterial, summary);
            materialRows.Add(new MaterialTextureReportRow(group.Key, group.Count(), resolvedMaterial, texture));

            int material = builder.AddMaterial(
                group.Key,
                texture is null ? ColorFromString(group.Key, 0.82f) : new Rgba(1.0f, 1.0f, 1.0f, 0.96f),
                texture?.GlbTextureIndex);
            builder.AddPrimitive(
                mesh,
                positionAccessor,
                indices,
                GlbPrimitiveMode.Triangles,
                material,
                texCoordAccessor: texture is null ? null : texCoordAccessor);
        }

        builder.AddNode($"{NameOrStem(gfxMap.Name, stem)}_gfx", mesh);
        string outputPath = Path.Combine(_options.OutputDirectory, $"{stem}.gfx.glb");
        builder.Write(outputPath);
        WriteMaterialTextureReport(stem, materialRows, summary);

        summary.WrittenFiles.Add(outputPath);
        summary.GfxVertexCount = positions.Count;
        summary.GfxIndexCount = gfxMap.WorldDraw.Indices.Count;
        summary.GfxSurfaceCount = gfxMap.Dpvs.Surfaces.Count;
        summary.GfxVertexLayerByteCount = gfxMap.WorldDraw.VertexLayerData.PackedLayerData.Count;
        summary.RuntimeMaterialCount = _assets.MaterialCount;
        summary.RuntimeImageCount = _assets.ImageCount;
        if (skippedIndices > 0)
            summary.Warnings.Add($"Skipped {skippedIndices} GfxMap index value(s) outside the decoded vertex range.");
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

    private List<Vec2f>? DecodeWorldTexCoords(GfxWorldAsset gfxMap, MapRenderSummary summary)
    {
        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        int vertexCount = gfxMap.WorldDraw.VertexData.PackedVertices.Count / 0x10;
        if (layer.Count == 0 || vertexCount == 0)
        {
            summary.TexCoordStatus = "no layer data";
            return null;
        }

        // ponytail: heuristic until the PS3 vertex-layer consumers prove the exact stride/member.
        UvDecodeResult best = default;
        foreach (UvCandidate candidate in UvCandidate.Candidates)
        {
            UvDecodeResult result = TryDecodeWorldTexCoords(gfxMap, candidate, vertexCount);
            if (result.Score > best.Score)
                best = result;
        }

        summary.TexCoordStatus = $"candidate stride={best.Candidate.Stride} offset={best.Candidate.Offset} endian={(best.Candidate.BigEndian ? "BE" : "LE")} assigned={best.AssignedVertices}/{vertexCount} conflicts={best.Conflicts}";
        if (best.AssignedVertices < vertexCount / 4)
            return null;

        return best.TexCoords?.ToList();
    }

    private static UvDecodeResult TryDecodeWorldTexCoords(
        GfxWorldAsset gfxMap,
        UvCandidate candidate,
        int vertexCount)
    {
        IReadOnlyList<byte> layer = gfxMap.WorldDraw.VertexLayerData.PackedLayerData;
        var texCoords = new Vec2f[vertexCount];
        var assigned = new bool[vertexCount];
        int assignedCount = 0;
        int conflicts = 0;
        int invalid = 0;

        foreach (GfxSurface surface in gfxMap.Dpvs.Surfaces)
        {
            SrfTriangles triangles = surface.Triangles;
            for (int local = 0; local < triangles.VertexCount; local++)
            {
                int vertex = triangles.FirstVertex + local;
                int offset = triangles.VertexLayerData + local * candidate.Stride + candidate.Offset;
                if (vertex < 0 || vertex >= vertexCount || offset < 0 || offset + 4 > layer.Count)
                {
                    invalid++;
                    continue;
                }

                float u = ReadHalf(layer, offset, candidate.BigEndian);
                float v = ReadHalf(layer, offset + 2, candidate.BigEndian);
                if (!float.IsFinite(u) || !float.IsFinite(v) || MathF.Abs(u) > 4096 || MathF.Abs(v) > 4096)
                {
                    invalid++;
                    continue;
                }

                var texCoord = new Vec2f(u, v);
                if (!assigned[vertex])
                {
                    texCoords[vertex] = texCoord;
                    assigned[vertex] = true;
                    assignedCount++;
                }
                else if (MathF.Abs(texCoords[vertex].U - texCoord.U) > 0.001f ||
                         MathF.Abs(texCoords[vertex].V - texCoord.V) > 0.001f)
                {
                    conflicts++;
                }
            }
        }

        int score = assignedCount - conflicts * 8 - invalid / 16;
        return new UvDecodeResult(candidate, texCoords, assignedCount, conflicts, score);
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
        if (_textureByImageAddress.TryGetValue(cacheKey, out int existingTexture))
            return new TextureBinding(binding.Texture, binding.Image, existingTexture, null, true, null);

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
            return new TextureBinding(binding.Texture, binding.Image, null, null, false, reason);
        }

        if (!GfxImageDecoder.TryDecodePng(binding.Image, payload, width, height, out DecodedGfxImage decoded, out reason))
        {
            summary.TextureDecodeSkippedCount++;
            return new TextureBinding(binding.Texture, binding.Image, null, null, false, reason);
        }

        string textureDirectory = Path.Combine(_options.OutputDirectory, "textures");
        Directory.CreateDirectory(textureDirectory);
        string pngPath = Path.Combine(textureDirectory, $"{SafeFileName(cacheKey)}.png");
        File.WriteAllBytes(pngPath, decoded.PngBytes);
        int glbTexture = builder.AddPngTexture(decoded.Name, decoded.PngBytes);
        _textureByImageAddress[cacheKey] = glbTexture;
        summary.DecodedTextureCount++;
        summary.TextureOutputDirectory = textureDirectory;
        return new TextureBinding(binding.Texture, binding.Image, glbTexture, pngPath, true, null);
    }

    private (MaterialTextureDef Texture, GfxImageAsset Image)? SelectBaseColorTexture(MaterialAsset material)
    {
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
        writer.WriteLine("materialName,surfaceCount,resolvedMaterial,textureSemantic,imageName,width,height,baseWidth,baseHeight,format,payloadBytes,streamData,decoded,pngPath,decodeReason");
        foreach (MaterialTextureReportRow row in rows)
        {
            TextureBinding? texture = row.Texture;
            writer.WriteLine(string.Join(
                ",",
                Csv(row.MaterialName),
                row.SurfaceCount.ToString(CultureInfo.InvariantCulture),
                Csv(row.Material?.Info.Name),
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
                Csv(texture?.PngPath),
                Csv(texture?.DecodeReason)));
        }

        summary.WrittenFiles.Add(outputPath);
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

internal readonly record struct TextureBinding(
    MaterialTextureDef Texture,
    GfxImageAsset Image,
    int? GlbTextureIndex,
    string? PngPath,
    bool Decoded,
    string? DecodeReason);

internal readonly record struct MaterialTextureReportRow(
    string MaterialName,
    int SurfaceCount,
    MaterialAsset? Material,
    TextureBinding? Texture);

internal readonly record struct UvCandidate(int Stride, int Offset, bool BigEndian)
{
    public static readonly IReadOnlyList<UvCandidate> Candidates =
    [
        new(4, 0, true),
        new(4, 0, false),
        new(8, 0, true),
        new(8, 4, true),
        new(8, 0, false),
        new(8, 4, false),
        new(12, 0, true),
        new(12, 4, true),
        new(12, 8, true),
        new(16, 0, true),
        new(16, 4, true),
        new(16, 8, true),
        new(16, 12, true)
    ];
}

internal readonly record struct UvDecodeResult(
    UvCandidate Candidate,
    IReadOnlyList<Vec2f>? TexCoords,
    int AssignedVertices,
    int Conflicts,
    int Score);

internal static class Vec3fTupleExtensions
{
    public static (Vec3f Min, Vec3f Max) Normalize(this (Vec3f Min, Vec3f Max) value)
    {
        return (Vec3f.Min(value.Min, value.Max), Vec3f.Max(value.Min, value.Max));
    }
}

internal readonly record struct Rgba(float R, float G, float B, float A);
