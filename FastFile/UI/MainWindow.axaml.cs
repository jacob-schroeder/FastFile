using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FastFile.Logic;
using FastFile.Models;
using FastFile.Models.Archive;
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
    private FastFileDocument? _document;
    private DB_Header? _fastFileHeader;
    private XFile? _zoneHeader;
    private XAssetList? _assetList;
    private readonly List<string> _logMessages = new();

    public MainWindow()
    {
        InitializeComponent();
        SelectMainTab("FastFile");
        UpdateDocumentState();
    }

    private void MainTabButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tabName })
        {
            SelectMainTab(tabName);
        }
    }

    private void SelectMainTab(string tabName)
    {
        var isFastFile = string.Equals(tabName, "FastFile", StringComparison.Ordinal);
        var isZone = string.Equals(tabName, "Zone", StringComparison.Ordinal);
        var isAssets = string.Equals(tabName, "Assets", StringComparison.Ordinal);
        var isLog = string.Equals(tabName, "Log", StringComparison.Ordinal);

        FastFileTabView.IsVisible = isFastFile;
        ZoneTabView.IsVisible = isZone;
        AssetsTabView.IsVisible = isAssets;
        LogTabView.IsVisible = isLog;

        FastFileMainTabButton.Classes.Set("active", isFastFile);
        ZoneMainTabButton.Classes.Set("active", isZone);
        AssetsMainTabButton.Classes.Set("active", isAssets);
        LogMainTabButton.Classes.Set("active", isLog);
    }

    private void NewMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        _document = FastFileDocument.CreateNew();
        _buffer = _document.Buffer;
        _fastFileHeader = _document.Header;
        _zoneHeader = _document.ZoneHeader;
        _assetList = _document.AssetList;

        ResetLog();
        AddLog("INFO", "Initialized a new fastfile document");

        UpdateFastFileHeaderView();
        UpdateZoneTabView();
        UpdateAssetsTabView();
        UpdateDocumentState();

        FastFileTabView.SetStatus("New fastfile\nAssets: 0");
        UpdateLogView();
    }

    private async void OpenMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(OpenFastFileAsync, DispatcherPriority.Background);
    }

    private async Task OpenFastFileAsync()
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

            var buffer = memoryStream.ToArray();
            AddLog("INFO", $"Read {buffer.Length:N0} bytes");

            await Task.Run(() => ParseFastFile(buffer));
            UpdateFastFileHeaderView();
            UpdateZoneTabView();
            UpdateAssetsTabView();
            UpdateDocumentState();

            FastFileTabView.SetStatus($"Opened: {file.Name}\nAssets: {_assetList?.AssetCount ?? 0}");
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
        var fastFileHeader = ffReader.ParseHeader();
        AddLog("INFO", $"Fastfile header parsed: magic={fastFileHeader.Magic}, version={fastFileHeader.Version}, size={fastFileHeader.FileSize:N0} bytes");

        AddLog("INFO", "Unpacking zone data");
        var zone = ffReader.UnpackZone();
        AddLog("INFO", $"Zone unpacked: {zone.Length:N0} bytes");
        AddWarnings("FastFileReader", ffReader.Warnings);
        
        var zoneReader = new ZoneReader(zone);
        AddLog("INFO", "Parsing zone header");
        var zoneHeader = zoneReader.ParseHeader();
        AddLog("INFO", $"Zone header parsed: size={zoneHeader.Size:N0}, externalSize={zoneHeader.ExternalSize:N0}");

        AddLog("INFO", "Parsing asset list");
        var assetList = zoneReader.ParseXAssetList();

        AddLog("INFO", $"Asset list parsed: assets={assetList.AssetCount:N0}, scriptStrings={assetList.ScriptStringCount:N0}");
        AddWarnings("ZoneReader", zoneReader.Warnings);

        _buffer = buffer;
        _fastFileHeader = fastFileHeader;
        _zoneHeader = zoneHeader;
        _assetList = assetList;
        _document = FastFileDocument.FromParsed(buffer, fastFileHeader, zoneHeader, assetList);
    }

    private void UpdateFastFileHeaderView()
    {
        FastFileTabView.UpdateHeader(_fastFileHeader);
    }

    private void UpdateZoneTabView()
    {
        ZoneTabView.UpdateZone(_zoneHeader,  _assetList);
    }

    private void UpdateAssetsTabView()
    {
        AssetsTabView.UpdateAssets(_assetList);
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

    private void SaveMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (_document is null)
        {
            return;
        }

        AddLog("INFO", _document.IsNew
            ? "Save requested for new fastfile document"
            : "Save requested for opened fastfile document");
        UpdateLogView();
    }

    private void UpdateDocumentState()
    {
        SaveMenuItem.IsEnabled = _document is not null;
    }

    private void CloseMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var repoUri = new Uri("https://github.com/jacob-schroeder/FastFile");
        var docsUri = new Uri("https://codresearch.dev/index.php/Main_Page");

        var dialog = new Window
        {
            Title = "About FastFile",
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = "FastFile",
                        FontSize = 20,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "Created by: Jacob Schroeder"
                    },
                    new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock { Text = "Repository" },
                            new HyperlinkButton
                            {
                                Content = repoUri.ToString(),
                                NavigateUri = repoUri,
                                Padding = new Avalonia.Thickness(0)
                            }
                        }
                    },
                    new StackPanel
                    {
                        Spacing = 2,
                        Children =
                        {
                            new TextBlock { Text = "Research" },
                            new HyperlinkButton
                            {
                                Content = docsUri.ToString(),
                                NavigateUri = docsUri,
                                Padding = new Avalonia.Thickness(0)
                            }
                        }
                    }
                }
            }
        };

        await dialog.ShowDialog(this);
    }
}
