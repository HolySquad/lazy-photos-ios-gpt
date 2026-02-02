using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Service for managing thumbnail generation and quality tracking.
/// Single Responsibility: Thumbnail generation, prioritization, and quality management.
/// </summary>
public interface IThumbnailService
{
	/// <summary>
	/// Starts background thumbnail generation for the given photos.
	/// </summary>
	Task StartThumbnailFillAsync(
		IEnumerable<PhotoItem> photos,
		int visibleStartIndex,
		int visibleEndIndex,
		Func<bool> isScrollingProvider,
		CancellationToken ct);

	/// <summary>
	/// Upgrades visible thumbnails from low quality to high quality when scrolling stops.
	/// </summary>
	Task UpgradeVisibleThumbnailsAsync(
		IReadOnlyList<PhotoItem> photos,
		int visibleStartIndex,
		int visibleEndIndex,
		CancellationToken ct);

	/// <summary>
	/// Tracks that a photo has a low-quality thumbnail.
	/// </summary>
	void TrackLowQualityThumbnail(string key);

	/// <summary>
	/// Removes tracking for a photo's low-quality thumbnail.
	/// </summary>
	void RemoveLowQualityTracking(string key);

	/// <summary>
	/// Checks if a photo has a low-quality thumbnail.
	/// </summary>
	bool HasLowQualityThumbnail(string key);

	/// <summary>
	/// Cancels any pending thumbnail operations.
	/// </summary>
	void CancelPendingOperations();
}
