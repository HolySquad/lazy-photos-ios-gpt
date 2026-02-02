using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of photo state management.
/// Single Responsibility: Managing the indexed collection and ordered list of photos.
/// </summary>
public sealed class PhotoStateRepository : IPhotoStateRepository
{
	private readonly object _indexLock = new();
	private readonly Dictionary<string, PhotoItem> _photoIndex = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<PhotoItem> _orderedPhotos = new();
	private int _displayCount;

	public int DisplayCount
	{
		get
		{
			lock (_indexLock)
				return _displayCount;
		}
	}

	public int TotalCount
	{
		get
		{
			lock (_indexLock)
				return _orderedPhotos.Count;
		}
	}

	public void InitializeFromCache(IReadOnlyList<PhotoItem> items)
	{
		lock (_indexLock)
		{
			_photoIndex.Clear();
			_orderedPhotos.Clear();

			foreach (var item in items)
			{
				var key = EnsureKeyInternal(item);
				if (_photoIndex.ContainsKey(key))
					continue;

				_photoIndex[key] = item;
				_orderedPhotos.Add(item);
			}
		}
	}

	public IReadOnlyList<PhotoItem> MergeAndSort(params IEnumerable<PhotoItem>[] newItemSets)
	{
		var added = false;
		List<PhotoItem>? mergedSnapshot;

		lock (_indexLock)
		{
			foreach (var set in newItemSets)
			{
				foreach (var item in set)
				{
					var key = EnsureKeyInternal(item);
					if (_photoIndex.ContainsKey(key))
						continue;

					_photoIndex[key] = item;
					added = true;
				}
			}

			if (!added && _orderedPhotos.Count > 0)
				return _orderedPhotos;

			mergedSnapshot = _photoIndex.Values.ToList();
		}

		mergedSnapshot = mergedSnapshot
			.OrderByDescending(p => p.TakenAt ?? DateTimeOffset.MinValue)
			.ThenByDescending(p => p.DisplayName)
			.ToList();

		lock (_indexLock)
		{
			_orderedPhotos.Clear();
			_orderedPhotos.AddRange(mergedSnapshot);
		}

		return mergedSnapshot;
	}

	public List<PhotoItem> BuildDisplaySnapshot()
	{
		lock (_indexLock)
		{
			var count = Math.Min(_displayCount, _orderedPhotos.Count);
			var displayItems = new List<PhotoItem>(count);
			for (var i = 0; i < count; i++)
				displayItems.Add(_orderedPhotos[i]);
			return displayItems;
		}
	}

	public List<PhotoItem> BuildOrderedSnapshot()
	{
		lock (_indexLock)
			return _orderedPhotos.ToList();
	}

	public void SetDisplayCount(int count)
	{
		lock (_indexLock)
			_displayCount = count;
	}

	public void EnsureDisplayCountInitialized(int totalCount, int pageSize)
	{
		lock (_indexLock)
		{
			if (_displayCount == 0 && totalCount > 0)
				_displayCount = Math.Min(pageSize, totalCount);
		}
	}

	public bool TryIncrementDisplayCount(int pageSize)
	{
		lock (_indexLock)
		{
			var targetCount = _displayCount + pageSize;
			var nextCount = Math.Min(targetCount, _orderedPhotos.Count);

			if (nextCount <= _displayCount)
				return false;

			_displayCount = nextCount;
			return true;
		}
	}

	public void Reset()
	{
		lock (_indexLock)
		{
			_displayCount = 0;
			// Note: We don't clear the index/ordered list here as that's done during InitializeFromCache
		}
	}

	public string EnsureKey(PhotoItem item)
	{
		lock (_indexLock)
			return EnsureKeyInternal(item);
	}

	public void UpdateKeyForHashedPhoto(PhotoItem photo)
	{
		var newKey = ComputeKey(photo);
		if (string.Equals(photo.UniqueKey, newKey, StringComparison.OrdinalIgnoreCase))
			return;

		var oldKey = photo.UniqueKey;
		photo.UniqueKey = newKey;

		lock (_indexLock)
		{
			if (!string.IsNullOrWhiteSpace(oldKey) &&
				_photoIndex.TryGetValue(oldKey, out var existing) &&
				ReferenceEquals(existing, photo))
			{
				_photoIndex.Remove(oldKey);
			}

			if (!_photoIndex.ContainsKey(newKey))
				_photoIndex[newKey] = photo;
		}
	}

	private string EnsureKeyInternal(PhotoItem item)
	{
		if (!string.IsNullOrWhiteSpace(item.UniqueKey))
			return item.UniqueKey;

		var key = ComputeKey(item);
		item.UniqueKey = key;
		return key;
	}

	private static string ComputeKey(PhotoItem item)
	{
		if (!string.IsNullOrWhiteSpace(item.Hash))
			return $"hash:{item.Hash}";

		if (!string.IsNullOrWhiteSpace(item.Id))
			return $"id:{item.Id}";

		return $"fallback:{item.DisplayName}:{item.TakenAt?.UtcDateTime:o}";
	}
}
