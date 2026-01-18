using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class PhotoLibraryService : IPhotoLibraryService
{
	public Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct)
	{
		return GetRecentPhotosAsyncCore(maxCount, ct);
	}

	public Task<ImageSource?> GetFullImageAsync(PhotoItem photo, CancellationToken ct)
	{
		return GetFullImageAsyncCore(photo, ct);
	}

	private partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct);
}
