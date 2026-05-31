using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Zone;
using System.Collections.Generic;
using System.Linq;
using UI.Models;
using UI.Views.Assets;

namespace UI.Tabs;

public partial class AssetsTab : UserControl
{
    private List<AssetGroupDisplayItem> _assetGroups = [];

    public AssetsTab()
    {
        InitializeComponent();
        ShowDefaultAssetView();
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
                        Display = item.Asset.XAssetPtr.Result?.GetDisplayName ?? $"Asset {item.Index:N0}",
                        Asset = item.Asset.XAssetPtr.Result,
                        AssetType = item.Asset.Type
                    })
                    .ToArray()
            })
            .ToList() ?? [];

        RefreshAssetGroups();
        ShowDefaultAssetView();
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

        switch (asset.AssetType)
        {
            case XAssetType.Techset:
                AssetDetailContentControl.Content = new TechsetAssetView();
                return;
            case XAssetType.RawFile:
                if (asset.Asset is RawFile rawFile)
                {
                    AssetDetailContentControl.Content = new RawfileAssetView(rawFile);
                    return;
                }

                break;
        }

        ShowDefaultAssetView();
    }

    private void RefreshAssetGroups()
    {
        AssetGroupsItemsControl.ItemsSource = null;
        AssetGroupsItemsControl.ItemsSource = _assetGroups.ToArray();
    }

    private void ShowDefaultAssetView()
    {
        AssetDetailContentControl.Content = new DefaultAssetView();
    }
}
