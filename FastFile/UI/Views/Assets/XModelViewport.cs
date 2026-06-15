using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UI.Views.Assets;

public sealed class XModelViewport : Control
{
    public static readonly StyledProperty<XModelRenderMesh?> MeshProperty =
        AvaloniaProperty.Register<XModelViewport, XModelRenderMesh?>(nameof(Mesh));

    public static readonly StyledProperty<double> YawProperty =
        AvaloniaProperty.Register<XModelViewport, double>(nameof(Yaw), 35);

    public static readonly StyledProperty<double> PitchProperty =
        AvaloniaProperty.Register<XModelViewport, double>(nameof(Pitch), -18);

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<XModelViewport, double>(nameof(Zoom), 1);

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(22, 23, 26));
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 82, 87, 96)), 1);
    private static readonly IPen EdgePen = new Pen(new SolidColorBrush(Color.FromRgb(143, 211, 255)), 1);
    private static readonly IBrush VertexBrush = new SolidColorBrush(Color.FromRgb(219, 88, 96));
    private static readonly IPen AxisXPen = new Pen(new SolidColorBrush(Color.FromRgb(219, 88, 96)), 1.5);
    private static readonly IPen AxisYPen = new Pen(new SolidColorBrush(Color.FromRgb(136, 196, 112)), 1.5);
    private static readonly IPen AxisZPen = new Pen(new SolidColorBrush(Color.FromRgb(112, 156, 220)), 1.5);

    private bool _isDragging;
    private Point _lastPointerPosition;

    static XModelViewport()
    {
        AffectsRender<XModelViewport>(MeshProperty, YawProperty, PitchProperty, ZoomProperty);
    }

    public XModelViewport()
    {
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.SizeAll);
    }

    public XModelRenderMesh? Mesh
    {
        get => GetValue(MeshProperty);
        set => SetValue(MeshProperty, value);
    }

    public double Yaw
    {
        get => GetValue(YawProperty);
        set => SetValue(YawProperty, NormalizeYaw(value));
    }

    public double Pitch
    {
        get => GetValue(PitchProperty);
        set => SetValue(PitchProperty, Math.Clamp(value, -89, 89));
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, Math.Clamp(value, 0.2, 4));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);
        context.FillRectangle(BackgroundBrush, rect);
        DrawGrid(context, rect);

        if (Mesh is not { HasGeometry: true } mesh)
        {
            return;
        }

        var viewportCenter = rect.Center;
        var scale = Math.Min(rect.Width, rect.Height) * 0.42 / mesh.Radius * Zoom;
        var projectedCenter = Transform(Vector3.Zero, mesh.Center, viewportCenter, scale);

        DrawAxes(context, mesh, viewportCenter, scale);

        DrawTriangles(context, mesh, viewportCenter, scale);

        foreach (var edge in mesh.Edges)
        {
            if (edge.A >= mesh.Vertices.Count || edge.B >= mesh.Vertices.Count)
            {
                continue;
            }

            context.DrawLine(
                EdgePen,
                Transform(mesh.Vertices[edge.A], mesh.Center, viewportCenter, scale),
                Transform(mesh.Vertices[edge.B], mesh.Center, viewportCenter, scale));
        }

        if (mesh.Edges.Count == 0)
        {
            foreach (var vertex in mesh.Vertices)
            {
                var point = Transform(vertex, mesh.Center, viewportCenter, scale);
                context.FillRectangle(VertexBrush, new Rect(point.X - 1, point.Y - 1, 2, 2));
            }
        }

        context.FillRectangle(VertexBrush, new Rect(projectedCenter.X - 2, projectedCenter.Y - 2, 4, 4));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isDragging = true;
        _lastPointerPosition = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isDragging)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _lastPointerPosition;
        _lastPointerPosition = position;

        Yaw += delta.X * 0.45;
        Pitch -= delta.Y * 0.45;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        StopDragging(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Zoom *= e.Delta.Y > 0 ? 1.1 : 0.9;
        e.Handled = true;
    }

    private void StopDragging(IPointer pointer)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        pointer.Capture(null);
    }

    private void DrawGrid(DrawingContext context, Rect rect)
    {
        var spacing = 48d;
        for (var x = rect.X; x <= rect.Right; x += spacing)
        {
            context.DrawLine(GridPen, new Point(x, rect.Y), new Point(x, rect.Bottom));
        }

        for (var y = rect.Y; y <= rect.Bottom; y += spacing)
        {
            context.DrawLine(GridPen, new Point(rect.X, y), new Point(rect.Right, y));
        }
    }

    private void DrawAxes(DrawingContext context, XModelRenderMesh mesh, Point center, double scale)
    {
        var length = mesh.Radius * 0.8f;
        var origin = Transform(mesh.Center, mesh.Center, center, scale);

        context.DrawLine(AxisXPen, origin, Transform(mesh.Center + new Vector3(length, 0, 0), mesh.Center, center, scale));
        context.DrawLine(AxisYPen, origin, Transform(mesh.Center + new Vector3(0, length, 0), mesh.Center, center, scale));
        context.DrawLine(AxisZPen, origin, Transform(mesh.Center + new Vector3(0, 0, length), mesh.Center, center, scale));
    }

    private void DrawTriangles(DrawingContext context, XModelRenderMesh mesh, Point viewportCenter, double scale)
    {
        if (mesh.Triangles.Count == 0)
        {
            return;
        }

        foreach (var triangle in ProjectTriangles(mesh, viewportCenter, scale).OrderBy(item => item.Depth))
        {
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(triangle.A, true);
                geometryContext.LineTo(triangle.B);
                geometryContext.LineTo(triangle.C);
                geometryContext.EndFigure(true);
            }

            context.DrawGeometry(
                new SolidColorBrush(ShadeColor(triangle.Material.Color, triangle.Light)),
                null,
                geometry);
        }
    }

    private IEnumerable<ProjectedTriangle> ProjectTriangles(XModelRenderMesh mesh, Point viewportCenter, double scale)
    {
        foreach (var triangle in mesh.Triangles)
        {
            if (triangle.A >= mesh.Vertices.Count ||
                triangle.B >= mesh.Vertices.Count ||
                triangle.C >= mesh.Vertices.Count)
            {
                continue;
            }

            var material = triangle.MaterialIndex >= 0 && triangle.MaterialIndex < mesh.Materials.Count
                ? mesh.Materials[triangle.MaterialIndex]
                : new XModelRenderMaterial(
                    "Fallback",
                    Color.FromRgb(96, 132, 160),
                    "fallback",
                    IsDecodedTexture: false);

            var a = Project(mesh.Vertices[triangle.A], mesh.Center, viewportCenter, scale);
            var b = Project(mesh.Vertices[triangle.B], mesh.Center, viewportCenter, scale);
            var c = Project(mesh.Vertices[triangle.C], mesh.Center, viewportCenter, scale);
            var normal = Vector3.Cross(b.Rotated - a.Rotated, c.Rotated - a.Rotated);
            var light = normal.LengthSquared() <= float.Epsilon
                ? 0.65
                : 0.58 + (Math.Abs(Vector3.Normalize(normal).Z) * 0.32);

            yield return new ProjectedTriangle(
                a.Point,
                b.Point,
                c.Point,
                (a.Depth + b.Depth + c.Depth) / 3d,
                material,
                light);
        }
    }

    private ProjectedVertex Project(Vector3 vertex, Vector3 modelCenter, Point viewportCenter, double scale)
    {
        var rotated = Rotate(vertex - modelCenter);
        return new ProjectedVertex(
            new Point(
                viewportCenter.X + (rotated.X * scale),
                viewportCenter.Y - (rotated.Y * scale)),
            rotated,
            rotated.Z);
    }

    private Point Transform(Vector3 vertex, Vector3 modelCenter, Point viewportCenter, double scale)
    {
        return Project(vertex, modelCenter, viewportCenter, scale).Point;
    }

    private static Color ShadeColor(Color color, double light)
    {
        light = Math.Clamp(light, 0.35, 1.05);
        return Color.FromRgb(
            ScaleChannel(color.R, light),
            ScaleChannel(color.G, light),
            ScaleChannel(color.B, light));
    }

    private static byte ScaleChannel(byte channel, double light)
    {
        return (byte)Math.Clamp((int)Math.Round(channel * light), 0, 255);
    }

    private Vector3 Rotate(Vector3 value)
    {
        var yaw = Math.PI * Yaw / 180d;
        var pitch = Math.PI * Pitch / 180d;

        var sinYaw = (float)Math.Sin(yaw);
        var cosYaw = (float)Math.Cos(yaw);
        var sinPitch = (float)Math.Sin(pitch);
        var cosPitch = (float)Math.Cos(pitch);

        var yawed = new Vector3(
            (value.X * cosYaw) + (value.Z * sinYaw),
            value.Y,
            (-value.X * sinYaw) + (value.Z * cosYaw));

        return new Vector3(
            yawed.X,
            (yawed.Y * cosPitch) - (yawed.Z * sinPitch),
            (yawed.Y * sinPitch) + (yawed.Z * cosPitch));
    }

    private static double NormalizeYaw(double value)
    {
        value %= 360;
        return value < 0 ? value + 360 : value;
    }

    private readonly record struct ProjectedVertex(Point Point, Vector3 Rotated, double Depth);

    private readonly record struct ProjectedTriangle(
        Point A,
        Point B,
        Point C,
        double Depth,
        XModelRenderMaterial Material,
        double Light);
}
