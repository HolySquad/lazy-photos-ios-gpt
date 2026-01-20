using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class PhotoLibraryService : IPhotoLibraryService
{
	public Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsync(int maxCount, CancellationToken ct)
	{
		return GetRecentPhotosAsyncCore(maxCount, ct);
	}

	public IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsync(int maxCount, CancellationToken ct)
	{
		return StreamRecentPhotosAsyncCore(maxCount, ct);
	}

	public Task<string?> ComputeHashAsync(PhotoItem photo, CancellationToken ct)
	{
		return ComputeHashAsyncCore(photo, ct);
	}

	public Task<ImageSource?> BuildThumbnailAsync(PhotoItem photo, CancellationToken ct)
	{
		return BuildThumbnailAsync(photo, false, ct);
	}

	public Task<ImageSource?> BuildThumbnailAsync(PhotoItem photo, bool lowQuality, CancellationToken ct)
	{
		return BuildThumbnailAsyncCore(photo, lowQuality, ct);
	}

	public Task<ImageSource?> GetFullImageAsync(PhotoItem photo, CancellationToken ct)
	{
		return GetFullImageAsyncCore(photo, ct);
	}

	private partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
	private partial IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
	private partial Task<string?> ComputeHashAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<ImageSource?> BuildThumbnailAsyncCore(PhotoItem photo, bool lowQuality, CancellationToken ct);
	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct);
}
