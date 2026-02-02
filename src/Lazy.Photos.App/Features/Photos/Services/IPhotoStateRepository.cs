using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Repository for managing photo state - the indexed collection and ordered list.
/// Single Responsibility: Photo state persistence and retrieval in memory.
/// </summary>
public interface IPhotoStateRepository
{
	/// <summary>
	/// Gets the current display count for pagination.
	/// </summary>
	int DisplayCount { get; }

	/// <summary>
	/// Gets the total count of ordered photos.
	/// </summary>
	int TotalCount { get; }

	/// <summary>
	/// Initializes the repository from a collection of cached photos.
	/// </summary>
	void InitializeFromCache(IReadOnlyList<PhotoItem> items);

	/// <summary>
	/// Merges new items into the collection and returns the merged result sorted by date.
	/// </summary>
	IReadOnlyList<PhotoItem> MergeAndSort(params IEnumerable<PhotoItem>[] newItemSets);

	/// <summary>
	/// Builds a snapshot of items currently displayed based on display count.
	/// </summary>
	List<PhotoItem> BuildDisplaySnapshot();

	/// <summary>
	/// Builds a snapshot of all ordered photos.
	/// </summary>
	List<PhotoItem> BuildOrderedSnapshot();

	/// <summary>
	/// Sets the display count for pagination.
	/// </summary>
	void SetDisplayCount(int count);

	/// <summary>
	/// Ensures display count is initialized if it's zero.
	/// </summary>
	void EnsureDisplayCountInitialized(int totalCount, int pageSize);

	/// <summary>
	/// Increments display count by a page size, up to the total count.
	/// </summary>
	/// <returns>True if display count was increased.</returns>
	bool TryIncrementDisplayCount(int pageSize);

	/// <summary>
	/// Resets the repository state for a fresh load.
	/// </summary>
	void Reset();

	/// <summary>
	/// Ensures a photo has a unique key and returns it.
	/// </summary>
	string EnsureKey(PhotoItem item);

	/// <summary>
	/// Updates the key for a photo after its hash has been computed.
	/// </summary>
	void UpdateKeyForHashedPhoto(PhotoItem photo);
}
