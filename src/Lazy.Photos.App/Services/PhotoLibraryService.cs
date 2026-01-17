using Lazy.Photos.App.Models;

namespace Lazy.Photos.App.Services;

public partial class PhotoLibraryService : IPhotoLibraryService
{
	public Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct)
	{
		return GetRecentPhotosAsyncCore(maxCount, ct);
	}

	private partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
}
