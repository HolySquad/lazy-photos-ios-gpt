using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Sync.Models;
using Lazy.Photos.App.Features.Sync.Services;

namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for starting photo sync from device to server.
/// Identifies new/modified photos and queues them for upload.
/// Photos are uploaded one at a time for better progress tracking and stability.
/// </summary>
public sealed class ExecuteSyncUseCase : IExecuteSyncUseCase
{
	private const int MaxPhotosToSync = 1000; // Limit for v1
	private const int HashComputeChunkSize = 6; // Used only for initial hash computation

	private readonly IPhotoLibraryService _libraryService;
	private readonly IPhotoCacheService _cacheService;
	private readonly IUploadQueueService _queueService;
	private readonly ISyncOrchestrationService _orchestrationService;
	private readonly IPhotoPermissionService _permissionService;
	private readonly ILogService _logService;

	public ExecuteSyncUseCase(
		IPhotoLibraryService libraryService,
		IPhotoCacheService cacheService,
		IUploadQueueService queueService,
		ISyncOrchestrationService orchestrationService,
		IPhotoPermissionService permissionService,
		ILogService logService)
	{
		_libraryService = libraryService;
		_cacheService = cacheService;
		_queueService = queueService;
		_orchestrationService = orchestrationService;
		_permissionService = permissionService;
		_logService = logService;
	}

	public async Task<ExecuteSyncResult> ExecuteAsync(CancellationToken ct)
	{
		try
		{
			await _logService.LogInfoAsync("Sync", "Starting sync preparation");

			// Check photo library permissions
			var hasPermission = await _permissionService.EnsurePhotosPermissionAsync();
			if (!hasPermission)
			{
				await _logService.LogErrorAsync("Sync", "Photo library permission denied");
				return new ExecuteSyncResult(false, 0, "Photo library permission is required to sync photos.");
			}

			// Load device photos
			var devicePhotos = await _libraryService.GetRecentPhotosAsync(MaxPhotosToSync, ct);
			await _logService.LogInfoAsync("Sync", $"Found {devicePhotos.Count} photos on device");

			// Load cached photos
			var cachedPhotos = await _cacheService.GetCachedPhotosAsync(ct);
			var cachedPhotoIds = new HashSet<string>(cachedPhotos.Where(p => !string.IsNullOrWhiteSpace(p.Id)).Select(p => p.Id!));
			var syncedHashes = new HashSet<string>(cachedPhotos.Where(p => p.IsSynced && !string.IsNullOrWhiteSpace(p.Hash)).Select(p => p.Hash!));

			// Identify new photos (not in cache or not synced)
			var newPhotos = devicePhotos
				.Where(p => !string.IsNullOrWhiteSpace(p.Id))
				.Where(p => !cachedPhotoIds.Contains(p.Id!) || !p.IsSynced)
				.ToList();

			await _logService.LogInfoAsync("Sync", $"Identified {newPhotos.Count} new photos to upload");

			if (newPhotos.Count == 0)
			{
				await _logService.LogInfoAsync("Sync", "No new photos to sync");
				return new ExecuteSyncResult(true, 0, null);
			}

			// Build queue items
			var queueItems = new List<SyncQueueItem>();

			// Process photos in chunks for hash computation
			for (int i = 0; i < newPhotos.Count; i += HashComputeChunkSize)
			{
				if (ct.IsCancellationRequested)
					break;

				var chunk = newPhotos.Skip(i).Take(HashComputeChunkSize);

				foreach (var photo in chunk)
				{
					if (ct.IsCancellationRequested)
						break;

					// Skip if hash is already known and synced
					if (!string.IsNullOrWhiteSpace(photo.Hash) && syncedHashes.Contains(photo.Hash))
					{
						await _logService.LogInfoAsync("Sync", $"Skipping {photo.DisplayName} - already synced (hash: {photo.Hash})");
						continue;
					}

					// Collect metadata
					long sizeBytes = 0;
					string? mimeType = null;
					int? width = null;
					int? height = null;

					try
					{
						sizeBytes = await _libraryService.GetPhotoSizeAsync(photo, ct);
						mimeType = await _libraryService.GetPhotoMimeTypeAsync(photo, ct);
						var dimensions = await _libraryService.GetPhotoDimensionsAsync(photo, ct);
						if (dimensions.HasValue)
						{
							width = dimensions.Value.Width;
							height = dimensions.Value.Height;
						}
					}
					catch (Exception ex)
					{
						await _logService.LogWarningAsync("Sync", $"Failed to get metadata for {photo.DisplayName}: {ex.Message}");
						mimeType = DetermineMimeType(photo.DisplayName);
					}

					var queueItem = new SyncQueueItem
					{
						LocalPhotoId = photo.Id!,
						Hash = photo.Hash,
						LocalPath = photo.Id!,
						FileName = photo.DisplayName ?? "unknown.jpg",
						SizeBytes = sizeBytes,
						MimeType = mimeType ?? DetermineMimeType(photo.DisplayName),
						CapturedAt = photo.TakenAt,
						Width = width,
						Height = height,
						LocationLat = null,
						LocationLon = null,
						Status = QueueItemStatus.Pending
					};

					queueItems.Add(queueItem);
				}
			}

			if (queueItems.Count == 0)
			{
				await _logService.LogInfoAsync("Sync", "No new photos to queue after filtering");
				return new ExecuteSyncResult(true, 0, null);
			}

			// Enqueue items
			await _queueService.EnqueueItemsAsync(queueItems, ct);
			await _logService.LogInfoAsync("Sync", $"Queued {queueItems.Count} photos for upload");

			// Start sync orchestration
			await _orchestrationService.StartSyncAsync(ct);

			return new ExecuteSyncResult(true, queueItems.Count, null);
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to prepare sync", ex);
			return new ExecuteSyncResult(false, 0, ex.Message);
		}
	}

	private static string DetermineMimeType(string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			return "image/jpeg";

		var extension = Path.GetExtension(fileName).ToLowerInvariant();
		return extension switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".heic" or ".heif" => "image/heic",
			".webp" => "image/webp",
			".gif" => "image/gif",
			".mp4" => "video/mp4",
			".mov" => "video/quicktime",
			_ => "image/jpeg"
		};
	}
}
