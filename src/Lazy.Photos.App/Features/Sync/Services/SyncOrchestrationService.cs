using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Sync.Models;
using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;

namespace Lazy.Photos.App.Features.Sync.Services;

/// <summary>
/// Orchestrates the photo sync process with pause/resume capability.
/// Processes uploads in chunks of 6 items for memory efficiency.
/// </summary>
public sealed class SyncOrchestrationService : ISyncOrchestrationService
{
	private const int ChunkSize = 6;
	private const int MaxRetries = 3;
	private const long UploadChunkSizeBytes = 1024 * 1024; // 1MB chunks

	private readonly IUploadQueueService _queueService;
	private readonly ISyncStateRepository _stateRepository;
	private readonly IPhotosApiClient _apiClient;
	private readonly IPhotoLibraryService _libraryService;
	private readonly IPhotoCacheService _cacheService;
	private readonly ILogService _logService;

	private CancellationTokenSource? _cts;
	private Task? _syncTask;

	public SyncOrchestrationService(
		IUploadQueueService queueService,
		ISyncStateRepository stateRepository,
		IPhotosApiClient apiClient,
		IPhotoLibraryService libraryService,
		IPhotoCacheService cacheService,
		ILogService logService)
	{
		_queueService = queueService;
		_stateRepository = stateRepository;
		_apiClient = apiClient;
		_libraryService = libraryService;
		_cacheService = cacheService;
		_logService = logService;

		CurrentState = new SyncState();
	}

	public SyncState CurrentState { get; }

	public bool CanStart => CurrentState.Status is SyncStatus.Idle or SyncStatus.Completed or SyncStatus.Error or SyncStatus.Cancelled;
	public bool CanPause => CurrentState.Status is SyncStatus.Running or SyncStatus.Preparing;
	public bool CanResume => CurrentState.Status is SyncStatus.Paused;
	public bool CanCancel => CurrentState.Status is SyncStatus.Running or SyncStatus.Preparing or SyncStatus.Paused;

	public async Task StartSyncAsync(CancellationToken ct)
	{
		if (!CanStart)
		{
			await _logService.LogWarningAsync("Sync", "Cannot start sync - already running or invalid state");
			return;
		}

		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

		CurrentState.Status = SyncStatus.Preparing;
		CurrentState.ErrorMessage = null;
		CurrentState.StartedAt = DateTimeOffset.UtcNow;
		CurrentState.CompletedAt = null;

		await _logService.LogInfoAsync("Sync", "Sync started");

		_syncTask = Task.Run(async () => await ProcessSyncAsync(_cts.Token), _cts.Token);
	}

	public async Task PauseSyncAsync()
	{
		if (!CanPause)
		{
			await _logService.LogWarningAsync("Sync", "Cannot pause sync - not running");
			return;
		}

		await _logService.LogInfoAsync("Sync", "Pausing sync");

		_cts?.Cancel();
		if (_syncTask != null)
		{
			try
			{
				await _syncTask;
			}
			catch (OperationCanceledException)
			{
				// Expected
			}
		}

		CurrentState.Status = SyncStatus.Paused;
		await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);

		await _logService.LogInfoAsync("Sync", "Sync paused");
	}

	public async Task ResumeSyncAsync(CancellationToken ct)
	{
		if (!CanResume)
		{
			await _logService.LogWarningAsync("Sync", "Cannot resume sync - not paused");
			return;
		}

		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		CurrentState.Status = SyncStatus.Running;

		await _logService.LogInfoAsync("Sync", "Resuming sync");

		_syncTask = Task.Run(async () => await ProcessSyncAsync(_cts.Token), _cts.Token);
	}

	public async Task CancelSyncAsync()
	{
		if (!CanCancel)
		{
			await _logService.LogWarningAsync("Sync", "Cannot cancel sync - not running");
			return;
		}

		await _logService.LogInfoAsync("Sync", "Cancelling sync");

		CurrentState.Status = SyncStatus.Cancelling;

		_cts?.Cancel();
		if (_syncTask != null)
		{
			try
			{
				await _syncTask;
			}
			catch (OperationCanceledException)
			{
				// Expected
			}
		}

		CurrentState.Status = SyncStatus.Cancelled;
		CurrentState.CompletedAt = DateTimeOffset.UtcNow;
		await _stateRepository.ClearStateAsync(CancellationToken.None);

		await _logService.LogInfoAsync("Sync", "Sync cancelled");
	}

	private async Task ProcessSyncAsync(CancellationToken ct)
	{
		try
		{
			CurrentState.Status = SyncStatus.Running;

			var pendingItems = await _queueService.GetPendingItemsAsync(ct);
			CurrentState.TotalItems = pendingItems.Count;
			CurrentState.UpdateProgress();

			await _logService.LogInfoAsync("Sync", $"Processing {pendingItems.Count} items");

			// Process in chunks of 6
			for (int i = 0; i < pendingItems.Count; i += ChunkSize)
			{
				if (ct.IsCancellationRequested)
					break;

				var chunk = pendingItems.Skip(i).Take(ChunkSize).ToList();
				await ProcessChunkAsync(chunk, ct);
			}

			if (!ct.IsCancellationRequested)
			{
				CurrentState.Status = SyncStatus.Completed;
				CurrentState.CompletedAt = DateTimeOffset.UtcNow;
				await _logService.LogInfoAsync("Sync", $"Sync completed: {CurrentState.CompletedItems} uploaded, {CurrentState.FailedItems} failed");
			}
		}
		catch (OperationCanceledException)
		{
			// Handled by caller
		}
		catch (Exception ex)
		{
			CurrentState.Status = SyncStatus.Error;
			CurrentState.ErrorMessage = ex.Message;
			CurrentState.CompletedAt = DateTimeOffset.UtcNow;
			await _logService.LogErrorAsync("Sync", "Sync failed", ex);
		}
		finally
		{
			await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);
		}
	}

	private async Task ProcessChunkAsync(List<SyncQueueItem> chunk, CancellationToken ct)
	{
		foreach (var item in chunk)
		{
			if (ct.IsCancellationRequested)
				break;

			await ProcessItemAsync(item, ct);
		}

		// Save state after each chunk
		await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);
	}

	private async Task ProcessItemAsync(SyncQueueItem item, CancellationToken ct)
	{
		CurrentState.CurrentItemName = item.FileName;

		try
		{
			// Compute hash if missing
			if (string.IsNullOrWhiteSpace(item.Hash))
			{
				await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Hashing, null, ct);
				item.Hash = await ComputeHashForItemAsync(item, ct);

				if (string.IsNullOrWhiteSpace(item.Hash))
				{
					await _logService.LogWarningAsync("Upload", $"Failed to compute hash for {item.FileName}");
					await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, "Hash computation failed", ct);
					CurrentState.FailedItems++;
					CurrentState.UpdateProgress();
					return;
				}
			}

			await UploadItemWithRetryAsync(item, ct);
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Upload", $"Failed to upload {item.FileName}", ex);
			await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, ex.Message, ct);
			CurrentState.FailedItems++;
			CurrentState.UpdateProgress();
		}
	}

	private async Task UploadItemWithRetryAsync(SyncQueueItem item, CancellationToken ct)
	{
		for (int attempt = 0; attempt <= MaxRetries; attempt++)
		{
			try
			{
				await UploadItemAsync(item, ct);
				return; // Success
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (attempt == MaxRetries)
				{
					await _logService.LogErrorAsync("Upload", $"Failed to upload {item.FileName} after {MaxRetries} retries", ex);
					await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, $"Failed after {MaxRetries} retries: {ex.Message}", ct);
					CurrentState.FailedItems++;
					CurrentState.UpdateProgress();
					throw;
				}

				// Exponential backoff: 1s, 2s, 4s
				var delayMs = (int)Math.Pow(2, attempt) * 1000;
				await _logService.LogWarningAsync("Upload", $"Upload attempt {attempt + 1} failed for {item.FileName}, retrying in {delayMs}ms");
				await Task.Delay(delayMs, ct);
			}
		}
	}

	private async Task UploadItemAsync(SyncQueueItem item, CancellationToken ct)
	{
		await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Uploading, null, ct);

		var photoItem = new Photos.Models.PhotoItem { Id = item.LocalPhotoId };

		// Collect metadata if not already present
		if (item.SizeBytes == 0)
			item.SizeBytes = await _libraryService.GetPhotoSizeAsync(photoItem, ct);

		if (string.IsNullOrWhiteSpace(item.MimeType) || item.MimeType == "image/jpeg")
			item.MimeType = await _libraryService.GetPhotoMimeTypeAsync(photoItem, ct);

		if (item.Width == null || item.Height == null)
		{
			var dimensions = await _libraryService.GetPhotoDimensionsAsync(photoItem, ct);
			if (dimensions.HasValue)
			{
				item.Width = dimensions.Value.Width;
				item.Height = dimensions.Value.Height;
			}
		}

		// Create upload session
		var sessionRequest = new UploadSessionRequest(
			Hash: item.Hash!,
			SizeBytes: item.SizeBytes,
			MimeType: item.MimeType,
			CapturedAt: item.CapturedAt,
			Width: item.Width,
			Height: item.Height,
			LocationLat: item.LocationLat,
			LocationLon: item.LocationLon
		);

		var sessionResponse = await _apiClient.CreateUploadSessionAsync(sessionRequest, ct);

		// If photo already exists on server, skip upload
		if (sessionResponse.AlreadyExists)
		{
			await _logService.LogInfoAsync("Upload", $"Photo {item.FileName} already exists on server (hash: {item.Hash})");
			await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Skipped, null, ct);
			await UpdateCacheAsSyncedAsync(item, ct);
			CurrentState.CompletedItems++;
			CurrentState.UpdateProgress();
			return;
		}

		// Upload file in chunks using platform-specific stream
		await UploadFileInChunksAsync(photoItem, sessionResponse.UploadSessionId, ct);

		// Complete upload
		var completeRequest = new UploadCompleteRequest(StorageKey: $"uploads/{item.Hash}");
		await _apiClient.CompleteUploadAsync(sessionResponse.UploadSessionId, completeRequest, ct);

		await _logService.LogInfoAsync("Upload", $"Successfully uploaded {item.FileName} ({item.SizeBytes} bytes)");
		await _queueService.MarkAsUploadedAsync(item.Id, ct);
		await UpdateCacheAsSyncedAsync(item, ct);

		CurrentState.CompletedItems++;
		CurrentState.UpdateProgress();
	}

	private async Task UploadFileInChunksAsync(Photos.Models.PhotoItem photoItem, Guid sessionId, CancellationToken ct)
	{
		await using var photoStream = await _libraryService.GetPhotoStreamAsync(photoItem, ct);
		if (photoStream == null)
			throw new InvalidOperationException($"Failed to get photo stream for {photoItem.Id}");

		long offset = 0;
		var buffer = new byte[UploadChunkSizeBytes];

		while (true)
		{
			var bytesRead = await photoStream.ReadAsync(buffer, 0, buffer.Length, ct);
			if (bytesRead == 0)
				break;

			await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
			await _apiClient.UploadChunkAsync(sessionId, offset, chunkStream, ct);

			offset += bytesRead;
		}
	}

	private async Task<string?> ComputeHashForItemAsync(SyncQueueItem item, CancellationToken ct)
	{
		try
		{
			// Create a temporary PhotoItem for hash computation
			var photoItem = new Photos.Models.PhotoItem
			{
				Id = item.LocalPhotoId
			};

			return await _libraryService.ComputeHashAsync(photoItem, ct);
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Upload", $"Failed to compute hash for {item.FileName}", ex);
			return null;
		}
	}

	private async Task UpdateCacheAsSyncedAsync(SyncQueueItem item, CancellationToken ct)
	{
		try
		{
			// Load cached photos and update the IsSynced flag
			var cachedPhotos = await _cacheService.GetCachedPhotosAsync(ct);
			var matchingPhoto = cachedPhotos.FirstOrDefault(p => p.Id == item.LocalPhotoId || p.Hash == item.Hash);

			if (matchingPhoto != null)
			{
				matchingPhoto.IsSynced = true;
				await _cacheService.SavePhotosAsync(new[] { matchingPhoto }, ct);
			}
		}
		catch (Exception ex)
		{
			await _logService.LogWarningAsync("Cache", $"Failed to update cache for {item.FileName}", ex.Message);
		}
	}
}
