using CommunityToolkit.Mvvm.ComponentModel;

namespace Lazy.Photos.App.Features.Sync.Models;

/// <summary>
/// Represents the current state of a sync operation.
/// Observable for data binding in UI.
/// </summary>
public sealed partial class SyncState : ObservableObject
{
	[ObservableProperty]
	private SyncStatus _status = SyncStatus.Idle;

	[ObservableProperty]
	private int _totalItems;

	[ObservableProperty]
	private int _completedItems;

	[ObservableProperty]
	private int _failedItems;

	[ObservableProperty]
	private string? _currentItemName;

	[ObservableProperty]
	private double _progressPercentage;

	[ObservableProperty]
	private long _currentFileUploadedBytes;

	[ObservableProperty]
	private long _currentFileTotalBytes;

	[ObservableProperty]
	private double _currentFileProgressPercentage;

	[ObservableProperty]
	private string? _errorMessage;

	[ObservableProperty]
	private DateTimeOffset? _startedAt;

	[ObservableProperty]
	private DateTimeOffset? _completedAt;

	[ObservableProperty]
	private int _activeUploads;

	[ObservableProperty]
	private int _parallelUploadCount = 2;

	[ObservableProperty]
	private long _totalBytesUploaded;

	[ObservableProperty]
	private long _totalBytesToUpload;

	/// <summary>
	/// Gets the number of items still pending upload.
	/// </summary>
	public int PendingItems => TotalItems - CompletedItems - FailedItems;

	/// <summary>
	/// Updates the progress percentage based on completed/total items.
	/// Includes partial progress from all active uploads for smoother display.
	/// </summary>
	public void UpdateProgress()
	{
		if (TotalItems > 0)
		{
			ProgressPercentage = (double)CompletedItems / TotalItems;

			// Add fractional progress from active uploads
			if (TotalBytesToUpload > 0)
			{
				var activeProgress = (double)TotalBytesUploaded / TotalBytesToUpload;
				var activeItemCount = activeProgress > 0 ? Math.Min(activeProgress, ActiveUploads) : 0;
				ProgressPercentage = Math.Min(1.0, (CompletedItems + activeItemCount) / TotalItems);
			}
		}
		else
		{
			ProgressPercentage = 0;
		}
	}
}
