namespace Lazy.Photos.App.Features.Photos.Services;

public interface IPhotoCacheMaintenance
{
	Task<string> GetDatabaseSizeAsync(CancellationToken ct);
	Task ClearCacheAsync(CancellationToken ct);
}
