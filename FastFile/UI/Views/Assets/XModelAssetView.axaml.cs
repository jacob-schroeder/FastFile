using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using System.Globalization;
using System.Linq;
using UI.Models;
using UI.Navigation;

namespace UI.Views.Assets;

public partial class XModelAssetView : UserControl
{
    private XModel? _model;

    public XModelAssetView()
    {
        InitializeComponent();
    }

    public XModelAssetView(XModel model) : this()
    {
        _model = model;

        XModelNameTextBlock.Text = XModelPreviewHelper.GetDisplayName(model);
        XModelSummaryTextBlock.Text = $"{model.NumBones:N0} bones | {model.NumSurfs:N0} surfaces | {model.NumLods:N0} LODs";
        XModelOffsetTextBlock.Text = $"0x{model.Offset:X8}";
        ViewXModelButton.IsEnabled = true;
        XModelDetailsItemsControl.ItemsSource = BuildDetails(model);
        XModelLodItemsControl.ItemsSource = BuildLodItems(model);
    }

    private void ViewXModelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_model is null)
        {
            return;
        }

        var owner = VisualRoot as Window;
        XModelPreviewHelper.Show(_model, owner);
    }

    private void BlockStreamNavigationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BlockStreamNavigationTarget target } button)
        {
            BlockStreamNavigator.Navigate(button, target);
        }
    }

    private static KeyValueListItem[] BuildDetails(XModel model)
    {
        return
        [
            AssetViewFormatters.PointerItem("Name Pointer", model.NamePtr),
            new("Resolved Name", XModelPreviewHelper.GetDisplayName(model)),
            new("Root Offset", $"0x{model.Offset:X8}"),
            new("Root Size", "0x120"),
            new("Bones", $"{FormatByte(model.NumBones)} total | {FormatByte(model.NumRootBones)} root | {model.ParentCount:N0} child"),
            new("Surfaces", $"{FormatByte(model.NumSurfs)} model surfaces | {CountResolvedSurfaces(model):N0} resolved render surfaces"),
            new("LOD Counts", $"{FormatByte(model.NumLods)} active | max loaded {FormatByte(model.MaxLoadedLod)} | collision {FormatByte(model.CollLod)}"),
            new("LOD Ramp Type", FormatByte(model.LodRampType)),
            new("Flags", $"0x{model.Flags:X2}"),
            new("Scale", FormatFloat(model.Scale)),
            new("No Scale Part Bits", FormatIntArray(model.NoScalePartBits)),
            new("Bone Names", FormatArrayPointer(model.BoneNames, model.BoneNameCount), AssetViewFormatters.GetNavigationTarget(model.BoneNames)),
            new("Parent List", FormatArrayPointer(model.ParentList, model.ParentCount), AssetViewFormatters.GetNavigationTarget(model.ParentList)),
            new("Quats", FormatArrayPointer(model.Quats, model.QuatComponentCount), AssetViewFormatters.GetNavigationTarget(model.Quats)),
            new("Translations", FormatArrayPointer(model.Trans, model.PartCount), AssetViewFormatters.GetNavigationTarget(model.Trans)),
            new("Part Classification", FormatArrayPointer(model.PartClassification, model.BoneNameCount), AssetViewFormatters.GetNavigationTarget(model.PartClassification)),
            new("Base Matrices", FormatArrayPointer(model.BaseMat, model.BoneNameCount), AssetViewFormatters.GetNavigationTarget(model.BaseMat)),
            new("Material Handles", FormatArrayPointer(model.MaterialHandles, model.MaterialHandleCount), AssetViewFormatters.GetNavigationTarget(model.MaterialHandles)),
            new("Collision Surfaces", FormatArrayPointer(model.CollSurfs, model.NumCollSurfs), AssetViewFormatters.GetNavigationTarget(model.CollSurfs)),
            new("Contents", $"0x{model.Contents:X8}"),
            new("Bone Info", FormatArrayPointer(model.BoneInfo, model.BoneNameCount), AssetViewFormatters.GetNavigationTarget(model.BoneInfo)),
            new("Radius", FormatFloat(model.Radius)),
            new("Bounds", FormatBounds(model.Bounds)),
            new("Inv High Mip Radius", FormatArrayPointer(model.InvHighMipRadius, model.MaterialHandleCount), AssetViewFormatters.GetNavigationTarget(model.InvHighMipRadius)),
            new("Mem Usage", model.MemUsage.ToString("N0", CultureInfo.CurrentCulture)),
            new("Phys Preset", FormatObjectPointer(model.PhysPreset), AssetViewFormatters.GetNavigationTarget(model.PhysPreset)),
            new("Phys Collmap", FormatObjectPointer(model.PhysCollmap), AssetViewFormatters.GetNavigationTarget(model.PhysCollmap))
        ];
    }

    private static KeyValueListItem[] BuildLodItems(XModel model)
    {
        var lods = model.LodInfo ?? [];
        var items = new KeyValueListItem[lods.Length];

        for (var i = 0; i < lods.Length; i++)
        {
            var lod = lods[i];
            items[i] = new KeyValueListItem(
                $"LOD {i}",
                $"dist {FormatFloat(lod.Dist)} | {lod.NumSurfs:N0} surfs @ {lod.SurfIndex:N0} | modelSurfs {AssetViewFormatters.FormatPointerRaw(lod.ModelSurfs)} | directSurfs {AssetViewFormatters.FormatPointerRaw(lod.Surfs)}",
                AssetViewFormatters.GetNavigationTarget(lod.ModelSurfs) ?? AssetViewFormatters.GetNavigationTarget(lod.Surfs));
        }

        return items;
    }

    private static int CountResolvedSurfaces(XModel model)
    {
        foreach (var lod in model.LodInfo ?? [])
        {
            if (lod.NumSurfs == 0)
            {
                continue;
            }

            if (lod.Surfs is { IsResolved: true, Value: { } directSurfs })
            {
                return directSurfs.Length;
            }

            if (lod.ModelSurfs is { IsResolved: true, Value: { Surfs: { IsResolved: true, Value: { } assetSurfs } } })
            {
                return assetSurfs.Length;
            }
        }

        return 0;
    }

    private static string FormatArrayPointer<T>(XPointer<T[]>? pointer, int expectedCount)
    {
        if (pointer is null)
        {
            return AssetViewFormatters.NullPointerText;
        }

        var pointerText = AssetViewFormatters.FormatPointerRaw(pointer);
        if (!pointer.IsResolved)
        {
            return $"{pointerText} | unresolved";
        }

        var count = pointer.Value?.Length ?? 0;
        return $"{pointerText} | {count:N0} / {expectedCount:N0} values";
    }

    private static string FormatObjectPointer<T>(XPointer<T>? pointer)
    {
        if (pointer is null)
        {
            return AssetViewFormatters.NullPointerText;
        }

        var pointerText = AssetViewFormatters.FormatPointerRaw(pointer);
        return pointer.IsResolved
            ? $"{pointerText} | resolved"
            : $"{pointerText} | unresolved";
    }

    private static string FormatBounds(Bounds bounds)
    {
        return $"mid ({FormatFloat(bounds.MidPoint.X)}, {FormatFloat(bounds.MidPoint.Y)}, {FormatFloat(bounds.MidPoint.Z)}) | half ({FormatFloat(bounds.HalfSize.X)}, {FormatFloat(bounds.HalfSize.Y)}, {FormatFloat(bounds.HalfSize.Z)})";
    }

    private static string FormatIntArray(int[] values)
    {
        return values.Length == 0
            ? "[]"
            : string.Join(", ", values.Select(value => $"0x{value:X8}"));
    }

    private static string FormatByte(byte value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("N3", CultureInfo.CurrentCulture);
    }
}
