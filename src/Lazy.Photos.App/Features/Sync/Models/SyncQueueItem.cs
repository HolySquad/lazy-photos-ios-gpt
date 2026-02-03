namespace Lazy.Photos.App.Features.Sync.Models;

/// <summary>
/// Represents a photo in the upload queue.
/// </summary>
public sealed class SyncQueueItem
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string LocalPhotoId { get; set; } = string.Empty;
	public string? Hash { get; set; }
	public string LocalPath { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public string MimeType { get; set; } = "image/jpeg";
	public DateTimeOffset? CapturedAt { get; set; }
	public int? Width { get; set; }
	public int? Height { get; set; }
	public double? LocationLat { get; set; }
	public double? LocationLon { get; set; }
	public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
	public int RetryCount { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? LastAttemptAt { get; set; }
}

/// <summary>
/// Represents the status of a queue item.
/// </summary>
public enum QueueItemStatus
{
	/// <summary>Item is waiting to be processed.</summary>
	Pending,

	/// <summary>Hash is being computed for the item.</summary>
	Hashing,

	/// <summary>Item is currently being uploaded.</summary>
	Uploading,

	/// <summary>Item was successfully uploaded.</summary>
	Uploaded,

	/// <summary>Item upload failed after retries.</summary>
	Failed,

	/// <summary>Item was skipped (e.g., already exists on server).</summary>
	Skipped,

	/// <summary>Item upload was cancelled by user.</summary>
	Cancelled
}
