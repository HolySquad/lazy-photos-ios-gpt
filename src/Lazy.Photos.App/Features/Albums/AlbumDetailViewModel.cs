using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Albums;

public sealed partial class AlbumDetailViewModel : ObservableObject
{
	private readonly IAlbumService _albumService;
	private readonly IPhotoNavigationService _photoNavigationService;
	private CancellationTokenSource? _loadCts;
	private string? _albumId;

	[ObservableProperty]
	private ObservableCollection<PhotoItem> photos = new();

	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private bool isRefreshing;

	[ObservableProperty]
	private string? errorMessage;

	[ObservableProperty]
	private int columns = 3;

	[ObservableProperty]
	private double cellSize = 120;

	[ObservableProperty]
	private string albumTitle = "Album";

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	partial void OnErrorMessageChanged(string? value)
	{
		OnPropertyChanged(nameof(HasError));
	}

	public AlbumDetailViewModel(IAlbumService albumService, IPhotoNavigationService photoNavigationService)
	{
		_albumService = albumService;
		_photoNavigationService = photoNavigationService;
	}

	public void SetAlbum(string albumId, string? albumName)
	{
		_albumId = albumId;
		AlbumTitle = string.IsNullOrWhiteSpace(albumName) ? "Album" : albumName;
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
		if (IsLoading)
			return;

		IsRefreshing = true;
		await LoadAsync();
	}

	[RelayCommand]
	private async Task ShowAddPhotosAsync()
	{
		if (string.IsNullOrWhiteSpace(_albumId))
			return;

		await Shell.Current.GoToAsync(nameof(AlbumPhotoPickerPage), true, new Dictionary<string, object>
		{
			{ "albumId", _albumId },
			{ "albumName", AlbumTitle }
		});
	}

	[RelayCommand]
	private async Task RemovePhotoAsync(PhotoItem? photo)
	{
		if (photo?.Id is null || string.IsNullOrWhiteSpace(_albumId))
			return;

		try
		{
			await _albumService.RemovePhotoFromAlbumAsync(_albumId, photo.Id, CancellationToken.None);
			Photos.Remove(photo);
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Failed to remove photo: {ex.Message}";
		}
	}

	[RelayCommand]
	private async Task OpenPhotoAsync(PhotoItem? photo)
	{
		if (photo == null)
			return;

		await _photoNavigationService.ShowPhotoAsync(photo, new ReadOnlyCollection<PhotoItem>(Photos));
	}

	public async Task EnsureLoadedAsync()
	{
		if (Photos.Count > 0)
			return;

		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();
		var ct = _loadCts.Token;

		if (string.IsNullOrWhiteSpace(_albumId))
		{
			ErrorMessage = "Album not found.";
			IsLoading = false;
			IsRefreshing = false;
			return;
		}

		try
		{
			IsLoading = true;
			ErrorMessage = null;

			var items = await _albumService.GetAlbumPhotosAsync(_albumId, ct);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				Photos = new ObservableCollection<PhotoItem>(items);
			});
		}
		catch (OperationCanceledException)
		{
			// Ignore cancellation
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Failed to load album photos: {ex.Message}";
			OnPropertyChanged(nameof(HasError));
		}
		finally
		{
			IsLoading = false;
			IsRefreshing = false;
		}
	}
}
