using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using UI.Models;
using System.Globalization;
using FastFile.Models.Zone;

namespace UI.Views.Assets;

public partial class WeaponAssetView : UserControl
{
    private WeaponVariantDef? _weapon;
    private XModel? _previewModel;

    public WeaponAssetView()
    {
        InitializeComponent();
    }

    public WeaponAssetView(WeaponVariantDef weapon) : this()
    {
        _weapon = weapon;
        _previewModel = XModelPreviewHelper.ResolveWeaponPreviewModel(weapon);

        WeaponNameTextBlock.Text = GetDisplayName(weapon);
        WeaponSummaryTextBlock.Text = "Weapon variant asset";
        WeaponOffsetTextBlock.Text = $"0x{weapon.Offset:X8}";
        ViewXModelButton.IsEnabled = _previewModel is not null;
        WeaponSummaryItemsControl.ItemsSource = BuildSummaryItems(weapon);
    }

    private void ViewXModelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_weapon is null || _previewModel is null)
        {
            return;
        }

        if (VisualRoot is Window owner)
        {
            XModelPreviewHelper.Show(_previewModel, owner, GetDisplayName(_weapon));
            return;
        }

        XModelPreviewHelper.Show(_previewModel, contextName: GetDisplayName(_weapon));
    }

    private static string GetDisplayName(WeaponVariantDef weapon)
    {
        return string.IsNullOrWhiteSpace(weapon.GetDisplayName)
            ? "(unnamed weapon variant)"
            : weapon.GetDisplayName;
    }

    private static KeyValueListItem[] BuildSummaryItems(WeaponVariantDef weapon)
    {
        var previewModel = XModelPreviewHelper.ResolveWeaponPreviewModel(weapon);

        return
        [
            new("Display Name", GetResolvedString(weapon.InternalNamePtr)),
            new("Internal Name Pointer", AssetViewFormatters.FormatPointerRaw(weapon.InternalNamePtr)),
            new("Display Name Pointer", AssetViewFormatters.FormatPointerRaw(weapon.DisplayNamePtr)),
            new("Display Name Value", GetResolvedString(weapon.DisplayNamePtr)),
            new("Alt Weapon Name Pointer", AssetViewFormatters.FormatPointerRaw(weapon.szAltWeaponName)),
            new("Alt Weapon Name", GetResolvedString(weapon.szAltWeaponName)),
            new("WeaponDef Pointer", AssetViewFormatters.FormatPointerRaw(weapon.WeaponDefPtr)),
            new("WeaponDef", weapon.WeaponDef is not null && !string.IsNullOrWhiteSpace(weapon.WeaponDef.InternalName)
                ? $"{weapon.WeaponDef.InternalName} (0x{weapon.WeaponDef.Offset:X8})"
                : "[not resolved]"),
            new("Preview XModel", previewModel is null
                ? "[not resolved]"
                : $"{previewModel.GetDisplayName} (0x{previewModel.Offset:X8})"),
            new("Hide Tags", FormatArrayPointer(weapon.HideTags, WeaponVariantDef.HideTagCount)),
            new("Animation Set Pointer", FormatArrayPointer(weapon.XAnims, WeaponVariantDef.WeaponAnimCount)),
            new("Clip Size", FormatInt(weapon.iClipSize)),
            new("Fire Time", FormatInt(weapon.iFireTime)),
            new("Impact Type", weapon.impactType.ToString()),
            new("ADS Zoom FOV", FormatFloat(weapon.fAdsZoomFov)),
            new("ADS In Time", FormatInt(weapon.iAdsTransInTime)),
            new("ADS Out Time", FormatInt(weapon.iAdsTransOutTime)),
            new("ADS View Kick Center Speed", FormatFloat(weapon.fAdsViewKickCenterSpeed)),
            new("ADS Hip View Kick Center Speed", FormatFloat(weapon.fHipViewKickCenterSpeed)),
            new("Motion Tracker", FormatFlagByte(weapon.motionTracker)),
            new("Enhanced", FormatFlagByte(weapon.enhanced)),
            new("Dpad Icon Shows Ammo", FormatFlagByte(weapon.dpadIconShowsAmmo)),
            new("Alt Weapon Index", FormatUInt(weapon.altWeaponIndex)),
            new("Alt Raise Time", FormatInt(weapon.iAltRaiseTime)),
            new("First Raise Time", FormatInt(weapon.iFirstRaiseTime)),
            new("Drop Ammo Max", FormatInt(weapon.iDropAmmoMax)),
            new("ADS DOF Start", FormatFloat(weapon.adsDofStart)),
            new("ADS DOF End", FormatFloat(weapon.adsDofEnd)),
            new("Accuracy Knot Count", weapon.accuracyGraphKnotCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("Original Accuracy Knot Count", weapon.originalAccuracyGraphKnotCount.ToString("N0", CultureInfo.CurrentCulture)),
            new("WeaponDef Alt Ratio", weapon.dpadIconRatio.ToString())
        ];
    }

    private static string GetResolvedString(XPointer<string>? pointer)
    {
        if (pointer is { IsResolved: true, Value: { } value })
        {
            return string.IsNullOrWhiteSpace(value) ? "(empty)" : value;
        }

        return "[unresolved]";
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
            return $"{pointerText} · unresolved";
        }

        var count = pointer.Value?.Length ?? 0;
        return $"{pointerText} · {count:N0} / {expectedCount:N0} values";
    }

    private static string FormatInt(int value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static string FormatUInt(uint value)
    {
        return value.ToString("N0", CultureInfo.CurrentCulture);
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("N3", CultureInfo.CurrentCulture);
    }

    private static string FormatFlagByte(byte value)
    {
        return value == 0
            ? "No (0x00)"
            : $"Yes (0x{value:X2})";
    }
}
