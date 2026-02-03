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

	// Upload support methods
	Task<Stream?> GetPhotoStreamAsync(PhotoItem photo, CancellationToken ct);
	Task<long> GetPhotoSizeAsync(PhotoItem photo, CancellationToken ct);
	Task<string> GetPhotoMimeTypeAsync(PhotoItem photo, CancellationToken ct);
	Task<(int Width, int Height)?> GetPhotoDimensionsAsync(PhotoItem photo, CancellationToken ct);
}
