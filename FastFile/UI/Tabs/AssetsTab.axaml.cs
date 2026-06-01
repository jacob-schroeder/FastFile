using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Zone;
using System.Collections.Generic;
using System.Linq;
using UI.Models;
using UI.Views.Assets;

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
                    .Select(item => new DisplayItem
                    {
                        Id = item.Index,
                        Display = GetAssetDisplayName(item.Asset, item.Index),
                        Asset = item.Asset.XAssetPtr.Result,
                        AssetType = item.Asset.Type
                    })
                    .ToArray()
            })
            .ToList() ?? [];

        RefreshAssetGroups();
        _assetDetailTabs.Clear();
        RefreshAssetDetailTabs();
    }

    private void AssetGroupHeader_Click(object? sender, RoutedEventArgs e)
    {
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
        AssetGroupsItemsControl.ItemsSource = _assetGroups.ToArray();
    }

    private static string GetAssetDisplayName(XAsset asset, int index)
    {
        var resolvedAsset = asset.XAssetPtr.Result;
        var displayName = resolvedAsset?.GetDisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        if (resolvedAsset is LocalizeEntry { NamePtr.Kind: PointerKind.Offset }
            || asset.XAssetPtr.Kind == PointerKind.Offset)
        {
            return "[EXTERNAL]";
        }

        return $"Asset {index:N0}";
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
