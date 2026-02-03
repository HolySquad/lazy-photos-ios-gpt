using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.Data;

namespace Lazy.Photos.App.Features.Albums;

public sealed partial class AlbumPhotoPickerViewModel : ObservableObject
{
	private readonly IPhotosApiClient _apiClient;
	private readonly IAlbumService _albumService;
	private CancellationTokenSource? _loadCts;
	private string? _albumId;

	[ObservableProperty]
	private ObservableCollection<PhotoItem> photos = new();

	[ObservableProperty]
	private bool isLoading;

	[ObservableProperty]
	private bool isSaving;

	[ObservableProperty]
	private string? errorMessage;

	[ObservableProperty]
	private string title = "Add Photos";

	public ObservableCollection<PhotoItem> SelectedPhotos { get; } = new();

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public AlbumPhotoPickerViewModel(IPhotosApiClient apiClient, IAlbumService albumService)
	{
		_apiClient = apiClient;
		_albumService = albumService;
		SelectedPhotos.CollectionChanged += OnSelectedPhotosChanged;
	}

	partial void OnErrorMessageChanged(string? value)
	{
		OnPropertyChanged(nameof(HasError));
	}

	partial void OnIsSavingChanged(bool value)
	{
		AddSelectedCommand.NotifyCanExecuteChanged();
	}

	private void OnSelectedPhotosChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		AddSelectedCommand.NotifyCanExecuteChanged();
	}

	public void SetAlbum(string albumId, string? albumName)
	{
		_albumId = albumId;
		Title = string.IsNullOrWhiteSpace(albumName) ? "Add Photos" : $"Add to {albumName}";
	}

	public async Task EnsureLoadedAsync()
	{
		if (Photos.Count > 0)
			return;

		await LoadAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		await LoadAsync();
	}

	[RelayCommand(CanExecute = nameof(CanAddSelected))]
	private async Task AddSelectedAsync()
	{
		if (string.IsNullOrWhiteSpace(_albumId))
			return;

		try
		{
			IsSaving = true;
			ErrorMessage = null;

			foreach (var photo in SelectedPhotos)
			{
				if (string.IsNullOrWhiteSpace(photo.Id))
					continue;

				await _albumService.AddPhotoToAlbumAsync(_albumId, photo.Id, CancellationToken.None);
			}

			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Failed to add photos: {ex.Message}";
		}
		finally
		{
			IsSaving = false;
		}
	}

	[RelayCommand]
	private async Task CancelAsync()
	{
		await Shell.Current.GoToAsync("..");
	}

	private bool CanAddSelected() => !IsSaving && SelectedPhotos.Count > 0;

	private async Task LoadAsync()
	{
		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();
		var ct = _loadCts.Token;

		try
		{
			IsLoading = true;
			ErrorMessage = null;

			var response = await _apiClient.GetPhotosAsync(cursor: null, limit: 200, ct);
			var items = response.Photos.Select(PhotoItemMapper.FromDto).ToList();

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
			ErrorMessage = $"Failed to load photos: {ex.Message}";
		}
		finally
		{
			IsLoading = false;
		}
	}
}
