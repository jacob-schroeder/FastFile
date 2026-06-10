using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models;
using FastFile.Models.Archive;
using FastFile.Models.Zone;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UI;

public partial class MainWindow : Window
{
    private byte[]? _buffer;
    private FastFileDocument? _document;
    private DB_Header? _fastFileHeader;
    private XFile? _zoneHeader;
    private XAssetListOLD? _assetList;
    private string? _currentFileName;
    private string? _currentFilePath;
    private readonly List<string> _logMessages = new();
    private readonly object _loadProgressGate = new();
    private int _loadingStatusVersion;
    private int _lastQueuedAssetLoadPercent = -1;

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
        _assetList = _document.AssetListOld;
        _currentFileName = null;
        _currentFilePath = null;

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
        await OpenFastFileAsync();
    }

    private async Task OpenFastFileAsync()
    {
        bool loadStarted = false;
        var loadingStatusVersion = -1;

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
            UpdateLogView();
            var filePath = file.TryGetLocalPath();
            UpdateOpenFileStatus($"Opening {file.Name}", filePath ?? file.Name);
            BeginLoadingStatus("Reading file");
            loadStarted = true;

            await using var fileStream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var buffer = memoryStream.ToArray();

            BeginLoadingStatus("Loading zone");
            AddLog("INFO", "Parsing header, zone, and assets");
            UpdateLogView();
            loadingStatusVersion = Volatile.Read(ref _loadingStatusVersion);
            var parseResult = await Task.Run(() => ParseFastFile(buffer, QueueAssetReadProgress));

            _currentFileName = file.Name;
            _currentFilePath = filePath;
            _buffer = parseResult.Buffer;
            _fastFileHeader = parseResult.Header;
            _zoneHeader = parseResult.ZoneHeader;
            /*
            _assetList = parseResult.AssetList;
            _document = FastFileDocument.FromParsed(
                parseResult.Buffer,
                parseResult.Header,
                parseResult.ZoneHeader,
                parseResult.AssetListOld,
                parseResult.Zone);
            */
            return;
            AddWarnings("FastFileReader", parseResult.Warnings);

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
            UpdateOpenFileStatus();
            FastFileTabView.SetStatus($"Unable to open file.\n{ex.Message}");
            AddLog("ERROR", ex.Message);
            UpdateLogView();
        }
        finally
        {
            if (loadStarted && loadingStatusVersion == Volatile.Read(ref _loadingStatusVersion))
                CompleteLoadingStatus();
        }
    }

    private sealed record ParseResult(
        byte[] Buffer,
        DB_Header Header,
        XFile ZoneHeader,
        XAssetList AssetListOld,
        byte[] Zone,
        IReadOnlyList<string> Warnings);

    private ParseResult ParseFastFile(byte[] buffer, Action<int, int>? assetReadProgress)
    {
        var ffReader = new FastFileReader(buffer, buffer.Length);
        var fastFileHeader = ffReader.ParseHeader();

        var zone = ffReader.UnpackZone();

        var zoneReader = new XFileReader(zone, assetReadProgress).Read();
        var zoneHeader = zoneReader.GetHeader();
        var assetList = zoneReader.GetAssetList();

        return new ParseResult(
            buffer,
            fastFileHeader,
            zoneHeader,
            assetList,
            zone,
            [..ffReader.Warnings]);
    }

    private void BeginLoadingStatus(string message)
    {
        Interlocked.Increment(ref _loadingStatusVersion);
        lock (_loadProgressGate)
        {
            _lastQueuedAssetLoadPercent = -1;
        }

        LoadingStatusPanel.IsVisible = true;
        LoadingStatusTextBlock.Text = message;
        LoadingProgressBar.IsIndeterminate = true;
        LoadingProgressBar.Value = 0;
        LoadingPercentTextBlock.Text = string.Empty;
    }

    private void QueueAssetReadProgress(int assetsRead, int assetCount)
    {
        if (assetCount <= 0)
            return;

        var percent = GetAssetReadPercent(assetsRead, assetCount);
        lock (_loadProgressGate)
        {
            if (percent <= _lastQueuedAssetLoadPercent)
                return;

            _lastQueuedAssetLoadPercent = percent;
        }

        var loadingStatusVersion = Volatile.Read(ref _loadingStatusVersion);
        Dispatcher.UIThread.Post(
            () => UpdateAssetReadProgress(loadingStatusVersion, assetsRead, assetCount),
            DispatcherPriority.Background);
    }

    private void UpdateAssetReadProgress(int loadingStatusVersion, int assetsRead, int assetCount)
    {
        if (loadingStatusVersion != Volatile.Read(ref _loadingStatusVersion)
            || !LoadingStatusPanel.IsVisible)
        {
            return;
        }

        var percent = GetAssetReadPercent(assetsRead, assetCount);
        var displayedPercent = Math.Max((int)Math.Round(LoadingProgressBar.Value), percent);
        LoadingStatusTextBlock.Text = "Loading zone";
        LoadingProgressBar.IsIndeterminate = false;
        LoadingProgressBar.Value = displayedPercent;
        LoadingPercentTextBlock.Text = $"{displayedPercent}%";
    }

    private static int GetAssetReadPercent(int assetsRead, int assetCount)
    {
        if (assetCount <= 0)
            return 0;

        return Math.Clamp((int)Math.Round(assetsRead * 100d / assetCount), 0, 100);
    }

    private void CompleteLoadingStatus()
    {
        ClearLoadingStatus();
    }

    private void ClearLoadingStatus()
    {
        Interlocked.Increment(ref _loadingStatusVersion);
        lock (_loadProgressGate)
        {
            _lastQueuedAssetLoadPercent = -1;
        }

        LoadingStatusPanel.IsVisible = false;
        LoadingProgressBar.IsIndeterminate = false;
        LoadingProgressBar.Value = 0;
        LoadingPercentTextBlock.Text = string.Empty;
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

    private async void SaveMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        await SaveFastFileAsync();
    }

    private async Task SaveFastFileAsync()
    {
        throw new NotImplementedException();
    }

    private string GetSuggestedFastFileName()
    {
        if (!string.IsNullOrWhiteSpace(_currentFileName))
            return _currentFileName;

        return "untitled.ff";
    }

    private void UpdateDocumentState()
    {
        SaveMenuItem.IsEnabled = _document is not null;
        UpdateOpenFileStatus();
    }

    private void UpdateOpenFileStatus(string? displayText = null, string? tooltip = null)
    {
        if (string.IsNullOrWhiteSpace(displayText))
        {
            displayText = _document is null
                ? "None"
                : string.IsNullOrWhiteSpace(_currentFileName)
                    ? "Untitled"
                    : _currentFileName;

            tooltip = string.IsNullOrWhiteSpace(_currentFilePath)
                ? displayText
                : _currentFilePath;
        }

        OpenFileStatusTextBlock.Text = displayText;
        ToolTip.SetTip(OpenFileStatusBadge, tooltip ?? displayText);
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
