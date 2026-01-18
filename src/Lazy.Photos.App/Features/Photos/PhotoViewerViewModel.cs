using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Photos;

public partial class PhotoViewerViewModel : ObservableObject
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private CancellationTokenSource? _loadCts;

	[ObservableProperty]
	private PhotoItem? photo;

	[ObservableProperty]
	private ImageSource? displayImage;

	public PhotoViewerViewModel(IPhotoLibraryService photoLibraryService)
	{
		_photoLibraryService = photoLibraryService;
	}

	public PhotoViewerViewModel()
		: this(new PhotoLibraryService())
	{
	}

	public void SetPhoto(PhotoItem selectedPhoto)
	{
		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();

		Photo = selectedPhoto;
		DisplayImage = selectedPhoto.FullImage ?? selectedPhoto.Thumbnail;

		if (selectedPhoto.FullImage != null || string.IsNullOrWhiteSpace(selectedPhoto.Id))
			return;

		_ = LoadFullImageAsync(selectedPhoto, _loadCts.Token);
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
}
