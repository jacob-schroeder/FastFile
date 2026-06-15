using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FastFile.Models.Assets.XModels;

namespace UI.Views.Assets;

public partial class XModelViewer : UserControl
{
    private bool _isSyncingControls;

    public XModelViewer()
    {
        InitializeComponent();

        YawSlider.ValueChanged += RotationSlider_ValueChanged;
        PitchSlider.ValueChanged += RotationSlider_ValueChanged;
        ZoomSlider.ValueChanged += RotationSlider_ValueChanged;
        Viewport.PropertyChanged += Viewport_PropertyChanged;
    }

    public XModelViewer(XModel model) : this()
    {
        SetModel(model);
    }

    public void SetModel(XModel model)
    {
        var mesh = XModelMeshBuilder.Build(model);
        Viewport.Mesh = mesh;
        ModelNameTextBlock.Text = mesh.ModelName;
        ModelMetaTextBlock.Text = $"{mesh.VertexCount:N0} vertices · {mesh.TriangleCount:N0} triangles · {mesh.SurfaceCount:N0} surfaces · {FormatMaterialSummary(mesh)} · {mesh.Status}";
        EmptyStateTextBlock.Text = mesh.HasGeometry
            ? string.Empty
            : mesh.SurfaceCount == 0
                ? "No resolved XSurface records were available for this XModel in the current fastfile."
                : "Resolved XSurface records were found, but their vertex buffers were not available.";
        EmptyStateBorder.IsVisible = !mesh.HasGeometry;

        SyncControlsFromViewport();
    }

    private void RotationSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isSyncingControls)
        {
            return;
        }

        Viewport.Yaw = YawSlider.Value;
        Viewport.Pitch = PitchSlider.Value;
        Viewport.Zoom = ZoomSlider.Value;
    }

    private void Viewport_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == XModelViewport.YawProperty ||
            e.Property == XModelViewport.PitchProperty ||
            e.Property == XModelViewport.ZoomProperty)
        {
            SyncControlsFromViewport();
        }
    }

    private void ResetButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Viewport.Yaw = 35;
        Viewport.Pitch = -18;
        Viewport.Zoom = 1;
        SyncControlsFromViewport();
    }

    private void SyncControlsFromViewport()
    {
        _isSyncingControls = true;
        try
        {
            YawSlider.Value = Viewport.Yaw;
            PitchSlider.Value = Viewport.Pitch;
            ZoomSlider.Value = Viewport.Zoom;
        }
        finally
        {
            _isSyncingControls = false;
        }
    }

    private static string FormatMaterialSummary(XModelRenderMesh mesh)
    {
        if (mesh.MaterialCount == 0)
        {
            return "0 materials";
        }

        return $"{mesh.MaterialCount:N0} materials ({mesh.DecodedMaterialColorCount:N0} textured, {mesh.FallbackMaterialColorCount:N0} fallback)";
    }
}
