using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.DevStats;

public sealed partial class DevStatsViewModel : ObservableObject
{
	private readonly IPhotoCacheService _photoCacheService;
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoCacheMaintenance _cacheMaintenance;
	private CancellationTokenSource? _loadCts;

	[ObservableProperty]
	private int cachedCount;

	[ObservableProperty]
	private int syncedCount;

	[ObservableProperty]
	private int deviceCount;

	[ObservableProperty]
	private string status = "Idle";

	[ObservableProperty]
	private string databaseSize = "Unknown";

	public DevStatsViewModel(
		IPhotoCacheService photoCacheService,
		IPhotoLibraryService photoLibraryService,
		IPhotoCacheMaintenance cacheMaintenance)
	{
		_photoCacheService = photoCacheService;
		_photoLibraryService = photoLibraryService;
		_cacheMaintenance = cacheMaintenance;
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

			Status = "OK";
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
			Status = "Clearing...";
			await _cacheMaintenance.ClearCacheAsync(CancellationToken.None);
			await RefreshAsync();
		}
		catch (Exception ex)
		{
			Status = $"Error: {ex.Message}";
		}
	}
}
