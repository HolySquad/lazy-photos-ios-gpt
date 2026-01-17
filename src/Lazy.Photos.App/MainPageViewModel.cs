using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Models;
using Lazy.Photos.App.Services;
using Microsoft.Maui.ApplicationModel;

namespace Lazy.Photos.App;

public partial class MainPageViewModel : ObservableObject
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private CancellationTokenSource? _loadCts;
	private bool _loadedOnce;

	[ObservableProperty]
	private ObservableCollection<PhotoItem> photos = new();

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

	public MainPageViewModel(IPhotoLibraryService photoLibraryService)
	{
		_photoLibraryService = photoLibraryService;
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

	private static async Task<bool> EnsurePhotosPermissionAsync()
	{
#if ANDROID
		PermissionStatus status;
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			status = await Permissions.CheckStatusAsync<ReadMediaImagesPermission>();
			if (status != PermissionStatus.Granted)
				status = await Permissions.RequestAsync<ReadMediaImagesPermission>();
		}
		else
		{
			status = await Permissions.CheckStatusAsync<ReadExternalStoragePermission>();
			if (status != PermissionStatus.Granted)
				status = await Permissions.RequestAsync<ReadExternalStoragePermission>();
		}
#else
		var status = await Permissions.CheckStatusAsync<Permissions.Photos>();
		if (status != PermissionStatus.Granted)
			status = await Permissions.RequestAsync<Permissions.Photos>();
#endif
		return status == PermissionStatus.Granted;
	}
}
