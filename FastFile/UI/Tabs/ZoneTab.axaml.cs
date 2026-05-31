using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FastFile.Models;
using System.Collections.Generic;
using System.Linq;
using FastFile.Logic;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using UI.Models;

namespace UI.Tabs;

public partial class ZoneTab : UserControl
{
    private List<DisplayItem> _scriptStringItems = [];
    private XAssetList? _xassetList;

    public ZoneTab()
    {
        InitializeComponent();
        SetItems(new Dictionary<string, string> { ["Size"] = "-" });
        SetScriptStrings([]);
    }

    public void UpdateZone(FastFile.Models.Zone.XFile? xfile, FastFile.Models.Zone.XAssetList? xassetList)
    {
        _xassetList = xassetList;

        if (xfile is null)
        {
            SetItems(new Dictionary<string, string> { ["Size"] = "-" });
            SetScriptStrings([]);
            return;
        }

        var labels = new Dictionary<string, string>
        {
            ["Size"] = $"{xfile.Size:N0} bytes",
            ["External Size"] = $"{xfile.ExternalSize:N0} bytes"
        };

        for(int i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
        {
            var blockSize = xfile.BlockSize[i];
            labels[$"{(XFILE_BLOCK)i} Size"] = $"{blockSize:N0} bytes";
        }
        
        SetItems(labels);
        SetScriptStrings(xassetList?.ScriptStrings);
    }
    
    public void SetItems(Dictionary<string, string> items)
    {
        HeaderItemsControl.ItemsSource = items
            .Select(item => new KeyValueListItem(item.Key, item.Value))
            .ToArray();
    }

    private void SetScriptStrings(string?[]? scriptStrings)
    {
        _scriptStringItems = (scriptStrings ?? [])
            .Select((value, index) => new DisplayItem { Id = index, Display = value ?? string.Empty })
            .ToList();

        RefreshScriptStringItems();
    }

    private void RemoveScriptStringButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index })
        {
            removeScriptString(index);
        }
    }

    private void EditScriptStringButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index })
        {
            editScriptString(index);
        }
    }

    private void SaveScriptStringButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index })
        {
            saveScriptString(index);
        }
    }

    private void AddScriptStringButton_Click(object? sender, RoutedEventArgs e)
    {
        addScriptString();
    }

    private void ScriptStringButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button button)
        {
            ApplyScriptStringButtonColors(button, isHovering: true);
        }
    }

    private void ScriptStringButton_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Button button)
        {
            ApplyScriptStringButtonColors(button, isHovering: false);
        }
    }

    private void removeScriptString(int index)
    {
        var item = _scriptStringItems.FirstOrDefault(scriptString => scriptString.Id == index);
        if (item is null)
        {
            return;
        }

        _scriptStringItems.Remove(item);
        RenumberScriptStrings();
        CommitScriptStrings();
        RefreshScriptStringItems();
    }

    private void addScriptString()
    {
        _scriptStringItems.Insert(0, new DisplayItem { Display = string.Empty });
        RenumberScriptStrings();
        CommitScriptStrings();
        RefreshScriptStringItems();
    }

    private void editScriptString(int index)
    {
        foreach (var item in _scriptStringItems)
        {
            item.IsEditing = item.Id == index;
        }

        RefreshScriptStringItems();
    }

    private void saveScriptString(int index)
    {
        var item = _scriptStringItems.FirstOrDefault(scriptString => scriptString.Id == index);
        if (item is null)
        {
            return;
        }

        item.IsEditing = false;
        CommitScriptStrings();
        RefreshScriptStringItems();
    }

    private void CommitScriptStrings()
    {
        if (_xassetList is null)
        {
            return;
        }

        var pointers = _scriptStringItems
            .Select(item =>
            {
                var pointer = new ZonePointer<string?>(0);
                pointer.SetResult(string.IsNullOrEmpty(item.Display) ? null : item.Display);
                return pointer;
            })
            .ToArray();

        _xassetList.ScriptStringsPtr.SetResult(pointers);
        _xassetList.ScriptStringCount = _xassetList.ScriptStrings.Length;
    }

    private void RenumberScriptStrings()
    {
        for (var i = 0; i < _scriptStringItems.Count; i++)
        {
            _scriptStringItems[i].Id = i;
        }
    }

    private void RefreshScriptStringItems()
    {
        ScriptStringsItemsControl.ItemsSource = null;
        ScriptStringsItemsControl.ItemsSource = _scriptStringItems.ToArray();
    }

    private static void ApplyScriptStringButtonColors(Button button, bool isHovering)
    {
        var color = GetScriptStringButtonColor(button, isHovering);
        var brush = new SolidColorBrush(Color.Parse(color));

        button.Background = brush;
        button.BorderBrush = brush;
        button.Foreground = Brushes.White;
    }

    private static string GetScriptStringButtonColor(Button button, bool isHovering)
    {
        if (button.Classes.Contains("actionButtonSuccess"))
        {
            return isHovering ? "#157347" : "#198754";
        }

        if (button.Classes.Contains("actionButtonDanger"))
        {
            return isHovering ? "#BB2D3B" : "#DC3545";
        }

        return isHovering ? "#0B5ED7" : "#0D6EFD";
    }
}
