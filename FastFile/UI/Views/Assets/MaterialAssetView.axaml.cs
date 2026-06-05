using Avalonia.Controls;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using System.Globalization;
using System.Linq;
using UI.Models;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace UI.Views.Assets;

public partial class MaterialAssetView : UserControl
{
    public MaterialAssetView()
    {
        InitializeComponent();
    }

    public MaterialAssetView(MaterialAsset material) : this()
    {
        var name = string.IsNullOrWhiteSpace(material.GetDisplayName)
            ? "(unnamed material)"
            : material.GetDisplayName;

        MaterialNameTextBlock.Text = name;
        MaterialSubtitleTextBlock.Text = FormatTechniqueSet(material.TechniqueSet);
        MaterialTextureCountTextBlock.Text = $"{material.TextureCount:N0} textures";
        MaterialDetailsItemsControl.ItemsSource = BuildMaterialDetails(material);
        MaterialTexturesItemsControl.ItemsSource = BuildTextureItems(material);
        MaterialConstantsItemsControl.ItemsSource = BuildConstantItems(material);
    }

    private static KeyValueListItem[] BuildMaterialDetails(MaterialAsset material)
    {
        return
        [
            new("Name", string.IsNullOrWhiteSpace(material.GetDisplayName) ? "(unnamed material)" : material.GetDisplayName),
            new("Game Flags", $"0x{material.Info.GameFlags:X2}"),
            new("Sort Key", material.Info.SortKey.ToString(CultureInfo.CurrentCulture)),
            new("Texture Atlas", $"{material.Info.TextureAtlasRowCount:N0} rows x {material.Info.TextureAtlasColumnCount:N0} columns"),
            new("Draw Surface", $"0x{material.Info.DrawSurf.Packed:X16}"),
            new("Surface Type Bits", $"0x{material.Info.SurfaceTypeBits:X8}"),
            new("Texture Count", material.TextureCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("Constant Count", material.ConstantCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("State Bits Count", material.StateBitsCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("State Flags", $"0x{material.StateFlags:X2}"),
            new("Camera Region", material.CameraRegion.ToString(CultureInfo.CurrentCulture)),
            new("Technique Set", FormatTechniqueSet(material.TechniqueSet)),
            new("Texture Table", AssetViewFormatters.FormatPointerRaw(material.TextureTable)),
            new("Constant Table", AssetViewFormatters.FormatPointerRaw(material.ConstantTable)),
            new("State Bit Table", AssetViewFormatters.FormatPointerRaw(material.StateBitTable))
        ];
    }

    private static MaterialTextureDisplayItem[] BuildTextureItems(MaterialAsset material)
    {
        if (material.TextureTable is not { IsResolved: true, Result: not null })
        {
            return [];
        }

        return material.TextureTable.Result
            .Select((texture, index) => BuildTextureItem(texture, index))
            .ToArray();
    }

    private static MaterialTextureDisplayItem BuildTextureItem(MaterialTextureDef texture, int index)
    {
        var image = GetTextureImage(texture);
        var decoded = image is null ? null : GfxImageDecoder.Decode(image);
        var imageName = image is null
            ? GetTexturePointerStatus(texture)
            : string.IsNullOrWhiteSpace(image.Name) ? "(unnamed image)" : image.Name;

        return new MaterialTextureDisplayItem
        {
            Index = $"#{index + 1:N0}",
            Semantic = texture.Semantic.ToString(),
            ImageName = imageName,
            ImageSize = image is null ? string.Empty : $"{image.Width:N0} x {image.Height:N0}",
            Format = decoded?.Format ?? string.Empty,
            Status = decoded?.Status ?? GetTexturePointerStatus(texture),
            Pointer = $"0x{texture.Info.Raw:X8}",
            NameHash = $"0x{texture.NameHash:X8}",
            SampleState = $"0x{texture.SampleState:X2}",
            Preview = decoded?.Bitmap
        };
    }

    private static MaterialConstantDisplayItem[] BuildConstantItems(MaterialAsset material)
    {
        if (material.ConstantTable is not { IsResolved: true, Result: not null })
        {
            return [];
        }

        return material.ConstantTable.Result
            .Select(constant => new MaterialConstantDisplayItem
            {
                Name = string.IsNullOrWhiteSpace(constant.Name) ? "(unnamed constant)" : constant.Name,
                NameHash = $"0x{constant.NameHash:X8}",
                Value = FormatVec4(constant.Literal)
            })
            .ToArray();
    }

    private static GfxImage? GetTextureImage(MaterialTextureDef texture)
    {
        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            return texture.Info.Water?.Result?.Image?.Result;
        }

        return texture.Info.Image?.Result;
    }

    private static string GetTexturePointerStatus(MaterialTextureDef texture)
    {
        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            return AssetViewFormatters.FormatPointerRaw(texture.Info.Water);
        }

        return AssetViewFormatters.FormatPointerRaw(texture.Info.Image);
    }

    private static string FormatTechniqueSet(ZonePointer<MaterialTechniqueSet>? pointer)
    {
        if (pointer is null)
        {
            return AssetViewFormatters.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return AssetViewFormatters.OffsetPointerText;
        }

        if (pointer.Result is { } techniqueSet && !string.IsNullOrWhiteSpace(techniqueSet.GetDisplayName))
        {
            return techniqueSet.GetDisplayName;
        }

        return AssetViewFormatters.FormatPointer(pointer);
    }

    private static string FormatVec4(Vec4 value)
    {
        return string.Create(
            CultureInfo.CurrentCulture,
            $"{value.R:0.###}, {value.G:0.###}, {value.B:0.###}, {value.A:0.###}");
    }
}
