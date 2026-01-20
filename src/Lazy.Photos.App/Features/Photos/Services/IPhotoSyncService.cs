using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoSyncService
{
	Task<IReadOnlyList<PhotoItem>> GetRecentAsync(int maxCount, CancellationToken ct);
}
