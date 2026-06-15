using Avalonia.Controls;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using System.Globalization;
using System.Linq;
using FastFile.Models.Zone;
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
        var textureItems = BuildTextureItems(material);
        UpdateMaterialPreview(textureItems, material.TextureCount);
        MaterialDetailsItemsControl.ItemsSource = BuildMaterialDetails(material);
        MaterialTexturesItemsControl.ItemsSource = textureItems;
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
        return MaterialPreviewHelper.ResolveTextureTable(material)
            .Select((texture, index) => BuildTextureItem(texture, index))
            .ToArray();
    }

    private static MaterialTextureDisplayItem BuildTextureItem(MaterialTextureDef texture, int index)
    {
        var image = MaterialPreviewHelper.GetTextureImage(texture);
        var decoded = image is null ? null : GfxImageDecoder.Decode(image);
        var imageName = image is null
            ? MaterialPreviewHelper.GetTexturePointerStatus(texture)
            : string.IsNullOrWhiteSpace(image.Name) ? "(unnamed image)" : image.Name;

        return new MaterialTextureDisplayItem
        {
            Index = $"#{index + 1:N0}",
            Semantic = texture.Semantic.ToString(),
            ImageName = imageName,
            ImageSize = image is null ? string.Empty : $"{image.Width:N0} x {image.Height:N0}",
            Format = decoded?.Format ?? string.Empty,
            Status = decoded?.Status ?? MaterialPreviewHelper.GetTexturePointerStatus(texture),
            Pointer = $"0x{texture.Info.Raw:X8}",
            NameHash = $"0x{texture.NameHash:X8}",
            SampleState = $"0x{texture.SampleState:X2}",
            Preview = decoded?.Bitmap
        };
    }

    private void UpdateMaterialPreview(MaterialTextureDisplayItem[] textures, int declaredTextureCount)
    {
        var preview = SelectPreviewTexture(textures);

        MaterialPreviewImage.Source = preview?.Preview;
        MaterialPreviewImage.IsVisible = preview?.HasPreview == true;
        MaterialPreviewUnavailableTextBlock.IsVisible = preview?.HasPreview != true;

        if (preview?.HasPreview == true)
        {
            MaterialPreviewNameTextBlock.Text = preview.ImageName;
            MaterialPreviewMetaTextBlock.Text = FormatPreviewMeta(preview);
            MaterialPreviewUnavailableTextBlock.Text = string.Empty;
            return;
        }

        MaterialPreviewNameTextBlock.Text = "No material preview";
        MaterialPreviewMetaTextBlock.Text = declaredTextureCount == 0
            ? "No textures"
            : $"{declaredTextureCount:N0} textures";
        MaterialPreviewUnavailableTextBlock.Text = preview is null
            ? "This material does not have a resolved texture table."
            : $"No decoded texture preview is available.\n{preview.Status}";
    }

    private static MaterialTextureDisplayItem? SelectPreviewTexture(MaterialTextureDisplayItem[] textures)
    {
        var decodedTextures = textures
            .Where(texture => texture.HasPreview)
            .ToArray();

        if (decodedTextures.Length == 0)
        {
            return textures.FirstOrDefault();
        }

        var preferredSemantics = new[]
        {
            MaterialTextureSemantic.TS_COLOR_MAP.ToString(),
            MaterialTextureSemantic.TS_2D.ToString(),
            MaterialTextureSemantic.TS_FUNCTION.ToString(),
            MaterialTextureSemantic.TS_SPECULAR_MAP.ToString(),
            MaterialTextureSemantic.TS_NORMAL_MAP.ToString(),
            MaterialTextureSemantic.TS_WATER_MAP.ToString()
        };

        foreach (var semantic in preferredSemantics)
        {
            var match = decodedTextures.FirstOrDefault(texture => texture.Semantic == semantic);
            if (match is not null)
            {
                return match;
            }
        }

        return decodedTextures[0];
    }

    private static string FormatPreviewMeta(MaterialTextureDisplayItem texture)
    {
        var values = new[]
        {
            texture.Semantic,
            texture.ImageSize,
            texture.Format
        };

        return string.Join(" - ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static MaterialConstantDisplayItem[] BuildConstantItems(MaterialAsset material)
    {
        if (material.ConstantTable is not { IsResolved: true, Value: not null })
        {
            return [];
        }

        return material.ConstantTable.Value
            .Select(constant => new MaterialConstantDisplayItem
            {
                Name = string.IsNullOrWhiteSpace(constant.Name) ? "(unnamed constant)" : constant.Name,
                NameHash = $"0x{constant.NameHash:X8}",
                Value = FormatVec4(constant.Literal)
            })
            .ToArray();
    }

    private static string FormatTechniqueSet(XPointer<MaterialTechniqueSet>? pointer)
    {
        if (pointer is null)
        {
            return AssetViewFormatters.NullPointerText;
        }

        if (pointer.Kind == PointerKind.Offset)
        {
            return AssetViewFormatters.OffsetPointerText;
        }

        if (pointer.Value is { } techniqueSet && !string.IsNullOrWhiteSpace(techniqueSet.GetDisplayName))
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
