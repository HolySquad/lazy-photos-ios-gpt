using Lazy.Photos.App.Models;

namespace Lazy.Photos.App.Services;

public interface IPhotoLibraryService
{
	Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct);
}
