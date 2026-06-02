using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Zone;
using System.Collections.Generic;
using System.Linq;
using UI.Models;
using UI.Views.Assets;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace UI.Tabs;

public partial class AssetsTab : UserControl
{
    private List<AssetGroupDisplayItem> _assetGroups = [];
    private readonly List<AssetDetailTabItem> _assetDetailTabs = [];
    private AssetDetailTabItem? _selectedAssetDetailTab;

    public AssetsTab()
    {
        InitializeComponent();
        RefreshAssetDetailTabs();
    }

    public void UpdateAssets(XAssetList? assetList)
    {
        _assetGroups = assetList?.Assets
            .Select((asset, index) => new
            {
                Index = index,
                Type = asset.Type.ToString(),
                Asset = asset
            })
            .GroupBy(asset => asset.Type)
            .OrderBy(group => group.Key)
            .Select((group, index) => new AssetGroupDisplayItem
            {
                Id = index,
                Name = group.Key,
                Count = group.Count(),
                Assets = group
                    .Select(item =>
                    {
                        var isExternal = IsExternalAsset(item.Asset);

                        return new DisplayItem
                        {
                            Id = item.Index,
                            Display = GetAssetDisplayName(item.Asset, item.Index),
                            Asset = item.Asset.XAssetPtr.Result,
                            AssetType = item.Asset.Type,
                            IsExternal = isExternal
                        };
                    })
                    .ToArray()
            })
            .ToList() ?? [];

        RefreshAssetGroups();
        _assetDetailTabs.Clear();
        RefreshAssetDetailTabs();
    }

    private void AssetSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshAssetGroups();
    }

    private void AssetGroupHeader_Click(object? sender, RoutedEventArgs e)
    {
        if (HasAssetSearchQuery())
        {
            return;
        }

        if (sender is not Button { Tag: int id })
        {
            return;
        }

        var group = _assetGroups.FirstOrDefault(assetGroup => assetGroup.Id == id);
        if (group is null)
        {
            return;
        }

        group.IsExpanded = !group.IsExpanded;
        RefreshAssetGroups();
    }

    private void AssetItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DisplayItem asset })
        {
            return;
        }

        OpenAssetTab(asset);
    }

    private void RefreshAssetGroups()
    {
        AssetGroupsItemsControl.ItemsSource = null;
        AssetGroupsItemsControl.ItemsSource = GetVisibleAssetGroups().ToArray();
    }

    private IEnumerable<AssetGroupDisplayItem> GetVisibleAssetGroups()
    {
        var query = GetAssetSearchQuery();
        if (string.IsNullOrWhiteSpace(query))
        {
            return _assetGroups;
        }

        return _assetGroups
            .Select(group =>
            {
                var matchingAssets = group.Assets
                    .Where(asset => !asset.IsExternal
                                    && asset.Display.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                return matchingAssets.Length == 0
                    ? null
                    : new AssetGroupDisplayItem
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Count = matchingAssets.Length,
                        Assets = matchingAssets,
                        IsExpanded = true
                    };
            })
            .Where(group => group is not null)
            .Select(group => group!);
    }

    private bool HasAssetSearchQuery()
    {
        return !string.IsNullOrWhiteSpace(GetAssetSearchQuery());
    }

    private string GetAssetSearchQuery()
    {
        return AssetSearchTextBox.Text?.Trim() ?? string.Empty;
    }

    private static string GetAssetDisplayName(XAsset asset, int index)
    {
        var resolvedAsset = asset.XAssetPtr.Result;
        var displayName = resolvedAsset?.GetDisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        if (IsExternalAsset(asset))
        {
            return "[EXTERNAL]";
        }

        return $"Asset {index:N0}";
    }

    private static bool IsExternalAsset(XAsset asset)
    {
        return asset.XAssetPtr.Kind == PointerKind.Offset
            || asset.XAssetPtr.Result is LocalizeEntry { NamePtr.Kind: PointerKind.Offset };
    }

    private void AssetDetailTabCloseButton_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is not Button { Tag: int assetId })
        {
            return;
        }

        CloseAssetDetailTab(assetId);
    }

    private void AssetDetailTabCloseMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;

        if (sender is not MenuItem { Tag: int assetId })
        {
            return;
        }

        CloseAssetDetailTab(assetId);
    }

    private void AssetDetailTabCloseAllMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;

        _assetDetailTabs.Clear();
        RefreshAssetDetailTabs();
    }

    private void CloseAssetDetailTab(int assetId)
    {
        var tab = _assetDetailTabs.FirstOrDefault(item => item.AssetId == assetId);
        if (tab is null)
        {
            return;
        }

        var tabIndex = _assetDetailTabs.IndexOf(tab);

        _assetDetailTabs.Remove(tab);

        AssetDetailTabItem? nextSelectedTab = null;
        if (_assetDetailTabs.Count > 0)
        {
            var selectedTabIsStillOpen = _selectedAssetDetailTab is not null
                && !ReferenceEquals(_selectedAssetDetailTab, tab)
                && _assetDetailTabs.Contains(_selectedAssetDetailTab);

            nextSelectedTab = selectedTabIsStillOpen
                ? _selectedAssetDetailTab
                : _assetDetailTabs[System.Math.Min(tabIndex, _assetDetailTabs.Count - 1)];
        }

        RefreshAssetDetailTabs(nextSelectedTab);
    }

    private void AssetDetailTabButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AssetDetailTabItem tab })
        {
            RefreshAssetDetailTabs(tab);
        }
    }

    private void OpenAssetTab(DisplayItem asset)
    {
        var existingTab = _assetDetailTabs.FirstOrDefault(tab => tab.AssetId == asset.Id);
        if (existingTab is not null)
        {
            RefreshAssetDetailTabs(existingTab);
            return;
        }

        var tab = new AssetDetailTabItem
        {
            AssetId = asset.Id,
            Title = asset.Display,
            Content = CreateAssetView(asset)
        };

        _assetDetailTabs.Add(tab);
        RefreshAssetDetailTabs(tab);
    }

    private static Control CreateAssetView(DisplayItem asset)
    {
        return asset.AssetType switch
        {
            XAssetType.Localize when asset.Asset is LocalizeEntry localizeEntry => new LocalizeAssetView(localizeEntry),
            XAssetType.MenuFile when asset.Asset is MenuList menuList => new MenuListAssetView(menuList),
            XAssetType.StringTable when asset.Asset is StringTable stringTable => new StringTableAssetView(stringTable),
            XAssetType.Material when asset.Asset is MaterialAsset material => new MaterialAssetView(material),
            XAssetType.Image when asset.Asset is GfxImage image => new ImageAssetView(image),
            XAssetType.Techset => new TechsetAssetView(),
            XAssetType.RawFile when asset.Asset is RawFile rawFile => new RawfileAssetView(rawFile),
            _ => new DefaultAssetView()
        };
    }

    private void RefreshAssetDetailTabs(AssetDetailTabItem? selectedTab = null)
    {
        _selectedAssetDetailTab = selectedTab ?? _assetDetailTabs.LastOrDefault();

        foreach (var tab in _assetDetailTabs)
        {
            tab.IsSelected = ReferenceEquals(tab, _selectedAssetDetailTab);
        }

        AssetDetailTabsItemsControl.ItemsSource = null;
        AssetDetailTabsItemsControl.ItemsSource = _assetDetailTabs.ToArray();

        var hasTabs = _assetDetailTabs.Count > 0;
        AssetDetailTabsHost.IsVisible = hasTabs;
        AssetDetailEmptyContentControl.IsVisible = !hasTabs;

        if (!hasTabs)
        {
            AssetDetailSelectedContentControl.Content = null;
            AssetDetailEmptyContentControl.Content = new DefaultAssetView();
            return;
        }

        AssetDetailSelectedContentControl.Content = _selectedAssetDetailTab?.Content;
    }
}
