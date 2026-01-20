using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using Lazy.Photos.App.Features.Photos.Permissions;
#endif

namespace Lazy.Photos.App.Features.Photos;

public partial class MainPageViewModel : ObservableObject
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoNavigationService _photoNavigationService;
	private readonly IPhotoSyncService _photoSyncService;
	private CancellationTokenSource? _loadCts;
	private bool _loadedOnce;

	[ObservableProperty]
	private ObservableCollection<PhotoItem> photos = new();

	[ObservableProperty]
	private ObservableCollection<PhotoSection> photoSections = new();

	[ObservableProperty]
	private bool isBusy;

	[ObservableProperty]
	private string? errorMessage;

	[ObservableProperty]
	private bool hasError;

	[ObservableProperty]
	private int columns = 3;

	[ObservableProperty]
	private double cellSize = 120;

	public MainPageViewModel(
		IPhotoLibraryService photoLibraryService,
		IPhotoNavigationService photoNavigationService,
		IPhotoSyncService photoSyncService)
	{
		_photoLibraryService = photoLibraryService;
		_photoNavigationService = photoNavigationService;
		_photoSyncService = photoSyncService;
	}

	partial void OnErrorMessageChanged(string? value)
	{
		HasError = !string.IsNullOrWhiteSpace(value);
	}

	public async Task EnsureLoadedAsync()
	{
		if (_loadedOnce)
			return;

		_loadedOnce = true;
		await RefreshAsync();
	}

	public void UpdateGrid(int newColumns, double newCellSize)
	{
		if (Columns != newColumns)
			Columns = newColumns;

		if (Math.Abs(CellSize - newCellSize) >= 0.5)
			CellSize = newCellSize;
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		if (IsBusy)
			return;

		IsBusy = true;
		ErrorMessage = null;

		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();

		try
		{
			var remoteItems = await TryLoadRemoteAsync(_loadCts.Token);
			var deviceItems = await LoadDevicePhotosAsync(_loadCts.Token);
			var items = MergePhotos(remoteItems, deviceItems);

			Photos.Clear();
			foreach (var item in items)
				Photos.Add(item);

			RebuildSections();
		}
		catch (OperationCanceledException)
		{
			// Ignore cancellation.
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Failed to load photos: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void RebuildSections()
	{
		PhotoSections.Clear();
		if (Photos.Count == 0)
			return;

		var grouped = Photos
			.GroupBy(p => string.IsNullOrWhiteSpace(p.FolderName) ? "Unknown" : p.FolderName)
			.OrderByDescending(g => g.Max(p => p.TakenAt ?? DateTimeOffset.MinValue));

		foreach (var group in grouped)
			PhotoSections.Add(new PhotoSection(group.Key, group));
	}

	[RelayCommand]
	private async Task OpenPhotoAsync(PhotoItem? photo)
	{
		if (photo == null)
			return;

		await _photoNavigationService.ShowPhotoAsync(photo);
	}

	private static async Task<bool> EnsurePhotosPermissionAsync()
	{
#if ANDROID
		PermissionStatus status;
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			status = await MauiPermissions.CheckStatusAsync<ReadMediaImagesPermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadMediaImagesPermission>();
		}
		else
		{
			status = await MauiPermissions.CheckStatusAsync<ReadExternalStoragePermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadExternalStoragePermission>();
		}
#else
		var status = await MauiPermissions.CheckStatusAsync<MauiPermissions.Photos>();
		if (status != PermissionStatus.Granted)
			status = await MauiPermissions.RequestAsync<MauiPermissions.Photos>();
#endif
		return status == PermissionStatus.Granted;
	}

	private async Task<IReadOnlyList<PhotoItem>> TryLoadRemoteAsync(CancellationToken ct)
	{
		try
		{
			return await _photoSyncService.GetRecentAsync(300, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return Array.Empty<PhotoItem>();
		}
	}

	private async Task<IReadOnlyList<PhotoItem>> LoadDevicePhotosAsync(CancellationToken ct)
	{
		var permissionGranted = await EnsurePhotosPermissionAsync();
		if (!permissionGranted)
		{
			ErrorMessage = "Showing cloud photos. Photo permission is required to show device photos.";
			return Array.Empty<PhotoItem>();
		}

		return await _photoLibraryService.GetRecentPhotosAsync(300, ct);
	}

	private static IReadOnlyList<PhotoItem> MergePhotos(
		IReadOnlyList<PhotoItem> remoteItems,
		IReadOnlyList<PhotoItem> deviceItems)
	{
		var merged = new Dictionary<string, PhotoItem>(StringComparer.OrdinalIgnoreCase);

		foreach (var item in remoteItems)
		{
			var key = BuildKey(item);
			if (!merged.ContainsKey(key))
				merged[key] = item;
		}

		foreach (var item in deviceItems)
		{
			var key = BuildKey(item);
			if (!merged.ContainsKey(key))
				merged[key] = item;
		}

		return merged.Values.ToList();
	}

	private static string BuildKey(PhotoItem item)
	{
		if (!string.IsNullOrWhiteSpace(item.Hash))
			return $"hash:{item.Hash}";

		if (!string.IsNullOrWhiteSpace(item.Id))
			return $"id:{item.Id}";

		return $"fallback:{item.DisplayName}:{item.TakenAt?.UtcDateTime:o}";
	}
}
