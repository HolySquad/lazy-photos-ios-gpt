using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoLibraryService
{
	Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct);
	IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsync(int maxCount, CancellationToken ct);
	Task<string?> ComputeHashAsync(PhotoItem photo, CancellationToken ct);
	Task<ImageSource?> BuildThumbnailAsync(PhotoItem photo, CancellationToken ct);
	Task<ImageSource?> BuildThumbnailAsync(PhotoItem photo, bool lowQuality, CancellationToken ct);
	Task<ImageSource?> GetFullImageAsync(PhotoItem photo, CancellationToken ct);
}
