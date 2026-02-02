using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Manager for handling pagination state for both remote and device photos.
/// Single Responsibility: Managing pagination cursors, states, and device enumeration.
/// </summary>
public interface IPaginationManager
{
	/// <summary>
	/// Gets whether remote has more pages to load.
	/// </summary>
	bool RemoteHasMore { get; }

	/// <summary>
	/// Gets whether device enumeration is complete.
	/// </summary>
	bool DeviceEnumerationComplete { get; }

	/// <summary>
	/// Gets whether device access has been granted.
	/// </summary>
	bool DeviceAccessGranted { get; }

	/// <summary>
	/// Gets the current remote cursor.
	/// </summary>
	string? RemoteCursor { get; }

	/// <summary>
	/// Gets the page size used for pagination.
	/// </summary>
	int PageSize { get; }

	/// <summary>
	/// Gets the scroll load threshold (how close to end before loading next page).
	/// </summary>
	int ScrollLoadThreshold { get; }

	/// <summary>
	/// Sets the device access granted state.
	/// </summary>
	void SetDeviceAccessGranted(bool granted);

	/// <summary>
	/// Updates pagination state from a remote page response.
	/// </summary>
	void UpdateFromRemotePage(PhotoPage page);

	/// <summary>
	/// Fetches the next page of device photos.
	/// </summary>
	Task<List<PhotoItem>> FetchDevicePageAsync(int pageSize, CancellationToken ct);

	/// <summary>
	/// Loads the next remote page.
	/// </summary>
	Task<PhotoPage> LoadRemotePageAsync(CancellationToken ct);

	/// <summary>
	/// Checks if we should load more content based on current position.
	/// </summary>
	bool ShouldLoadMore(int lastVisibleIndex, int displayCount);

	/// <summary>
	/// Checks if more content is available to load.
	/// </summary>
	bool HasMoreContent();

	/// <summary>
	/// Resets the pagination state for a fresh load.
	/// </summary>
	Task ResetAsync();
}
