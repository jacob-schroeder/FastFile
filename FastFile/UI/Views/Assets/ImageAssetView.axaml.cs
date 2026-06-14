using Avalonia.Controls;
using FastFile.Models.Assets.Material;
using System.Globalization;
using System.Linq;
using UI.Models;

namespace UI.Views.Assets;

public partial class ImageAssetView : UserControl
{
    public ImageAssetView()
    {
        InitializeComponent();
    }

    public ImageAssetView(GfxImage image) : this()
    {
        var decoded = GfxImageDecoder.Decode(image);

        ImageNameTextBlock.Text = string.IsNullOrWhiteSpace(image.Name)
            ? "(unnamed image)"
            : image.Name;
        ImageSubtitleTextBlock.Text = $"{image.Width:N0} x {image.Height:N0} x {image.Depth:N0}";
        ImageFormatTextBlock.Text = decoded.Format;
        ImagePreview.Source = decoded.Bitmap;
        ImagePreview.IsVisible = decoded.HasBitmap;
        ImageUnavailableTextBlock.Text = decoded.HasBitmap
            ? string.Empty
            : decoded.Status;
        ImageUnavailableTextBlock.IsVisible = !decoded.HasBitmap;
        ImageDetailsItemsControl.ItemsSource = BuildDetails(image, decoded);
    }

    private static KeyValueListItem[] BuildDetails(GfxImage image, ImageDecodeResult decoded)
    {
        var loadDef = image.LoadDef?.Value;

        return
        [
            new("Name", string.IsNullOrWhiteSpace(image.Name) ? "(unnamed image)" : image.Name),
            new("Dimensions", $"{image.Width:N0} x {image.Height:N0} x {image.Depth:N0}"),
            new("Map Type", $"{image.MapType} (0x{(byte)image.MapType:X2})"),
            new("Semantic", $"{image.Semantic} (0x{(byte)image.Semantic:X2})"),
            new("Category", $"{image.Category} (0x{(byte)image.Category:X2})"),
            new("sRGB Reads", image.UseSrgbReads == 0 ? "No" : "Yes"),
            new("Picmip", string.Join(", ", image.Picmip)),
            new("No Picmip", image.NoPicmip == 0 ? "No" : "Yes"),
            new("Track", AssetViewFormatters.FormatByte(image.Track)),
            new("Platform Words (0x20)", string.Join(", ", image.PlatformControlWords.Select(value => value.ToString("N0", CultureInfo.CurrentCulture)))),
            new("Delay Load Pixels", image.DelayLoadPixels == 0 ? "No" : "Yes"),
            new("LoadDef Pointer", AssetViewFormatters.FormatPointerRaw(image.LoadDef)),
            new("LoadDef Levels", loadDef?.LevelCount.ToString("N0", CultureInfo.CurrentCulture) ?? AssetViewFormatters.UnresolvedPointerText),
            new("LoadDef Flags", loadDef is null ? AssetViewFormatters.UnresolvedPointerText : $"0x{loadDef.Flags:X8}"),
            new("LoadDef Format", loadDef is null ? decoded.Format : ImageFormatFormatter.Format(loadDef.Format)),
            new("Inline Data", loadDef is null ? AssetViewFormatters.UnresolvedPointerText : $"{loadDef.Data.Length:N0} bytes"),
            new("Decode Status", decoded.Status)
        ];
    }
}
