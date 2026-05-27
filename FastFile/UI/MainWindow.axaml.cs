using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FastFile.Logic;
using FastFile.Models;
using FastFile.Models.Zone;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI;

public partial class MainWindow : Window
{
    private byte[]? _buffer;
    private DB_Header? _fastFileHeader;
    private XFile? _zoneHeader;
    private XAssetList? _assetList;
    private readonly List<string> _logMessages = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (StorageProvider is null)
            {
                return;
            }

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open FastFile",
                AllowMultiple = false
            });

            var file = files.FirstOrDefault();
            if (file is null)
            {
                return;
            }

            ResetLog();
            AddLog("INFO", $"Opening {file.Name}");

            await using var fileStream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            _buffer = memoryStream.ToArray();
            AddLog("INFO", $"Read {_buffer.Length:N0} bytes");

            ParseFastFile(_buffer);
            UpdateFastFileHeaderView();

            FastFileTabView.SetStatus($"Opened {file.Name}\nAssets: {_assetList?.AssetCount ?? 0}");
            AddLog("INFO", "File load complete");
            UpdateLogView();
        }
        catch (Exception ex)
        {
            FastFileTabView.SetStatus($"Unable to open file.\n{ex.Message}");
            AddLog("ERROR", ex.Message);
            UpdateLogView();
        }
    }

    private void ParseFastFile(byte[] buffer)
    {
        var ffReader = new FastFileReader(buffer, buffer.Length);
        AddLog("INFO", "Parsing fastfile header");
        _fastFileHeader = ffReader.ParseHeader();
        AddLog("INFO", $"Fastfile header parsed: magic={_fastFileHeader.Magic}, version={_fastFileHeader.Version}, size={_fastFileHeader.FileSize:N0} bytes");

        AddLog("INFO", "Unpacking zone data");
        var zone = ffReader.UnpackZone();
        AddLog("INFO", $"Zone unpacked: {zone.Length:N0} bytes");
        AddWarnings("FastFileReader", ffReader.Warnings);

        var zoneReader = new ZoneReader(zone);
        AddLog("INFO", "Parsing zone header");
        _zoneHeader = zoneReader.ParseHeader();
        AddLog("INFO", $"Zone header parsed: size={_zoneHeader.Size:N0}, externalSize={_zoneHeader.ExternalSize:N0}");

        AddLog("INFO", "Parsing asset list");
        _assetList = zoneReader.ParseXAssetList();
        AddLog("INFO", $"Asset list parsed: assets={_assetList.AssetCount:N0}, scriptStrings={_assetList.ScriptStringCount:N0}");
        AddWarnings("ZoneReader", zoneReader.Warnings);
    }

    private void UpdateFastFileHeaderView()
    {
        FastFileTabView.UpdateHeader(_fastFileHeader);
    }

    private void ResetLog()
    {
        _logMessages.Clear();
        UpdateLogView();
    }

    private void AddLog(string level, string message)
    {
        _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
    }

    private void AddWarnings(string source, IReadOnlyList<string> warnings)
    {
        foreach (var warning in warnings)
        {
            AddLog("WARN", $"{source}: {warning}");
        }
    }

    private void UpdateLogView()
    {
        var text = _logMessages.Count == 0
            ? "Open a fastfile to view parse output."
            : string.Join(Environment.NewLine, _logMessages);

        LogTabView.SetLogText(text);
    }

    private void CloseMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About FastFile",
            Width = 320,
            Height = 180,
            CanResize = false,
            Content = new TextBlock
            {
                Text = "FastFile",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };

        await dialog.ShowDialog(this);
    }
}
