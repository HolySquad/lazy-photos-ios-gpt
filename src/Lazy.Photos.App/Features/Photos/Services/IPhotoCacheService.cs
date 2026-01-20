using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoCacheService
{
	Task<IReadOnlyList<PhotoItem>> GetCachedPhotosAsync(CancellationToken ct);
	Task SavePhotosAsync(IReadOnlyList<PhotoItem> photos, CancellationToken ct);
}
