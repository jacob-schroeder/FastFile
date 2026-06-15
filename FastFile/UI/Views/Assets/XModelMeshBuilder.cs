using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace UI.Views.Assets;

public static class XModelMeshBuilder
{
    private const int VertexStride = 0x10;

    public static XModelRenderMesh Build(XModel model)
    {
        var surfaces = ResolveSurfaces(model).ToArray();
        var vertices = new List<Vector3>();
        var edges = new List<XModelRenderEdge>();
        var triangles = new List<XModelRenderTriangle>();
        var materials = new List<XModelRenderMaterial>();
        var materialIndices = new Dictionary<MaterialAsset, int>();
        var fallbackMaterialIndices = new Dictionary<int, int>();
        var edgeKeys = new HashSet<long>();
        var triangleCount = 0;
        var decodeModes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var resolvedSurface in surfaces)
        {
            var surface = resolvedSurface.Surface;
            var surfaceVertexBytes = ResolveVertexBytes(surface);
            var firstVertex = vertices.Count;
            var decoded = DecodeVertices(model.Bounds, surface, surfaceVertexBytes);

            if (decoded.Vertices.Count == 0)
            {
                continue;
            }

            vertices.AddRange(decoded.Vertices);
            decodeModes.Add(decoded.Mode);
            triangleCount += surface.TriCount;

            if (surface.TriIndices is { IsResolved: true, Value: { } indices } && indices.Length >= 3)
            {
                var materialIndex = ResolveMaterialIndex(
                    model,
                    resolvedSurface.MaterialSlot,
                    materials,
                    materialIndices,
                    fallbackMaterialIndices);
                var indexCount = Math.Min(indices.Length, surface.TriIndexCount);
                for (var i = 0; i + 2 < indexCount; i += 3)
                {
                    AddTriangle(triangles, firstVertex, decoded.Vertices.Count, indices[i], indices[i + 1], indices[i + 2], materialIndex);
                    AddEdge(edges, edgeKeys, firstVertex, decoded.Vertices.Count, indices[i], indices[i + 1]);
                    AddEdge(edges, edgeKeys, firstVertex, decoded.Vertices.Count, indices[i + 1], indices[i + 2]);
                    AddEdge(edges, edgeKeys, firstVertex, decoded.Vertices.Count, indices[i + 2], indices[i]);
                }
            }
        }

        var modelName = string.IsNullOrWhiteSpace(model.Name)
            ? "(unnamed xmodel)"
            : model.Name;
        var status = surfaces.Length == 0
            ? "No resolved XSurface records in this fastfile"
            : decodeModes.Count == 0
                ? "No resolved XSurface vertex buffers"
                : string.Join(", ", decodeModes.OrderBy(mode => mode, StringComparer.Ordinal));

        return new XModelRenderMesh(
            modelName,
            vertices,
            edges,
            triangles,
            materials,
            surfaces.Length,
            triangleCount,
            status);
    }

    private static IEnumerable<ResolvedSurface> ResolveSurfaces(XModel model)
    {
        foreach (var lod in model.LodInfo ?? [])
        {
            if (lod is null || lod.NumSurfs == 0)
            {
                continue;
            }

            if (TryGetResolved(lod.Surfs, out var directSurfs))
            {
                for (var i = 0; i < directSurfs.Length; i++)
                {
                    var surface = directSurfs[i];
                    if (surface is not null)
                    {
                        yield return new ResolvedSurface(surface, lod.SurfIndex + i);
                    }
                }

                yield break;
            }

            if (TryGetResolved(lod.ModelSurfs, out var modelSurfs) &&
                TryGetResolved(modelSurfs.Surfs, out var assetSurfs))
            {
                for (var i = 0; i < assetSurfs.Length; i++)
                {
                    var surface = assetSurfs[i];
                    if (surface is not null)
                    {
                        yield return new ResolvedSurface(surface, lod.SurfIndex + i);
                    }
                }

                yield break;
            }
        }
    }

    private static int ResolveMaterialIndex(
        XModel model,
        int materialSlot,
        IList<XModelRenderMaterial> materials,
        IDictionary<MaterialAsset, int> materialIndices,
        IDictionary<int, int> fallbackMaterialIndices)
    {
        var material = ResolveMaterial(model, materialSlot);
        if (material is null)
        {
            if (fallbackMaterialIndices.TryGetValue(materialSlot, out var fallbackIndex))
            {
                return fallbackIndex;
            }

            fallbackIndex = materials.Count;
            fallbackMaterialIndices.Add(materialSlot, fallbackIndex);
            var color = MaterialPreviewHelper.ResolveSurfaceColor(null, materialSlot);
            materials.Add(new XModelRenderMaterial(
                $"Surface material {materialSlot:N0}",
                color.Color,
                color.Source,
                color.IsDecodedTexture));
            return fallbackIndex;
        }

        if (materialIndices.TryGetValue(material, out var index))
        {
            return index;
        }

        index = materials.Count;
        materialIndices.Add(material, index);
        var materialColor = MaterialPreviewHelper.ResolveSurfaceColor(material, materialSlot);
        materials.Add(new XModelRenderMaterial(
            string.IsNullOrWhiteSpace(material.GetDisplayName) ? $"Material {materialSlot:N0}" : material.GetDisplayName,
            materialColor.Color,
            materialColor.Source,
            materialColor.IsDecodedTexture));
        return index;
    }

    private static MaterialAsset? ResolveMaterial(XModel model, int materialSlot)
    {
        if (model.MaterialHandles is not { IsResolved: true, Value: { Length: > 0 } handles })
        {
            return null;
        }

        if (materialSlot < 0 || materialSlot >= handles.Length)
        {
            return null;
        }

        return handles[materialSlot].Value;
    }

    private static bool TryGetResolved<T>(XPointer<T>? pointer, out T value)
    {
        if (pointer is { IsResolved: true, Value: { } resolved })
        {
            value = resolved;
            return true;
        }

        value = default!;
        return false;
    }

    private static byte[]? ResolveVertexBytes(XSurface surface)
    {
        if (surface.Verts0 is { IsResolved: true, Value: { Length: > 0 } verts0 })
        {
            return verts0;
        }

        if (surface.Verts1 is { IsResolved: true, Value: { Length: > 0 } verts1 })
        {
            return verts1;
        }

        return null;
    }

    private static DecodedVertices DecodeVertices(Bounds? bounds, XSurface surface, byte[]? bytes)
    {
        if (bytes is null || surface.VertCount == 0)
        {
            return new DecodedVertices([], "missing vertex bytes");
        }

        var count = Math.Min(surface.VertCount, bytes.Length / VertexStride);
        if (count == 0)
        {
            return new DecodedVertices([], "short vertex buffer");
        }

        var candidates = new[]
        {
            DecodeAsBoundedInt16(bounds, bytes, count),
            DecodeAsHalfFloat(bytes, count),
            DecodeAsFloat(bytes, count)
        };

        return candidates
            .Where(candidate => candidate.Vertices.Count == count && IsUsable(candidate.Vertices))
            .OrderBy(candidate => ScoreCandidate(bounds, candidate.Vertices))
            .FirstOrDefault()
            ?? new DecodedVertices([], "unusable vertex data");
    }

    private static DecodedVertices DecodeAsBoundedInt16(Bounds? bounds, byte[] bytes, int count)
    {
        var vertices = new List<Vector3>(count);
        var hasBounds = TryGetUsableBounds(bounds, out var mid, out var half);

        for (var i = 0; i < count; i++)
        {
            var offset = i * VertexStride;
            var packed = new Vector3(
                BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset, 2)) / 32767f,
                BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset + 2, 2)) / 32767f,
                BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset + 4, 2)) / 32767f);

            vertices.Add(hasBounds
                ? mid + (packed * half)
                : packed);
        }

        return new DecodedVertices(vertices, hasBounds ? "bounds-normalized int16" : "normalized int16");
    }

    private static DecodedVertices DecodeAsHalfFloat(byte[] bytes, int count)
    {
        var vertices = new List<Vector3>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = i * VertexStride;
            vertices.Add(new Vector3(
                (float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2))),
                (float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 2, 2))),
                (float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset + 4, 2)))));
        }

        return new DecodedVertices(vertices, "big-endian half");
    }

    private static DecodedVertices DecodeAsFloat(byte[] bytes, int count)
    {
        var vertices = new List<Vector3>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = i * VertexStride;
            vertices.Add(new Vector3(
                ReadSingleBigEndian(bytes.AsSpan(offset, 4)),
                ReadSingleBigEndian(bytes.AsSpan(offset + 4, 4)),
                ReadSingleBigEndian(bytes.AsSpan(offset + 8, 4))));
        }

        return new DecodedVertices(vertices, "big-endian float");
    }

    private static float ReadSingleBigEndian(ReadOnlySpan<byte> bytes)
    {
        return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(bytes));
    }

    private static bool TryGetUsableBounds(Bounds? bounds, out Vector3 mid, out Vector3 half)
    {
        mid = Vector3.Zero;
        half = Vector3.One;

        if (bounds is null)
        {
            return false;
        }

        mid = ToVector(bounds.MidPoint);
        half = ToVector(bounds.HalfSize);

        return IsFinite(mid) &&
               IsFinite(half) &&
               half.X > 0.0001f &&
               half.Y > 0.0001f &&
               half.Z > 0.0001f &&
               half.Length() < 100000f;
    }

    private static Vector3 ToVector(Vec3 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    private static bool IsUsable(IReadOnlyList<Vector3> vertices)
    {
        if (vertices.Count == 0 || vertices.Any(vertex => !IsFinite(vertex)))
        {
            return false;
        }

        var min = vertices[0];
        var max = vertices[0];
        for (var i = 1; i < vertices.Count; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        var size = max - min;
        return size.Length() is > 0.0001f and < 1000000f;
    }

    private static double ScoreCandidate(Bounds? bounds, IReadOnlyList<Vector3> vertices)
    {
        if (!TryGetUsableBounds(bounds, out var mid, out var half))
        {
            return Radius(vertices);
        }

        var expectedRadius = half.Length();
        var center = Center(vertices);
        var centerPenalty = Vector3.Distance(center, mid) / Math.Max(expectedRadius, 0.001f);
        var radiusPenalty = Math.Abs(Radius(vertices) - expectedRadius) / Math.Max(expectedRadius, 0.001f);

        return centerPenalty + radiusPenalty;
    }

    private static Vector3 Center(IReadOnlyList<Vector3> vertices)
    {
        var min = vertices[0];
        var max = vertices[0];

        for (var i = 1; i < vertices.Count; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        return (min + max) * 0.5f;
    }

    private static float Radius(IReadOnlyList<Vector3> vertices)
    {
        var center = Center(vertices);
        var radius = 0f;

        foreach (var vertex in vertices)
        {
            radius = Math.Max(radius, Vector3.Distance(center, vertex));
        }

        return radius;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.X) &&
               float.IsFinite(value.Y) &&
               float.IsFinite(value.Z);
    }

    private static void AddEdge(
        ICollection<XModelRenderEdge> edges,
        ISet<long> edgeKeys,
        int firstVertex,
        int vertexCount,
        ushort indexA,
        ushort indexB)
    {
        if (indexA >= vertexCount || indexB >= vertexCount || indexA == indexB)
        {
            return;
        }

        var a = firstVertex + indexA;
        var b = firstVertex + indexB;
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        var key = ((long)min << 32) | (uint)max;

        if (edgeKeys.Add(key))
        {
            edges.Add(new XModelRenderEdge(a, b));
        }
    }

    private static void AddTriangle(
        ICollection<XModelRenderTriangle> triangles,
        int firstVertex,
        int vertexCount,
        ushort indexA,
        ushort indexB,
        ushort indexC,
        int materialIndex)
    {
        if (indexA >= vertexCount ||
            indexB >= vertexCount ||
            indexC >= vertexCount ||
            indexA == indexB ||
            indexB == indexC ||
            indexA == indexC)
        {
            return;
        }

        triangles.Add(new XModelRenderTriangle(
            firstVertex + indexA,
            firstVertex + indexB,
            firstVertex + indexC,
            materialIndex));
    }

    private sealed record ResolvedSurface(XSurface Surface, int MaterialSlot);

    private sealed record DecodedVertices(IReadOnlyList<Vector3> Vertices, string Mode);
}
