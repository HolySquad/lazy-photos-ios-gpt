using System.Collections.Concurrent;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of thumbnail generation service.
/// Single Responsibility: Managing thumbnail generation, prioritization, and quality tracking.
/// Open/Closed: Can be extended through IMemoryMonitor dependency.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoStateRepository _photoStateRepository;
	private readonly IMemoryMonitor _memoryMonitor;
	private readonly SemaphoreSlim _thumbnailSemaphore = new(1);
	private readonly ConcurrentDictionary<string, byte> _lowQualityThumbs = new(StringComparer.OrdinalIgnoreCase);
	private CancellationTokenSource? _thumbnailFillCts;

	private const int ChunkSize = 6;
	private const int ApplyBatchSize = 10;
	private const int FirstScreenCount = 30;
	private const int PriorityWindow = 24;
	private const int PriorityLead = 8;

	public ThumbnailService(
		IPhotoLibraryService photoLibraryService,
		IPhotoStateRepository photoStateRepository,
		IMemoryMonitor memoryMonitor)
	{
		_photoLibraryService = photoLibraryService;
		_photoStateRepository = photoStateRepository;
		_memoryMonitor = memoryMonitor;
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
			await Task.Delay(50, linkedCt).ConfigureAwait(false);
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
		var seen = new HashSet<PhotoItem>();

		for (var i = start; i < end; i++)
		{
			var item = all[i];
			prioritized.Add(item);
			seen.Add(item);
		}

		foreach (var item in all)
		{
			if (!seen.Contains(item))
				prioritized.Add(item);
		}

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
				await Task.Delay(50, ct).ConfigureAwait(false);

			try
			{
				await _thumbnailSemaphore.WaitAsync(ct).ConfigureAwait(false);
				if (_memoryMonitor.ShouldThrottle())
				{
					_thumbnailSemaphore.Release();
					await Task.Delay(100, ct).ConfigureAwait(false);
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
				await Task.Delay(50, ct).ConfigureAwait(false);
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
