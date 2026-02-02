using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Photos.UseCases;

/// <summary>
/// Implementation of cache persistence use case.
/// Single Responsibility: Managing debounced cache persistence.
/// </summary>
public sealed class CachePersistenceUseCase : ICachePersistenceUseCase
{
	private readonly IPhotoCacheService _photoCacheService;
	private CancellationTokenSource? _debounceCts;
	private const int DebounceMs = 750;

	public CachePersistenceUseCase(IPhotoCacheService photoCacheService)
	{
		_photoCacheService = photoCacheService;
	}

	public void QueuePersist(IReadOnlyList<PhotoItem> photos, CancellationToken ct)
	{
		CancelPending();

		var debounceCts = new CancellationTokenSource();
		_debounceCts = debounceCts;

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(DebounceMs, debounceCts.Token).ConfigureAwait(false);
				await PersistAsync(photos, ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// Debounced or cancelled
			}
		}, CancellationToken.None);
	}

	public void CancelPending()
	{
		_debounceCts?.Cancel();
		_debounceCts = null;
	}

	private async Task PersistAsync(IReadOnlyList<PhotoItem> items, CancellationToken ct)
	{
		try
		{
			var snapshot = items.Select(CloneForCache).ToList();
			await _photoCacheService.SavePhotosAsync(snapshot, ct).ConfigureAwait(false);
		}
		catch
		{
			// Best-effort cache persistence
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
}
