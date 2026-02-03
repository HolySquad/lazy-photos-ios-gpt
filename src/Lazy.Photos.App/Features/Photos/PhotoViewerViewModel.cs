using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Photos;

public partial class PhotoViewerViewModel : ObservableObject
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private CancellationTokenSource? _loadCts;
	private IReadOnlyList<PhotoItem> _photos = Array.Empty<PhotoItem>();
	private int _currentIndex = -1;

	[ObservableProperty]
	private PhotoItem? photo;

	[ObservableProperty]
	private ImageSource? displayImage;

	[ObservableProperty]
	private bool isChromeVisible = true;

	public bool HasPrevious => _currentIndex > 0;
	public bool HasNext => _currentIndex >= 0 && _currentIndex < _photos.Count - 1;

	public PhotoViewerViewModel(IPhotoLibraryService photoLibraryService)
	{
		_photoLibraryService = photoLibraryService;
	}

	public void SetPhoto(PhotoItem selectedPhoto)
	{
		SetPhotoContext(selectedPhoto, null);
	}

	public void SetPhotoContext(PhotoItem selectedPhoto, IReadOnlyList<PhotoItem>? contextPhotos)
	{
		if (contextPhotos == null || contextPhotos.Count == 0)
			_photos = new List<PhotoItem> { selectedPhoto };
		else
			_photos = contextPhotos;

		_currentIndex = FindIndex(_photos, selectedPhoto);
		if (_currentIndex < 0)
		{
			_photos = new List<PhotoItem> { selectedPhoto };
			_currentIndex = 0;
		}

		SetCurrentPhoto(_photos[_currentIndex]);
		UpdateNavigationState();
	}

	private void SetCurrentPhoto(PhotoItem selectedPhoto)
	{
		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();

		Photo = selectedPhoto;
		DisplayImage = selectedPhoto.FullImage ?? selectedPhoto.Thumbnail;

		if (selectedPhoto.FullImage != null || string.IsNullOrWhiteSpace(selectedPhoto.Id))
			return;

		_ = LoadFullImageAsync(selectedPhoto, _loadCts.Token);
	}

	[RelayCommand(CanExecute = nameof(CanMovePrevious))]
	private void PreviousPhoto()
	{
		if (!CanMovePrevious())
			return;

		_currentIndex--;
		SetCurrentPhoto(_photos[_currentIndex]);
		UpdateNavigationState();
	}

	[RelayCommand(CanExecute = nameof(CanMoveNext))]
	private void NextPhoto()
	{
		if (!CanMoveNext())
			return;

		_currentIndex++;
		SetCurrentPhoto(_photos[_currentIndex]);
		UpdateNavigationState();
	}

	private bool CanMovePrevious() => HasPrevious;

	private bool CanMoveNext() => HasNext;

	private void UpdateNavigationState()
	{
		OnPropertyChanged(nameof(HasPrevious));
		OnPropertyChanged(nameof(HasNext));
		PreviousPhotoCommand.NotifyCanExecuteChanged();
		NextPhotoCommand.NotifyCanExecuteChanged();
	}

	private static int FindIndex(IReadOnlyList<PhotoItem> photos, PhotoItem selectedPhoto)
	{
		if (photos.Count == 0)
			return -1;

		for (var i = 0; i < photos.Count; i++)
		{
			var candidate = photos[i];
			if (ReferenceEquals(candidate, selectedPhoto))
				return i;

			if (!string.IsNullOrWhiteSpace(selectedPhoto.Id) && candidate.Id == selectedPhoto.Id)
				return i;
		}

		return -1;
	}

	private async Task LoadFullImageAsync(PhotoItem selectedPhoto, CancellationToken ct)
	{
		try
		{
			var fullImage = await _photoLibraryService.GetFullImageAsync(selectedPhoto, ct);
			if (fullImage == null || ct.IsCancellationRequested)
				return;

			selectedPhoto.FullImage = fullImage;
			if (ReferenceEquals(Photo, selectedPhoto))
				DisplayImage = fullImage;
		}
		catch (OperationCanceledException)
		{
		}
	}

	[RelayCommand]
	private Task GoBackAsync()
	{
		return Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private void ToggleChrome()
	{
		IsChromeVisible = !IsChromeVisible;
	}
}
