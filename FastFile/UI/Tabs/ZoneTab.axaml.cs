using System;
using Avalonia.Controls;
using FastFile.Models;
using System.Collections.Generic;
using System.Linq;
using FastFile.Logic;
using UI.Models;

namespace UI.Tabs;

public partial class ZoneTab : UserControl
{
    public ZoneTab()
    {
        InitializeComponent();
        SetItems(new Dictionary<string, string>(){ ["Size"] = "-"});
    }

    public void UpdateZone(FastFile.Models.Zone.XFile? xfile)
    {
        if (xfile is null) return;

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
    }
    
    public void SetItems(Dictionary<string, string> items)
    {
        HeaderItemsControl.ItemsSource = items
            .Select(item => new KeyValueListItem(item.Key, item.Value))
            .ToArray();
    }
}
