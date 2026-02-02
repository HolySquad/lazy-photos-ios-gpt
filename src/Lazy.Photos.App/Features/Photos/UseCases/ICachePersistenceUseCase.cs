using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.UseCases;

/// <summary>
/// Use case for persisting photos to cache with debouncing.
/// Single Responsibility: Managing cache persistence with proper debouncing.
/// </summary>
public interface ICachePersistenceUseCase
{
	/// <summary>
	/// Queues photos for cache persistence with debouncing.
	/// </summary>
	void QueuePersist(IReadOnlyList<PhotoItem> photos, CancellationToken ct);

	/// <summary>
	/// Cancels any pending persistence operations.
	/// </summary>
	void CancelPending();
}
