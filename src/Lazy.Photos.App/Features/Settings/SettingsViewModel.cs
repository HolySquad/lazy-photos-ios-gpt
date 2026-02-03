using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Services;

namespace Lazy.Photos.App.Features.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IPhotoCacheService _photoCacheService;
    private readonly IPhotoLibraryService _photoLibraryService;
    private readonly IPhotoCacheMaintenance _cacheMaintenance;
    private readonly IAppSettingsService _appSettingsService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    private bool autoSyncEnabled = true;

    [ObservableProperty]
    private bool wifiOnlySync = true;

    [ObservableProperty]
    private int selectedUploadQualityIndex;

    public IReadOnlyList<UploadQualityOption> UploadQualityOptions { get; } = new[]
    {
        new UploadQualityOption("Original", "Full resolution, larger file size"),
        new UploadQualityOption("High quality", "Slightly compressed, saves storage"),
        new UploadQualityOption("Storage saver", "Reduced resolution, minimal storage")
    };

    public string SelectedUploadQualityDescription =>
        UploadQualityOptions[SelectedUploadQualityIndex].Description;

    partial void OnSelectedUploadQualityIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedUploadQualityDescription));
    }

    [ObservableProperty]
    private int cachedCount;

    [ObservableProperty]
    private int syncedCount;

    [ObservableProperty]
    private int deviceCount;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string databaseSize = "—";

    [ObservableProperty]
    private string thumbnailCacheSize = "—";

    [ObservableProperty]
    private bool isIndexing;

    [ObservableProperty]
    private string apiUrl = string.Empty;

    [ObservableProperty]
    private bool isTestingConnection;

    [ObservableProperty]
    private string connectionStatus = string.Empty;

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Gray;

    public bool IsNotIndexing => !IsIndexing;
    public bool IsNotTesting => !IsTestingConnection;
    public bool HasStatus => !string.IsNullOrEmpty(Status);
    public bool HasConnectionStatus => !string.IsNullOrEmpty(ConnectionStatus);
    public string AppVersion => "1.0.0";

    public SettingsViewModel(
        IPhotoCacheService photoCacheService,
        IPhotoLibraryService photoLibraryService,
        IPhotoCacheMaintenance cacheMaintenance,
        IAppSettingsService appSettingsService)
    {
        _photoCacheService = photoCacheService;
        _photoLibraryService = photoLibraryService;
        _cacheMaintenance = cacheMaintenance;
        _appSettingsService = appSettingsService;

        // Load API URL on initialization
        _ = LoadApiUrlAsync();
    }

    private async Task LoadApiUrlAsync()
    {
        ApiUrl = await _appSettingsService.GetApiUrlAsync() ?? "http://localhost:5000";
    }

    partial void OnIsIndexingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotIndexing));
    }

    partial void OnIsTestingConnectionChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotTesting));
    }

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatus));
    }

    partial void OnConnectionStatusChanged(string value)
    {
        OnPropertyChanged(nameof(HasConnectionStatus));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            Status = "Loading...";
            var cached = await _photoCacheService.GetCachedPhotosAsync(ct);
            CachedCount = cached.Count;
            SyncedCount = cached.Count(p => p.IsSynced);

            var devicePhotos = await _photoLibraryService.GetRecentPhotosAsync(500, ct);
            DeviceCount = devicePhotos.Count;

            DatabaseSize = await _cacheMaintenance.GetDatabaseSizeAsync(ct);
            ThumbnailCacheSize = await GetThumbnailCacheSizeAsync();

            Status = string.Empty;
        }
        catch (OperationCanceledException)
        {
            Status = "Canceled";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        try
        {
            Status = "Clearing cache...";
            await _cacheMaintenance.ClearCacheAsync(CancellationToken.None);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task IndexAsync()
    {
        if (IsIndexing)
            return;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            IsIndexing = true;
            Status = "Indexing device photos...";
            var devicePhotos = await _photoLibraryService.GetRecentPhotosAsync(500, ct);
            await _photoCacheService.SavePhotosAsync(devicePhotos, ct);
            Status = "Indexing complete";
            await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
            Status = "Index canceled";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsIndexing = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionStatus = "Testing...";
        ConnectionStatusColor = Colors.Gray;

        try
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
            var response = await httpClient.GetAsync("/health");

            if (response.IsSuccessStatusCode)
            {
                ConnectionStatus = "✓ Connected successfully";
                ConnectionStatusColor = Colors.Green;
            }
            else
            {
                ConnectionStatus = "✗ Server responded with error";
                ConnectionStatusColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"✗ Connection failed: {ex.Message}";
            ConnectionStatusColor = Colors.Red;
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task SaveApiUrlAsync()
    {
        await _appSettingsService.SetApiUrlAsync(ApiUrl);
        Status = "Server URL saved. Restart app to apply changes.";

        // Clear connection status after save
        ConnectionStatus = string.Empty;
    }

    private static Task<string> GetThumbnailCacheSizeAsync()
    {
        return Task.Run(() =>
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "thumbcache");
            if (!Directory.Exists(dir))
                return "0 KB";
            long total = 0;
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
            {
                try { total += new FileInfo(file).Length; } catch { /* ignore */ }
            }

            return total switch
            {
                < 1024 => $"{total} B",
                < 1024 * 1024 => $"{total / 1024.0:F1} KB",
                _ => $"{total / 1024.0 / 1024.0:F1} MB"
            };
        });
    }
}

public sealed record UploadQualityOption(string Name, string Description)
{
    public override string ToString() => Name;
}
