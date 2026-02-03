using Lazy.Photos.App.Features.Sync.Models;

namespace Lazy.Photos.App.Features.Sync.Services;

/// <summary>
/// Orchestrates photo sync operations with pause/resume capability.
/// Single Responsibility: Manage sync lifecycle and coordinate upload queue.
/// </summary>
public interface ISyncOrchestrationService
{
	/// <summary>
	/// Gets the current sync state (observable).
	/// </summary>
	SyncState CurrentState { get; }

	/// <summary>
	/// Starts a new sync operation from device to server.
	/// </summary>
	Task StartSyncAsync(CancellationToken ct);

	/// <summary>
	/// Pauses the current sync operation.
	/// </summary>
	Task PauseSyncAsync();

	/// <summary>
	/// Resumes a paused sync operation.
	/// </summary>
	Task ResumeSyncAsync(CancellationToken ct);

	/// <summary>
	/// Cancels the current sync operation.
	/// </summary>
	Task CancelSyncAsync();

	/// <summary>
	/// Gets whether sync can be started.
	/// </summary>
	bool CanStart { get; }

	/// <summary>
	/// Gets whether sync can be paused.
	/// </summary>
	bool CanPause { get; }

	/// <summary>
	/// Gets whether sync can be resumed.
	/// </summary>
	bool CanResume { get; }

	/// <summary>
	/// Gets whether sync can be cancelled.
	/// </summary>
	bool CanCancel { get; }
}
