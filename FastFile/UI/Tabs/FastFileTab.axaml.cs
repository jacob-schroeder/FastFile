using System;
using Avalonia.Controls;
using FastFile.Models;
using System.Collections.Generic;
using System.Linq;
using UI.Models;

namespace UI.Tabs;

public partial class FastFileTab : UserControl
{
    public FastFileTab()
    {
        InitializeComponent();
        SetItems(new Dictionary<string, string>
        {
            ["Magic"] = "-",
            ["Version"] = "-",
            ["Allow Online Update"] = "-",
            ["File Created"] = "-",
            ["Region"] = "-",
            ["Entry Count"] = "-",
            ["File Size"] = "-",
            ["Max File Size"] = "-"
        });
    }

    public void SetStatus(string status)
    {
        StatusTextBlock.Text = status;
    }

    public void UpdateHeader(DB_Header? header)
    {
        if (header is null)
        {
            SetItems(new Dictionary<string, string>
            {
                ["Magic"] = "-",
                ["Version"] = "-",
                ["Allow Online Update"] = "-",
                ["File Created"] = "-",
                ["Region"] = "-",
                ["Entry Count"] = "-",
                ["File Size"] = "-",
                ["Max File Size"] = "-"
            });
            return;
        }

        SetItems(new Dictionary<string, string>
        {
            ["Magic"] = header.Magic,
            ["Version"] = header.Version.ToString(),
            ["Allow Online Update"] = header.AllowOnlineUpdate.ToString(),
            ["File Created"] = DateTime.FromFileTimeUtc((long)header.FileCreationTime).ToString("MM/dd/yyyy hh:mm:ss tt"),
            ["Region"] = header.Region.ToString(),
            ["Entry Count"] = $"{header.EntryCount:D}",
            ["File Size"] = $"{header.FileSize:N0} bytes",
            ["Max File Size"] = $"{header.MaxFileSize:N0} bytes"
        });
    }

    public void SetItems(Dictionary<string, string> items)
    {
        HeaderItemsControl.ItemsSource = items
            .Select(item => new KeyValueListItem(item.Key, item.Value))
            .ToArray();
    }
}
