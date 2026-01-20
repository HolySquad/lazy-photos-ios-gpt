using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;
using System.Linq;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using Lazy.Photos.App.Features.Photos.Permissions;
#endif

namespace Lazy.Photos.App.Features.Photos;

public partial class MainPageViewModel : ObservableObject
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoNavigationService _photoNavigationService;
	private readonly IPhotoSyncService _photoSyncService;
	private readonly IPhotoCacheService _photoCacheService;
	private readonly object _indexLock = new();
	private CancellationTokenSource? _loadCts;
	private CancellationTokenSource? _scrollQuietCts;
	private CancellationTokenSource? _sectionDebounceCts;
	private CancellationTokenSource? _persistDebounceCts;
	private CancellationTokenSource? _thumbnailFillCts;
	private readonly SemaphoreSlim _thumbnailSemaphore = new(1);
	private readonly Dictionary<string, PhotoItem> _photoIndex = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<PhotoItem> _orderedPhotos = new();
	private bool _loadedOnce;
	private volatile bool _isScrolling;
	private volatile int _visibleStartIndex;
	private volatile int _visibleEndIndex;
	private readonly ConcurrentDictionary<string, byte> _lowQualityThumbs = new(StringComparer.OrdinalIgnoreCase);
	private const int DevicePageSize = 200;
	private const int RemoteLoadLimit = 120;
	private const int SectionDebounceMs = 350;
	private const int PersistDebounceMs = 750;

	[ObservableProperty]
	private ObservableCollection<PhotoItem> photos = new();

	[ObservableProperty]
	private ObservableCollection<PhotoSection> photoSections = new();

	[ObservableProperty]
	private bool isBusy;

	[ObservableProperty]
	private string? errorMessage;

	[ObservableProperty]
	private bool hasError;

	[ObservableProperty]
	private bool isInitializing;

	[ObservableProperty]
	private int columns = 3;

	[ObservableProperty]
	private double cellSize = 120;

	public MainPageViewModel(
		IPhotoLibraryService photoLibraryService,
		IPhotoNavigationService photoNavigationService,
		IPhotoSyncService photoSyncService,
		IPhotoCacheService photoCacheService)
	{
		_photoLibraryService = photoLibraryService;
		_photoNavigationService = photoNavigationService;
		_photoSyncService = photoSyncService;
		_photoCacheService = photoCacheService;
	}

	public void NotifyScrolled(int firstVisibleIndex, int lastVisibleIndex)
	{
		_isScrolling = true;
		_visibleStartIndex = Math.Max(0, firstVisibleIndex);
		_visibleEndIndex = Math.Max(_visibleStartIndex, lastVisibleIndex);
		_scrollQuietCts?.Cancel();
		_scrollQuietCts = new CancellationTokenSource();
		_ = ResetScrollQuietAsync(_scrollQuietCts.Token);
	}

	private async Task ResetScrollQuietAsync(CancellationToken ct)
	{
		try
		{
			await Task.Delay(250, ct).ConfigureAwait(false);
			_isScrolling = false;
			await UpgradeVisibleThumbnailsAsync(ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// Ignore; scrolling continued.
		}
	}

	partial void OnErrorMessageChanged(string? value)
	{
		HasError = !string.IsNullOrWhiteSpace(value);
	}

	public async Task EnsureLoadedAsync()
	{
		if (_loadedOnce)
			return;

		_loadedOnce = true;
		await RefreshAsync();
	}

	public void UpdateGrid(int newColumns, double newCellSize)
	{
		if (Columns != newColumns)
			Columns = newColumns;

		if (Math.Abs(CellSize - newCellSize) >= 0.5)
			CellSize = newCellSize;
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		if (IsBusy)
			return;

		IsBusy = true;
		IsInitializing = true;
		ErrorMessage = null;

		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();

		try
		{
			var cached = await _photoCacheService.GetCachedPhotosAsync(_loadCts.Token);

			await MainThread.InvokeOnMainThreadAsync(() => InitializeFromCache(cached));
			_ = StartThumbnailFillAsync(Photos, _loadCts.Token);

			var remoteTask = TryLoadRemoteAsync(_loadCts.Token);
			var deviceTask = LoadDevicePhotosAsync(_loadCts.Token);
			await Task.WhenAll(remoteTask, deviceTask);

			var remoteItems = remoteTask.Result;
			var deviceItems = deviceTask.Result;
			var merged = MergeAndSort(remoteItems, deviceItems);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ApplySnapshot(merged);
				RebuildSectionsFromOrdered();
			});

			QueuePersistCache(merged, _loadCts.Token);
			_ = StartThumbnailFillAsync(merged, _loadCts.Token);
			IsInitializing = false;
		}
		catch (OperationCanceledException)
		{
			// Ignore cancellation.
		}
		catch (Exception ex)
		{
			ErrorMessage = $"Failed to load photos: {ex.Message}";
			IsInitializing = false;
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void RebuildSectionsFromOrdered()
	{
		PhotoSections.Clear();
		if (Photos.Count == 0)
			return;

		var currentDate = GetLocalDate(Photos[0].TakenAt);
		var currentSection = new PhotoSection(FormatSectionTitle(currentDate), Array.Empty<PhotoItem>());
		PhotoSections.Add(currentSection);

		foreach (var photo in Photos)
		{
			var photoDate = GetLocalDate(photo.TakenAt);
			if (photoDate != currentDate)
			{
				currentDate = photoDate;
				currentSection = new PhotoSection(FormatSectionTitle(currentDate), Array.Empty<PhotoItem>());
				PhotoSections.Add(currentSection);
			}
			currentSection.Add(photo);
		}
	}

	[RelayCommand]
	private async Task OpenPhotoAsync(PhotoItem? photo)
	{
		if (photo == null)
			return;

		await _photoNavigationService.ShowPhotoAsync(photo);
	}

	private static async Task<bool> EnsurePhotosPermissionAsync()
	{
#if ANDROID
		PermissionStatus status;
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			status = await MauiPermissions.CheckStatusAsync<ReadMediaImagesPermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadMediaImagesPermission>();
		}
		else
		{
			status = await MauiPermissions.CheckStatusAsync<ReadExternalStoragePermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadExternalStoragePermission>();
		}
#else
		var status = await MauiPermissions.CheckStatusAsync<MauiPermissions.Photos>();
		if (status != PermissionStatus.Granted)
			status = await MauiPermissions.RequestAsync<MauiPermissions.Photos>();
#endif
		return status == PermissionStatus.Granted;
	}

	private async Task<IReadOnlyList<PhotoItem>> TryLoadRemoteAsync(CancellationToken ct)
	{
		try
		{
			return await _photoSyncService.GetRecentAsync(RemoteLoadLimit, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return Array.Empty<PhotoItem>();
		}
	}

	private async Task<IReadOnlyList<PhotoItem>> LoadDevicePhotosAsync(CancellationToken ct)
	{
		var permissionGranted = await EnsurePhotosPermissionAsync();
		if (!permissionGranted)
		{
			ErrorMessage = "Showing cloud photos. Photo permission is required to show device photos.";
			return Array.Empty<PhotoItem>();
		}

		var collected = new List<PhotoItem>();

		await Task.Run(async () =>
		{
			var page = new List<PhotoItem>(DevicePageSize);
			await foreach (var item in _photoLibraryService.StreamRecentPhotosAsync(int.MaxValue, ct).WithCancellation(ct).ConfigureAwait(false))
			{
				collected.Add(item);
				page.Add(item);

				if (page.Count >= DevicePageSize)
				{
					var snapshot = page;
					page = new List<PhotoItem>(DevicePageSize);
					await ApplyDevicePageAsync(snapshot, ct).ConfigureAwait(false);
				}
			}

			if (page.Count > 0)
				await ApplyDevicePageAsync(page, ct).ConfigureAwait(false);
		}, ct).ConfigureAwait(false);

		return collected;
	}

	private async Task ApplyDevicePageAsync(IReadOnlyList<PhotoItem> page, CancellationToken ct)
	{
		if (page.Count == 0 || ct.IsCancellationRequested)
			return;

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (ct.IsCancellationRequested)
				return;

			var added = AppendDevicePage(page);
			if (added > 0)
				QueueSectionRebuild();
		});
	}

	private Task StartHashFillAsync(IEnumerable<PhotoItem> photosToHash, CancellationToken ct)
	{
		var snapshot = photosToHash is List<PhotoItem> list ? list : photosToHash.ToList();
		return Task.Run(async () =>
		{
			foreach (var photo in snapshot)
			{
				if (!string.IsNullOrWhiteSpace(photo.Hash))
					continue;

				if (ct.IsCancellationRequested)
					break;

				try
				{
					var hash = await _photoLibraryService.ComputeHashAsync(photo, ct).ConfigureAwait(false);
					if (!string.IsNullOrWhiteSpace(hash))
					{
						photo.Hash = hash;
						UpdateKeyForHashedPhoto(photo);
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch
				{
					// Ignore hash failures to keep UI responsive.
				}
			}
		}, ct);
	}

	private Task StartThumbnailFillAsync(IEnumerable<PhotoItem> photosToFill, CancellationToken ct)
	{
		_thumbnailFillCts?.Cancel();
		_thumbnailFillCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var linkedCt = _thumbnailFillCts.Token;
		var snapshot = photosToFill is List<PhotoItem> list ? list : photosToFill.ToList();

		return Task.Run(async () =>
		{
			var allItems = snapshot;
			var missing = new List<PhotoItem>();
			for (var i = 0; i < allItems.Count; i++)
			{
				var item = allItems[i];
				if (item.Thumbnail == null)
					missing.Add(item);
			}

			if (missing.Count == 0)
			{
				QueuePersistCache(allItems, linkedCt);
				return;
			}

			var prioritized = BuildPriorityList(missing);
			var firstCount = Math.Min(30, prioritized.Count);
			var firstScreen = prioritized.GetRange(0, firstCount);
			var remaining = firstCount < prioritized.Count
				? prioritized.GetRange(firstCount, prioritized.Count - firstCount)
				: new List<PhotoItem>();

			await ProcessThumbnailBatchAsync(firstScreen, linkedCt).ConfigureAwait(false);
			await Task.Delay(50, linkedCt).ConfigureAwait(false);
			await ProcessThumbnailBatchAsync(remaining, linkedCt).ConfigureAwait(false);
			QueuePersistCache(allItems, linkedCt);
		}, linkedCt);
	}

	private List<PhotoItem> BuildPriorityList(List<PhotoItem> all)
	{
		if (all.Count == 0)
			return all;

		const int window = 24;
		const int lead = 8;
		var start = Math.Max(0, _visibleStartIndex - lead);
		var end = Math.Min(all.Count, start + window);

		var prioritized = new List<PhotoItem>(all.Count);
		var seen = new HashSet<PhotoItem>();

		for (var i = start; i < end; i++)
		{
			var item = all[i];
			prioritized.Add(item);
			seen.Add(item);
		}

		for (var i = 0; i < all.Count; i++)
		{
			var item = all[i];
			if (!seen.Contains(item))
				prioritized.Add(item);
		}

		return prioritized;
	}

	private async Task ProcessThumbnailBatchAsync(IEnumerable<PhotoItem> batch, CancellationToken ct)
	{
		const int chunkSize = 6;
		const int applyBatchSize = 10;
		int processedInChunk = 0;
		var pendingApply = new List<(PhotoItem photo, ImageSource thumb)>();

		foreach (var photo in batch)
		{
			if (ct.IsCancellationRequested)
				break;

			while (_isScrolling && !ct.IsCancellationRequested)
				await Task.Delay(50, ct).ConfigureAwait(false);

			try
			{
				await _thumbnailSemaphore.WaitAsync(ct).ConfigureAwait(false);
				if (ShouldThrottleMemory())
				{
					_thumbnailSemaphore.Release();
					await Task.Delay(100, ct).ConfigureAwait(false);
					continue;
				}

				var isLowQuality = _isScrolling;
				var thumb = await _photoLibraryService.BuildThumbnailAsync(photo, isLowQuality, ct).ConfigureAwait(false);
				if (thumb != null)
				{
					var key = EnsureKey(photo);
					if (isLowQuality)
						_lowQualityThumbs[key] = 0;
					else
						_lowQualityThumbs.TryRemove(key, out _);

					pendingApply.Add((photo, thumb));
					if (pendingApply.Count >= applyBatchSize)
					{
						await MainThread.InvokeOnMainThreadAsync(() =>
						{
							foreach (var (p, t) in pendingApply)
								p.Thumbnail = t;
							pendingApply.Clear();
						});
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
				// Ignore thumbnail failures.
			}
			finally
			{
				_thumbnailSemaphore.Release();
			}

			processedInChunk++;
			if (processedInChunk >= chunkSize)
			{
				processedInChunk = 0;
				await Task.Delay(50, ct).ConfigureAwait(false);
			}
		}

		if (pendingApply.Count > 0 && !ct.IsCancellationRequested)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				foreach (var (p, t) in pendingApply)
					p.Thumbnail = t;
				pendingApply.Clear();
			});
		}
	}

	private async Task UpgradeVisibleThumbnailsAsync(CancellationToken ct)
	{
		if (_lowQualityThumbs.IsEmpty || Photos.Count == 0)
			return;

		var visibleItems = new List<PhotoItem>();
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (Photos.Count == 0)
				return;

			var start = Math.Max(0, Math.Min(_visibleStartIndex, Photos.Count - 1));
			var end = Math.Max(start, Math.Min(_visibleEndIndex, Photos.Count - 1));
			for (var i = start; i <= end; i++)
				visibleItems.Add(Photos[i]);
		});

		if (visibleItems.Count == 0)
			return;

		foreach (var photo in visibleItems)
		{
			if (ct.IsCancellationRequested)
				break;

			var key = EnsureKey(photo);
			if (!_lowQualityThumbs.ContainsKey(key))
				continue;

			try
			{
				var thumb = await _photoLibraryService.BuildThumbnailAsync(photo, lowQuality: false, ct).ConfigureAwait(false);
				if (thumb != null)
				{
					await MainThread.InvokeOnMainThreadAsync(() => photo.Thumbnail = thumb);
					_lowQualityThumbs.TryRemove(key, out _);
				}
			}
			catch
			{
				// best-effort; continue
			}
		}
	}

	private void InitializeFromCache(IReadOnlyList<PhotoItem> items)
	{
		var ordered = new List<PhotoItem>(items.Count);
		lock (_indexLock)
		{
			_photoIndex.Clear();
			_orderedPhotos.Clear();

			foreach (var item in items)
			{
				var key = EnsureKey(item);
				if (_photoIndex.ContainsKey(key))
					continue;

				_photoIndex[key] = item;
				ordered.Add(item);
			}

			_orderedPhotos.AddRange(ordered);
		}

		Photos.Clear();
		for (var i = 0; i < ordered.Count; i++)
			Photos.Add(ordered[i]);

		RebuildSectionsFromOrdered();
	}

	private int AppendDevicePage(IReadOnlyList<PhotoItem> page)
	{
		var addedItems = new List<PhotoItem>();
		lock (_indexLock)
		{
			foreach (var item in page)
			{
				var key = EnsureKey(item);
				if (_photoIndex.ContainsKey(key))
					continue;

				_photoIndex[key] = item;
				_orderedPhotos.Add(item);
				addedItems.Add(item);
			}
		}

		for (var i = 0; i < addedItems.Count; i++)
			Photos.Add(addedItems[i]);

		return addedItems.Count;
	}

	private IReadOnlyList<PhotoItem> MergeAndSort(params IEnumerable<PhotoItem>[] newItemSets)
	{
		var added = false;
		List<PhotoItem>? mergedSnapshot = null;
		lock (_indexLock)
		{
			foreach (var set in newItemSets)
			{
				foreach (var item in set)
				{
					var key = EnsureKey(item);
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

	private string EnsureKey(PhotoItem item)
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

	private void UpdateKeyForHashedPhoto(PhotoItem photo)
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

	private void ApplySnapshot(IReadOnlyList<PhotoItem> items)
	{
		var targetCount = items.Count;
		var same = Photos.Count == targetCount;

		if (same)
		{
			for (var i = 0; i < targetCount; i++)
			{
				if (!ReferenceEquals(Photos[i], items[i]))
				{
					same = false;
					break;
				}
			}

			if (same)
				return;
		}

		while (Photos.Count < targetCount)
			Photos.Add(items[Photos.Count]);
		while (Photos.Count > targetCount)
			Photos.RemoveAt(Photos.Count - 1);

		for (var i = 0; i < targetCount; i++)
		{
			if (!ReferenceEquals(Photos[i], items[i]))
				Photos[i] = items[i];
		}
	}

	private async Task PersistCacheAsync(IReadOnlyList<PhotoItem> items, CancellationToken ct)
	{
		try
		{
			// Work off the UI thread with an immutable snapshot to avoid locking UI.
			var snapshot = new List<PhotoItem>(items.Count);
			for (var i = 0; i < items.Count; i++)
				snapshot.Add(CloneForCache(items[i]));

			await _photoCacheService.SavePhotosAsync(snapshot, ct).ConfigureAwait(false);
		}
		catch
		{
			// Best-effort cache persistence; ignore failures to keep UI responsive.
		}
	}

	private static PhotoItem CloneForCache(PhotoItem item)
	{
		return new PhotoItem
		{
			Id = item.Id,
			DisplayName = item.DisplayName,
			TakenAt = item.TakenAt,
			Hash = item.Hash,
			FolderName = item.FolderName,
			IsSynced = item.IsSynced,
			Thumbnail = item.Thumbnail,
			FullImage = item.FullImage
		};
	}

	private void QueueSectionRebuild()
	{
		_sectionDebounceCts?.Cancel();
		var debounceCts = new CancellationTokenSource();
		_sectionDebounceCts = debounceCts;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(SectionDebounceMs, debounceCts.Token).ConfigureAwait(false);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (!debounceCts.IsCancellationRequested)
						RebuildSectionsFromOrdered();
				});
			}
			catch (OperationCanceledException)
			{
				// Debounced.
			}
		}, CancellationToken.None);
	}

	private void QueuePersistCache(IReadOnlyList<PhotoItem> items, CancellationToken ct)
	{
		_persistDebounceCts?.Cancel();
		var debounceCts = new CancellationTokenSource();
		_persistDebounceCts = debounceCts;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(PersistDebounceMs, debounceCts.Token).ConfigureAwait(false);
				await PersistCacheAsync(items, ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Debounced.
			}
		}, CancellationToken.None);
	}

	private static bool ShouldThrottleMemory()
	{
#if ANDROID
		try
		{
			var runtime = Java.Lang.Runtime.GetRuntime();
			var free = runtime.FreeMemory();
			var total = runtime.TotalMemory();
			var max = runtime.MaxMemory();
			var available = max - (total - free);
			// Pause thumbnail generation if less than ~32 MB free to avoid GC spikes.
			return available < 32 * 1024 * 1024;
		}
		catch
		{
			return false;
		}
#else
		return false;
#endif
	}

	private static DateTime GetLocalDate(DateTimeOffset? takenAt)
	{
		return (takenAt ?? DateTimeOffset.Now).ToLocalTime().Date;
	}

	private static string FormatSectionTitle(DateTime date)
	{
		var today = DateTime.Now.Date;
		if (date == today)
			return "Today";
		if (date == today.AddDays(-1))
			return "Yesterday";
		return date.ToString("ddd, MMM d");
	}
}
