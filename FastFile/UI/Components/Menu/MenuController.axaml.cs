using Avalonia.Controls;
using Avalonia.Media;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using System;
using System.Globalization;
using System.Linq;
using UI.Models;
using MenuWindow = FastFile.Models.Assets.Menu.Elements.Window;

namespace UI.Components.Menu;

public partial class MenuController : UserControl
{
    private const double PreviewWidth = 720;
    private const double PreviewHeight = 405;
    private const double PreviewPadding = 20;
    private const double VirtualScreenWidth = 640;
    private const double VirtualScreenHeight = 480;
    private const byte AlignSub = 0;
    private const byte AlignLeftOrTop = 1;
    private const byte AlignCenter = 2;
    private const byte AlignRightOrBottom = 3;
    private const byte AlignFullscreen = 4;
    private const byte AlignCenterSafeArea = 7;

    public MenuController()
    {
        InitializeComponent();
    }

    public MenuController(MenuDef menu) : this()
    {
        SetMenu(menu);
    }

    public void SetMenu(MenuDef menu)
    {
        var items = GetMenuItems(menu);
        var menuName = MenuDisplayFormatter.FormatStringPointer(
            menu.Window?.NamePtr,
            menu.Window?.Name,
            "(unnamed menu)");

        MenuNameTextBlock.Text = menuName;
        MenuSubtitleTextBlock.Text = MenuDisplayFormatter.FormatRectangle(menu.Window?.Rect);
        MenuItemCountTextBlock.Text = $"{items.Length:N0} items";

        MenuDetailsItemsControl.ItemsSource = BuildMenuDetails(menu);
        WindowDetailsItemsControl.ItemsSource = BuildWindowDetails(menu.Window);
        MenuItemsItemsControl.ItemsSource = items;
        MenuItemsEmptyTextBlock.Text = menu.Items is { Kind: PointerKind.Offset }
            ? MenuDisplayFormatter.ExternalPointerText
            : "No items available.";
        MenuItemsEmptyTextBlock.IsVisible = items.Length == 0;

        RenderPreview(menu, items);
    }

    private static MenuItemDefDisplayItem[] GetMenuItems(MenuDef menu)
    {
        return menu.Items is { IsResolved: true, Result: not null }
            ? menu.Items.Result
                .Select((item, index) => new MenuItemDefDisplayItem(index, item))
                .ToArray()
            : [];
    }

    private static KeyValueListItem[] BuildMenuDetails(MenuDef menu)
    {
        return
        [
            new("Name", MenuDisplayFormatter.FormatStringPointer(menu.Window?.NamePtr, menu.Window?.Name, "(unnamed menu)")),
            new("Font", MenuDisplayFormatter.FormatStringPointer(menu.FontPtr, menu.Font, string.Empty)),
            new("Fullscreen", MenuDisplayFormatter.FormatYesNo(menu.Fullscreen)),
            new("Item Count", menu.ItemCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("Font Index", menu.FontIndex.ToString(CultureInfo.CurrentCulture)),
            new("Image Track", menu.ImageTrack.ToString(CultureInfo.CurrentCulture)),
            new("Sound", MenuDisplayFormatter.FormatStringPointer(menu.SoundName, menu.SoundName?.Result, string.Empty)),
            new("Allowed Binding", MenuDisplayFormatter.FormatStringPointer(menu.AllowedBinding, menu.AllowedBinding?.Result, string.Empty)),
            new("Fade Cycle", menu.FadeCycle.ToString(CultureInfo.CurrentCulture)),
            new("Fade Clamp", menu.FadeClamp.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Fade Amount", menu.FadeAmount.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Fade In", menu.FadeInAmount.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Blur Radius", menu.BlurRadius.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Focus Color", MenuDisplayFormatter.FormatVec4(menu.FocusColor)),
            new("Items Pointer", MenuDisplayFormatter.FormatPointer(menu.Items))
        ];
    }

    private static KeyValueListItem[] BuildWindowDetails(MenuWindow? window)
    {
        if (window is null)
        {
            return [new("Window", MenuDisplayFormatter.NullPointerText)];
        }

        return
        [
            new("Group", MenuDisplayFormatter.FormatStringPointer(window.GroupPtr, window.Group, string.Empty)),
            new("Rect", MenuDisplayFormatter.FormatRectangle(window.Rect)),
            new("Client Rect", MenuDisplayFormatter.FormatRectangle(window.RectClient)),
            new("Style", window.Style.ToString(CultureInfo.CurrentCulture)),
            new("Border", window.Border.ToString(CultureInfo.CurrentCulture)),
            new("Owner Draw", window.OwnerDraw.ToString(CultureInfo.CurrentCulture)),
            new("Owner Flags", $"0x{window.OwnerDrawFlags:X8}"),
            new("Border Size", window.BorderSize.ToString("0.###", CultureInfo.CurrentCulture)),
            new("Static Flags", $"0x{window.StaticFlags:X8}"),
            new("Dynamic Flags", string.Join(", ", window.DynamicFlags)),
            new("Foreground", MenuDisplayFormatter.FormatVec4(window.ForeColor)),
            new("Background", MenuDisplayFormatter.FormatVec4(window.BackColor)),
            new("Border Color", MenuDisplayFormatter.FormatVec4(window.BorderColor)),
            new("Background Material", MenuDisplayFormatter.FormatAssetPointer(window.Background))
        ];
    }

    private void RenderPreview(MenuDef menu, MenuItemDefDisplayItem[] items)
    {
        MenuPreviewCanvas.Children.Clear();

        var previewBackground = new Border
        {
            Width = PreviewWidth,
            Height = PreviewHeight,
            Background = SolidColorBrush.Parse("#18191C"),
            BorderBrush = SolidColorBrush.Parse("#3C3F44"),
            BorderThickness = new Avalonia.Thickness(1)
        };
        MenuPreviewCanvas.Children.Add(previewBackground);

        if (menu.Window?.Rect is null)
        {
            AddPreviewLabel("No window rectangle available.", 24, 24);
            return;
        }

        var menuBounds = ResolveRootRect(menu.Window.Rect);
        var transform = CreatePreviewTransform(menuBounds);
        AddPreviewRect(
            menuBounds,
            menuBounds,
            transform,
            MenuDisplayFormatter.ToBrush(menu.Window.BackColor, "#252629"),
            MenuDisplayFormatter.ToBrush(menu.Window.BorderColor, "#DB5860"),
            MenuDisplayFormatter.FormatStringPointer(menu.Window.NamePtr, menu.Window.Name, "menu"),
            1.0,
            2);

        foreach (var item in items.Where(item => item.Item?.Window?.Rect is not null))
        {
            var itemBounds = ResolveChildRect(item.Item!.Window.Rect, menuBounds);
            AddPreviewRect(
                itemBounds,
                menuBounds,
                transform,
                MenuDisplayFormatter.ToBrush(item.Item.Window.BackColor, "#34373D"),
                MenuDisplayFormatter.ToBrush(item.Item.Window.BorderColor, "#4E5157"),
                item.Name,
                0.62,
                1);
        }
    }

    private static PreviewTransform CreatePreviewTransform(PreviewRect rect)
    {
        var sourceWidth = Math.Max(1, rect.Width);
        var sourceHeight = Math.Max(1, rect.Height);
        var scale = Math.Min(
            (PreviewWidth - PreviewPadding * 2) / sourceWidth,
            (PreviewHeight - PreviewPadding * 2) / sourceHeight);

        return new PreviewTransform(
            rect.Left,
            rect.Top,
            scale,
            (PreviewWidth - sourceWidth * scale) / 2,
            (PreviewHeight - sourceHeight * scale) / 2);
    }

    private void AddPreviewRect(
        PreviewRect rect,
        PreviewRect origin,
        PreviewTransform transform,
        IBrush background,
        IBrush borderBrush,
        string label,
        double opacity,
        double borderThickness)
    {
        var left = transform.Left + (rect.Left - origin.Left) * transform.Scale;
        var top = transform.Top + (rect.Top - origin.Top) * transform.Scale;
        var width = Math.Max(4, rect.Width * transform.Scale);
        var height = Math.Max(4, rect.Height * transform.Scale);

        var previewRect = new Border
        {
            Width = width,
            Height = height,
            Background = background,
            BorderBrush = borderBrush,
            BorderThickness = new Avalonia.Thickness(borderThickness),
            Opacity = opacity,
            Child = new TextBlock
            {
                Text = label,
                Foreground = SolidColorBrush.Parse("#F4F4F5"),
                FontSize = 10,
                Margin = new Avalonia.Thickness(4, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };

        Canvas.SetLeft(previewRect, left);
        Canvas.SetTop(previewRect, top);
        MenuPreviewCanvas.Children.Add(previewRect);
    }

    private void AddPreviewLabel(string text, double left, double top)
    {
        var label = new TextBlock
        {
            Text = text,
            Foreground = SolidColorBrush.Parse("#9DA1AA"),
            FontSize = 13
        };

        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        MenuPreviewCanvas.Children.Add(label);
    }

    private readonly record struct PreviewTransform(
        double OriginX,
        double OriginY,
        double Scale,
        double Left,
        double Top);

    private static PreviewRect ResolveRootRect(RectangleDef rect)
    {
        return ResolveAlignedRect(
            rect,
            new PreviewRect(0, 0, VirtualScreenWidth, VirtualScreenHeight),
            useSubAlignParent: false);
    }

    private static PreviewRect ResolveChildRect(RectangleDef rect, PreviewRect parent)
    {
        return ResolveAlignedRect(rect, parent, useSubAlignParent: true);
    }

    private static PreviewRect ResolveAlignedRect(RectangleDef rect, PreviewRect parent, bool useSubAlignParent)
    {
        var width = Math.Max(1d, rect.W);
        var height = Math.Max(1d, rect.H);
        var horizontal = ResolveAlignedAxis(
            rect.X,
            width,
            rect.HorzAlign,
            parent.Left,
            parent.Width,
            VirtualScreenWidth,
            useSubAlignParent);
        var vertical = ResolveAlignedAxis(
            rect.Y,
            height,
            rect.VertAlign,
            parent.Top,
            parent.Height,
            VirtualScreenHeight,
            useSubAlignParent);

        return new PreviewRect(horizontal.Start, vertical.Start, horizontal.Size, vertical.Size);
    }

    private static PreviewAxis ResolveAlignedAxis(
        float offset,
        double size,
        byte alignment,
        double parentStart,
        double parentSize,
        double virtualSize,
        bool useSubAlignParent)
    {
        var isSubAlign = useSubAlignParent && alignment == AlignSub;
        var start = isSubAlign ? parentStart : 0;
        var availableSize = isSubAlign ? parentSize : virtualSize;

        return alignment switch
        {
            AlignSub => new PreviewAxis(start + offset, size),
            AlignLeftOrTop => new PreviewAxis(start + offset, size),
            AlignCenter or AlignCenterSafeArea => new PreviewAxis(start + (availableSize - size) / 2 + offset, size),
            AlignRightOrBottom => new PreviewAxis(start + availableSize - size - offset, size),
            AlignFullscreen => new PreviewAxis(start, availableSize),
            _ => new PreviewAxis(start + offset, size)
        };
    }

    private readonly record struct PreviewRect(double Left, double Top, double Width, double Height)
    {
        public double Right => Left + Width;

        public double Bottom => Top + Height;
    }

    private readonly record struct PreviewAxis(double Start, double Size);
}
