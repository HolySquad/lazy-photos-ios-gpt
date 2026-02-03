using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoNavigationService
{
	Task ShowPhotoAsync(PhotoItem photo);
	Task ShowPhotoAsync(PhotoItem photo, IReadOnlyList<PhotoItem> contextPhotos);
}
