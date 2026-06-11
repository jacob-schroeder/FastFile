using System;
using System.Collections.Generic;
using System.Numerics;

namespace UI.Views.Assets;

public sealed class XModelRenderMesh
{
    public XModelRenderMesh(
        string modelName,
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<XModelRenderEdge> edges,
        int surfaceCount,
        int triangleCount,
        string status)
    {
        ModelName = modelName;
        Vertices = vertices;
        Edges = edges;
        SurfaceCount = surfaceCount;
        TriangleCount = triangleCount;
        Status = status;
        (Center, Radius) = CalculateBounds(vertices);
    }

    public string ModelName { get; }

    public IReadOnlyList<Vector3> Vertices { get; }

    public IReadOnlyList<XModelRenderEdge> Edges { get; }

    public int SurfaceCount { get; }

    public int TriangleCount { get; }

    public int VertexCount => Vertices.Count;

    public int EdgeCount => Edges.Count;

    public string Status { get; }

    public Vector3 Center { get; }

    public float Radius { get; }

    public bool HasGeometry => Vertices.Count > 0;

    private static (Vector3 Center, float Radius) CalculateBounds(IReadOnlyList<Vector3> vertices)
    {
        if (vertices.Count == 0)
        {
            return (Vector3.Zero, 1f);
        }

        var min = vertices[0];
        var max = vertices[0];

        for (var i = 1; i < vertices.Count; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        var center = (min + max) * 0.5f;
        var radius = 0f;

        foreach (var vertex in vertices)
        {
            radius = MathF.Max(radius, Vector3.Distance(center, vertex));
        }

        return (center, MathF.Max(radius, 0.001f));
    }
}

public readonly record struct XModelRenderEdge(int A, int B);
