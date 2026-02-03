using System.Collections.Concurrent;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of thumbnail generation service with device-adaptive tuning.
/// Single Responsibility: Managing thumbnail generation, prioritization, and quality tracking.
/// Dependency Inversion: Uses IDeviceProfileService for adaptive performance tuning.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoStateRepository _photoStateRepository;
	private readonly IMemoryMonitor _memoryMonitor;
	private readonly IDeviceProfileService _deviceProfileService;
	private readonly SemaphoreSlim _thumbnailSemaphore;
	private readonly ConcurrentDictionary<string, byte> _lowQualityThumbs = new(StringComparer.OrdinalIgnoreCase);
	private CancellationTokenSource? _thumbnailFillCts;

	private const int FirstScreenCount = 30;

	// Profile-based tuning properties
	private DeviceProfile Profile => _deviceProfileService.GetProfile();
	private int ChunkSize => Profile.ChunkSize;
	private int ApplyBatchSize => Profile.ApplyBatchSize;
	private int ChunkDelayMs => Profile.ChunkDelayMs;
	private int ScrollPauseDelayMs => Profile.ScrollPauseDelayMs;
	private int PriorityWindow => Profile.PriorityWindow;
	private int PriorityLead => Profile.PriorityLead;

	public ThumbnailService(
		IPhotoLibraryService photoLibraryService,
		IPhotoStateRepository photoStateRepository,
		IMemoryMonitor memoryMonitor,
		IDeviceProfileService deviceProfileService)
	{
		_photoLibraryService = photoLibraryService;
		_photoStateRepository = photoStateRepository;
		_memoryMonitor = memoryMonitor;
		_deviceProfileService = deviceProfileService;

		// Initialize semaphore with profile-based concurrency
		_thumbnailSemaphore = new SemaphoreSlim(deviceProfileService.GetProfile().ThumbnailConcurrency);
	}

	public Task StartThumbnailFillAsync(
		IEnumerable<PhotoItem> photos,
		int visibleStartIndex,
		int visibleEndIndex,
		Func<bool> isScrollingProvider,
		CancellationToken ct)
	{
		CancelPendingOperations();
		_thumbnailFillCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		var linkedCt = _thumbnailFillCts.Token;
		var snapshot = photos is List<PhotoItem> list ? list : photos.ToList();

		return Task.Run(async () =>
		{
			var missing = snapshot.Where(item => item.Thumbnail == null).ToList();

			if (missing.Count == 0)
				return;

			var prioritized = BuildPriorityList(missing, visibleStartIndex);
			var firstScreen = prioritized.Take(FirstScreenCount).ToList();
			var remaining = prioritized.Skip(FirstScreenCount).ToList();

			await ProcessThumbnailBatchAsync(firstScreen, isScrollingProvider, linkedCt).ConfigureAwait(false);
			await Task.Delay(ChunkDelayMs, linkedCt).ConfigureAwait(false);
			await ProcessThumbnailBatchAsync(remaining, isScrollingProvider, linkedCt).ConfigureAwait(false);
		}, linkedCt);
	}

	public async Task UpgradeVisibleThumbnailsAsync(
		IReadOnlyList<PhotoItem> photos,
		int visibleStartIndex,
		int visibleEndIndex,
		CancellationToken ct)
	{
		if (_lowQualityThumbs.IsEmpty || photos.Count == 0)
			return;

		var start = Math.Max(0, Math.Min(visibleStartIndex, photos.Count - 1));
		var end = Math.Max(start, Math.Min(visibleEndIndex, photos.Count - 1));

		var visibleItems = new List<PhotoItem>();
		for (var i = start; i <= end; i++)
			visibleItems.Add(photos[i]);

		if (visibleItems.Count == 0)
			return;

		foreach (var photo in visibleItems)
		{
			if (ct.IsCancellationRequested)
				break;

			var key = _photoStateRepository.EnsureKey(photo);
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
				// Best-effort; continue
			}
		}
	}

	public void TrackLowQualityThumbnail(string key)
	{
		_lowQualityThumbs[key] = 0;
	}

	public void RemoveLowQualityTracking(string key)
	{
		_lowQualityThumbs.TryRemove(key, out _);
	}

	public bool HasLowQualityThumbnail(string key)
	{
		return _lowQualityThumbs.ContainsKey(key);
	}

	public void CancelPendingOperations()
	{
		_thumbnailFillCts?.Cancel();
		_thumbnailFillCts = null;
	}

	private List<PhotoItem> BuildPriorityList(List<PhotoItem> all, int visibleStartIndex)
	{
		if (all.Count == 0)
			return all;

		var start = Math.Max(0, visibleStartIndex - PriorityLead);
		var end = Math.Min(all.Count, start + PriorityWindow);

		var prioritized = new List<PhotoItem>(all.Count);

		// Add visible window items first (highest priority)
		for (var i = start; i < end; i++)
			prioritized.Add(all[i]);

		// Add items before visible window (closer items first)
		for (var i = start - 1; i >= 0; i--)
			prioritized.Add(all[i]);

		// Add items after visible window
		for (var i = end; i < all.Count; i++)
			prioritized.Add(all[i]);

		return prioritized;
	}

	private async Task ProcessThumbnailBatchAsync(
		IEnumerable<PhotoItem> batch,
		Func<bool> isScrollingProvider,
		CancellationToken ct)
	{
		var processedInChunk = 0;
		var pendingApply = new List<(PhotoItem photo, ImageSource thumb)>();

		foreach (var photo in batch)
		{
			if (ct.IsCancellationRequested)
				break;

			while (isScrollingProvider() && !ct.IsCancellationRequested)
				await Task.Delay(ScrollPauseDelayMs, ct).ConfigureAwait(false);

			try
			{
				await _thumbnailSemaphore.WaitAsync(ct).ConfigureAwait(false);
				if (_memoryMonitor.ShouldThrottle())
				{
					_thumbnailSemaphore.Release();
					await Task.Delay(ChunkDelayMs * 2, ct).ConfigureAwait(false);
					continue;
				}

				var isLowQuality = isScrollingProvider();
				var thumb = await _photoLibraryService.BuildThumbnailAsync(photo, isLowQuality, ct).ConfigureAwait(false);
				if (thumb != null)
				{
					var key = _photoStateRepository.EnsureKey(photo);
					if (isLowQuality)
						_lowQualityThumbs[key] = 0;
					else
						_lowQualityThumbs.TryRemove(key, out _);

					pendingApply.Add((photo, thumb));
					if (pendingApply.Count >= ApplyBatchSize)
					{
						await ApplyThumbnailBatchAsync(pendingApply);
						pendingApply.Clear();
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
				// Ignore thumbnail failures
			}
			finally
			{
				_thumbnailSemaphore.Release();
			}

			processedInChunk++;
			if (processedInChunk >= ChunkSize)
			{
				processedInChunk = 0;
				await Task.Delay(ChunkDelayMs, ct).ConfigureAwait(false);
			}
		}

		if (pendingApply.Count > 0 && !ct.IsCancellationRequested)
			await ApplyThumbnailBatchAsync(pendingApply);
	}

	private static Task ApplyThumbnailBatchAsync(List<(PhotoItem photo, ImageSource thumb)> batch)
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
		{
			foreach (var (photo, thumb) in batch)
				photo.Thumbnail = thumb;
		});
	}
}
