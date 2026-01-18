using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using Lazy.Photos.App.Features.Photos.Permissions;
#endif

namespace Lazy.Photos.App.Features.Photos;

public partial class MainPageViewModel : ObservableObject
{
	private static readonly string[] SectionTitles = { "Download", "Pictures", "Movies" };
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoNavigationService _photoNavigationService;
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

	public MainPageViewModel(IPhotoLibraryService photoLibraryService, IPhotoNavigationService photoNavigationService)
	{
		_photoLibraryService = photoLibraryService;
		_photoNavigationService = photoNavigationService;
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
			var permissionGranted = await EnsurePhotosPermissionAsync();
			if (!permissionGranted)
			{
				ErrorMessage = "Photo permission is required to show device photos.";
				return;
			}

			var items = await _photoLibraryService.GetRecentPhotosAsync(300, _loadCts.Token);
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

		foreach (var title in SectionTitles)
			PhotoSections.Add(new PhotoSection(title, Photos));
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
}
