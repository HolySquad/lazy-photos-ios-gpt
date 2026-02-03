using Lazy.Photos.App.Features.Sync.Models;

namespace Lazy.Photos.App.Features.Sync.Services;

/// <summary>
/// Persists sync state to survive app restarts.
/// </summary>
public interface ISyncStateRepository
{
	/// <summary>
	/// Saves the current sync state to persistent storage.
	/// </summary>
	Task SaveStateAsync(SyncState state, CancellationToken ct);

	/// <summary>
	/// Loads the persisted sync state.
	/// </summary>
	Task<SyncState?> LoadStateAsync(CancellationToken ct);

	/// <summary>
	/// Clears the persisted sync state.
	/// </summary>
	Task ClearStateAsync(CancellationToken ct);
}
