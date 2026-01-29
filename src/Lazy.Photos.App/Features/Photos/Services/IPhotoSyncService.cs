using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoSyncService
{
	Task<PhotoPage> GetPageAsync(string? cursor, int limit, CancellationToken ct);
	Task<IReadOnlyList<PhotoItem>> GetRecentAsync(int maxCount, CancellationToken ct);
}

public sealed record PhotoPage(IReadOnlyList<PhotoItem> Items, string? Cursor, bool HasMore);
