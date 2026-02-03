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
	private string? _errorMessage;

	[ObservableProperty]
	private DateTimeOffset? _startedAt;

	[ObservableProperty]
	private DateTimeOffset? _completedAt;

	/// <summary>
	/// Gets the number of items still pending upload.
	/// </summary>
	public int PendingItems => TotalItems - CompletedItems - FailedItems;

	/// <summary>
	/// Updates the progress percentage based on completed/total items.
	/// </summary>
	public void UpdateProgress()
	{
		if (TotalItems > 0)
		{
			ProgressPercentage = (double)CompletedItems / TotalItems;
		}
		else
		{
			ProgressPercentage = 0;
		}
	}
}
