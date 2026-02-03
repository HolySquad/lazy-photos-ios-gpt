using Lazy.Photos.App.Features.Sync.Models;

namespace Lazy.Photos.App.Features.Sync.Services;

/// <summary>
/// Manages the upload queue with persistence and retry logic.
/// Single Responsibility: Queue operations and database persistence.
/// </summary>
public interface IUploadQueueService
{
	/// <summary>
	/// Adds items to the upload queue.
	/// </summary>
	Task EnqueueItemsAsync(IReadOnlyList<SyncQueueItem> items, CancellationToken ct);

	/// <summary>
	/// Gets all pending items from the queue.
	/// </summary>
	Task<IReadOnlyList<SyncQueueItem>> GetPendingItemsAsync(CancellationToken ct);

	/// <summary>
	/// Updates the status of a queue item.
	/// </summary>
	Task UpdateItemStatusAsync(string itemId, QueueItemStatus status, string? errorMessage, CancellationToken ct);

	/// <summary>
	/// Marks an item as uploaded.
	/// </summary>
	Task MarkAsUploadedAsync(string itemId, CancellationToken ct);

	/// <summary>
	/// Removes all completed/failed items from the queue.
	/// </summary>
	Task ClearCompletedItemsAsync(CancellationToken ct);

	/// <summary>
	/// Gets queue statistics.
	/// </summary>
	Task<QueueStatistics> GetStatisticsAsync(CancellationToken ct);
}

/// <summary>
/// Statistics about the upload queue.
/// </summary>
public sealed record QueueStatistics(
	int Total,
	int Pending,
	int Uploading,
	int Completed,
	int Failed);
