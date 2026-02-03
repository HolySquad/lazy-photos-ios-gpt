namespace Lazy.Photos.App.Features.Sync.Models;

/// <summary>
/// Represents the current status of a sync operation.
/// </summary>
public enum SyncStatus
{
	/// <summary>No sync operation is currently running.</summary>
	Idle,

	/// <summary>Sync is preparing (scanning for photos to upload).</summary>
	Preparing,

	/// <summary>Sync is actively running.</summary>
	Running,

	/// <summary>Sync has been paused by the user.</summary>
	Paused,

	/// <summary>Sync is being cancelled.</summary>
	Cancelling,

	/// <summary>Sync was cancelled by the user.</summary>
	Cancelled,

	/// <summary>Sync completed successfully.</summary>
	Completed,

	/// <summary>Sync encountered an error.</summary>
	Error
}
