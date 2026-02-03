using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Cross-platform photo library service with device-adaptive thumbnail generation.
/// </summary>
public partial class PhotoLibraryService : IPhotoLibraryService
{
	private readonly IDeviceProfileService _deviceProfileService;

	public PhotoLibraryService(IDeviceProfileService deviceProfileService)
	{
		_deviceProfileService = deviceProfileService;
	}

	protected DeviceProfile Profile => _deviceProfileService.GetProfile();

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

	public Task<Stream?> GetPhotoStreamAsync(PhotoItem photo, CancellationToken ct)
		=> GetPhotoStreamAsyncCore(photo, ct);

	public Task<long> GetPhotoSizeAsync(PhotoItem photo, CancellationToken ct)
		=> GetPhotoSizeAsyncCore(photo, ct);

	public Task<string> GetPhotoMimeTypeAsync(PhotoItem photo, CancellationToken ct)
		=> GetPhotoMimeTypeAsyncCore(photo, ct);

	public Task<(int Width, int Height)?> GetPhotoDimensionsAsync(PhotoItem photo, CancellationToken ct)
		=> GetPhotoDimensionsAsyncCore(photo, ct);

	private partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
	private partial IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsyncCore(int maxCount, CancellationToken ct);
	private partial Task<string?> ComputeHashAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<ImageSource?> BuildThumbnailAsyncCore(PhotoItem photo, bool lowQuality, CancellationToken ct);
	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<Stream?> GetPhotoStreamAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<long> GetPhotoSizeAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<string> GetPhotoMimeTypeAsyncCore(PhotoItem photo, CancellationToken ct);
	private partial Task<(int Width, int Height)?> GetPhotoDimensionsAsyncCore(PhotoItem photo, CancellationToken ct);
}
