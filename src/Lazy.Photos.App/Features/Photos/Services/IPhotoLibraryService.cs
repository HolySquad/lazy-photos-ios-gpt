using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoLibraryService
{
	Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct);
	Task<ImageSource?> GetFullImageAsync(PhotoItem photo, CancellationToken ct);
}
