using Lazy.Photos.App.Features.Photos;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public sealed class PhotoNavigationService : IPhotoNavigationService
{
	public Task ShowPhotoAsync(PhotoItem photo)
	{
		return Shell.Current.GoToAsync(nameof(PhotoViewerPage), true, new Dictionary<string, object>
		{
			{ "photo", photo }
		});
	}

	public Task ShowPhotoAsync(PhotoItem photo, IReadOnlyList<PhotoItem> contextPhotos)
	{
		return Shell.Current.GoToAsync(nameof(PhotoViewerPage), true, new Dictionary<string, object>
		{
			{ "photo", photo },
			{ "photos", contextPhotos }
		});
	}
}
