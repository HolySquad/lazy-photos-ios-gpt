using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Services;
using Microsoft.Maui.ApplicationModel;

namespace Lazy.Photos.App.Features.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
	private readonly IPhotoCacheService _photoCacheService;
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoCacheMaintenance _cacheMaintenance;
	private readonly IAppSettingsService _appSettingsService;
	private readonly IAuthenticationService _authService;
	private CancellationTokenSource? _loadCts;
	private bool _isLoadingPreferences;

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
		if (_isLoadingPreferences)
			return;
		_ = _appSettingsService.SetUploadQualityIndexAsync(value);
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

	[ObservableProperty]
	private string accountTitle = "Not signed in";

	[ObservableProperty]
	private string accountSubtitle = "Sign in to sync your photos";

	[ObservableProperty]
	private string accountActionText = "Sign In";

	[ObservableProperty]
	private bool isSignedIn;

	public bool IsNotIndexing => !IsIndexing;
	public bool IsNotTesting => !IsTestingConnection;
	public bool HasStatus => !string.IsNullOrEmpty(Status);
	public bool HasConnectionStatus => !string.IsNullOrEmpty(ConnectionStatus);
	public string AppVersion => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";

	public SettingsViewModel(
		IPhotoCacheService photoCacheService,
		IPhotoLibraryService photoLibraryService,
		IPhotoCacheMaintenance cacheMaintenance,
		IAppSettingsService appSettingsService,
		IAuthenticationService authService)
	{
		_photoCacheService = photoCacheService;
		_photoLibraryService = photoLibraryService;
		_cacheMaintenance = cacheMaintenance;
		_appSettingsService = appSettingsService;
		_authService = authService;

		// Load API URL on initialization
		_ = LoadApiUrlAsync();
		_ = LoadPreferencesAsync();
	}

	private async Task LoadApiUrlAsync()
	{
		ApiUrl = await _appSettingsService.GetApiUrlAsync() ?? "http://192.168.0.161:5175/health";
	}

	private async Task LoadPreferencesAsync()
	{
		_isLoadingPreferences = true;
		try
		{
			AutoSyncEnabled = await _appSettingsService.GetAutoSyncEnabledAsync();
			WifiOnlySync = await _appSettingsService.GetWifiOnlySyncAsync();
			var index = await _appSettingsService.GetUploadQualityIndexAsync();
			SelectedUploadQualityIndex = Math.Clamp(index, 0, UploadQualityOptions.Count - 1);
		}
		finally
		{
			_isLoadingPreferences = false;
		}
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

	partial void OnAutoSyncEnabledChanged(bool value)
	{
		if (_isLoadingPreferences)
			return;
		_ = _appSettingsService.SetAutoSyncEnabledAsync(value);
	}

	partial void OnWifiOnlySyncChanged(bool value)
	{
		if (_isLoadingPreferences)
			return;
		_ = _appSettingsService.SetWifiOnlySyncAsync(value);
	}

	public async Task InitializeAsync()
	{
		await LoadAccountAsync();
		await RefreshAsync();
	}

	private async Task LoadAccountAsync()
	{
		var isAuthenticated = await _authService.IsAuthenticatedAsync();
		var email = await _authService.GetCurrentUserEmailAsync();

		IsSignedIn = isAuthenticated;

		if (isAuthenticated)
		{
			AccountTitle = string.IsNullOrWhiteSpace(email) ? "Signed in" : email;
			AccountSubtitle = "Account connected";
			AccountActionText = "Sign Out";
		}
		else
		{
			AccountTitle = "Not signed in";
			AccountSubtitle = "Sign in to sync your photos";
			AccountActionText = "Sign In";
		}
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

	[RelayCommand]
	private async Task AccountActionAsync()
	{
		if (IsSignedIn)
		{
			await _authService.LogoutAsync();
			await _appSettingsService.SetUserEmailAsync(string.Empty);
			await LoadAccountAsync();
			Status = "Signed out";
			return;
		}

		await Shell.Current.GoToAsync("onboarding");
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
