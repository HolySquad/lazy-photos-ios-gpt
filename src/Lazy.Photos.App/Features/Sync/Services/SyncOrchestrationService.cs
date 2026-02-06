using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Sync.Models;
using Lazy.Photos.App.Services;
using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;

namespace Lazy.Photos.App.Features.Sync.Services;

/// <summary>
/// Orchestrates the photo sync process with pause/resume capability.
/// Supports configurable parallel uploads (1-128) with SemaphoreSlim throttling.
/// </summary>
public sealed class SyncOrchestrationService : ISyncOrchestrationService
{
	private const int MaxRetries = 3;
	private const long UploadChunkSizeBytes = 1024 * 1024; // 1MB chunks
	private const int StateSaveBatchSize = 5; // Save state every N completed items in parallel mode

	private readonly IUploadQueueService _queueService;
	private readonly ISyncStateRepository _stateRepository;
	private readonly IPhotosApiClient _apiClient;
	private readonly IPhotoLibraryService _libraryService;
	private readonly IPhotoCacheService _cacheService;
	private readonly ILogService _logService;
	private readonly IAppSettingsService _settingsService;

	private CancellationTokenSource? _cts;
	private Task? _syncTask;

	// Thread-safe counters for parallel uploads
	private int _completedCount;
	private int _failedCount;
	private int _activeCount;
	private long _totalBytesUploaded;
	private long _totalBytesToUpload;
	private int _stateSaveCounter;

	public SyncOrchestrationService(
		IUploadQueueService queueService,
		ISyncStateRepository stateRepository,
		IPhotosApiClient apiClient,
		IPhotoLibraryService libraryService,
		IPhotoCacheService cacheService,
		ILogService logService,
		IAppSettingsService settingsService)
	{
		_queueService = queueService;
		_stateRepository = stateRepository;
		_apiClient = apiClient;
		_libraryService = libraryService;
		_cacheService = cacheService;
		_logService = logService;
		_settingsService = settingsService;

		CurrentState = new SyncState();

		// Load previous state and settings asynchronously
		_ = Task.Run(async () =>
		{
			try
			{
				var parallelCount = await _settingsService.GetParallelUploadCountAsync();
				CurrentState.ParallelUploadCount = parallelCount;

				var savedState = await _stateRepository.LoadStateAsync(CancellationToken.None);
				if (savedState != null && savedState.Status == SyncStatus.Running)
				{
					savedState.Status = SyncStatus.Paused;
					await _logService.LogInfoAsync("Sync", "Detected incomplete sync from previous session");
				}

				if (savedState != null)
				{
					CurrentState.Status = savedState.Status;
					CurrentState.TotalItems = savedState.TotalItems;
					CurrentState.CompletedItems = savedState.CompletedItems;
					CurrentState.FailedItems = savedState.FailedItems;
					CurrentState.CurrentItemName = savedState.CurrentItemName;
					CurrentState.ProgressPercentage = savedState.ProgressPercentage;
					CurrentState.ErrorMessage = savedState.ErrorMessage;
					CurrentState.StartedAt = savedState.StartedAt;
					CurrentState.CompletedAt = savedState.CompletedAt;
					CurrentState.CurrentFileUploadedBytes = savedState.CurrentFileUploadedBytes;
					CurrentState.CurrentFileTotalBytes = savedState.CurrentFileTotalBytes;
					CurrentState.CurrentFileProgressPercentage = savedState.CurrentFileProgressPercentage;
				}
			}
			catch (Exception ex)
			{
				await _logService.LogWarningAsync("Sync", "Failed to load previous sync state", ex.Message);
			}
		});
	}

	public SyncState CurrentState { get; }

	public bool CanStart => CurrentState.Status is SyncStatus.Idle or SyncStatus.Completed or SyncStatus.Error or SyncStatus.Cancelled;
	public bool CanPause => CurrentState.Status is SyncStatus.Running or SyncStatus.Preparing;
	public bool CanResume => CurrentState.Status is SyncStatus.Paused;
	public bool CanCancel => CurrentState.Status is SyncStatus.Running or SyncStatus.Preparing or SyncStatus.Paused;

	public async Task StartSyncAsync(CancellationToken ct)
	{
		if (CurrentState.Status == SyncStatus.Paused)
		{
			await _logService.LogInfoAsync("Sync", "Resuming previous paused sync");
			await ResumeSyncAsync(ct);
			return;
		}

		if (CurrentState.Status == SyncStatus.Running || CurrentState.Status == SyncStatus.Preparing)
		{
			await _logService.LogInfoAsync("Sync", "Sync already in progress - continuing");
			return;
		}

		if (!CanStart)
		{
			await _logService.LogWarningAsync("Sync", "Cannot start sync - invalid state");
			return;
		}

		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

		CurrentState.Status = SyncStatus.Preparing;
		CurrentState.ErrorMessage = null;
		CurrentState.StartedAt = DateTimeOffset.UtcNow;
		CurrentState.CompletedAt = null;

		// Load the current parallel upload setting
		var parallelCount = await _settingsService.GetParallelUploadCountAsync();
		CurrentState.ParallelUploadCount = parallelCount;

		await _logService.LogInfoAsync("Sync", $"Sync started with {parallelCount} parallel uploads");

		_syncTask = Task.Run(async () => await ProcessSyncAsync(parallelCount, _cts.Token), _cts.Token);
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
		CurrentState.ActiveUploads = 0;
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

		var parallelCount = await _settingsService.GetParallelUploadCountAsync();
		CurrentState.ParallelUploadCount = parallelCount;

		await _logService.LogInfoAsync("Sync", $"Resuming sync with {parallelCount} parallel uploads");

		_syncTask = Task.Run(async () => await ProcessSyncAsync(parallelCount, _cts.Token), _cts.Token);
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
		CurrentState.ActiveUploads = 0;
		CurrentState.CompletedAt = DateTimeOffset.UtcNow;
		await _stateRepository.ClearStateAsync(CancellationToken.None);

		await _logService.LogInfoAsync("Sync", "Sync cancelled");
	}

	private async Task ProcessSyncAsync(int parallelCount, CancellationToken ct)
	{
		try
		{
			CurrentState.Status = SyncStatus.Running;

			var pendingItems = await _queueService.GetPendingItemsAsync(ct);
			CurrentState.TotalItems = pendingItems.Count;

			// Reset counters
			_completedCount = CurrentState.CompletedItems;
			_failedCount = CurrentState.FailedItems;
			_activeCount = 0;
			_totalBytesUploaded = 0;
			_totalBytesToUpload = 0;
			_stateSaveCounter = 0;
			CurrentState.UpdateProgress();

			await _logService.LogInfoAsync("Sync", $"Processing {pendingItems.Count} items with {parallelCount} parallel uploads");

			if (parallelCount <= 1)
			{
				// Sequential mode - original behavior
				foreach (var item in pendingItems)
				{
					if (ct.IsCancellationRequested)
						break;

					await ProcessItemAsync(item, ct);
					await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);
				}
			}
			else
			{
				// Parallel mode with SemaphoreSlim throttling
				using var semaphore = new SemaphoreSlim(parallelCount, parallelCount);
				var tasks = new List<Task>();

				foreach (var item in pendingItems)
				{
					if (ct.IsCancellationRequested)
						break;

					await semaphore.WaitAsync(ct);

					tasks.Add(Task.Run(async () =>
					{
						try
						{
							await ProcessItemParallelAsync(item, ct);
						}
						finally
						{
							semaphore.Release();
						}
					}, ct));
				}

				// Wait for all in-flight uploads to complete
				try
				{
					await Task.WhenAll(tasks);
				}
				catch (OperationCanceledException)
				{
					// Expected on cancel/pause
				}
			}

			if (!ct.IsCancellationRequested)
			{
				CurrentState.Status = SyncStatus.Completed;
				CurrentState.ActiveUploads = 0;
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
			CurrentState.ActiveUploads = 0;
			CurrentState.CompletedAt = DateTimeOffset.UtcNow;
			await _logService.LogErrorAsync("Sync", "Sync failed", ex);
		}
		finally
		{
			await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);
		}
	}

	/// <summary>
	/// Processes a single item in sequential mode (preserves original behavior).
	/// </summary>
	private async Task ProcessItemAsync(SyncQueueItem item, CancellationToken ct)
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			CurrentState.CurrentItemName = item.FileName;
			CurrentState.CurrentFileUploadedBytes = 0;
			CurrentState.CurrentFileTotalBytes = item.SizeBytes;
			CurrentState.CurrentFileProgressPercentage = 0;
			CurrentState.ActiveUploads = 1;
		});

		try
		{
			if (string.IsNullOrWhiteSpace(item.Hash))
			{
				await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Hashing, null, ct);
				item.Hash = await ComputeHashForItemAsync(item, ct);

				if (string.IsNullOrWhiteSpace(item.Hash))
				{
					await _logService.LogWarningAsync("Upload", $"Failed to compute hash for {item.FileName}");
					await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, "Hash computation failed", ct);

					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						CurrentState.FailedItems++;
						CurrentState.UpdateProgress();
					});
					return;
				}
			}

			await UploadItemWithRetryAsync(item, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Upload", $"Failed to upload {item.FileName}", ex);
			await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, ex.Message, ct);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				CurrentState.FailedItems++;
				CurrentState.UpdateProgress();
			});
		}
	}

	/// <summary>
	/// Processes a single item in parallel mode with thread-safe counter updates.
	/// </summary>
	private async Task ProcessItemParallelAsync(SyncQueueItem item, CancellationToken ct)
	{
		var active = Interlocked.Increment(ref _activeCount);
		Interlocked.Add(ref _totalBytesToUpload, item.SizeBytes);

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			CurrentState.ActiveUploads = active;
			CurrentState.CurrentItemName = item.FileName;
			CurrentState.TotalBytesToUpload = Interlocked.Read(ref _totalBytesToUpload);
		});

		try
		{
			if (string.IsNullOrWhiteSpace(item.Hash))
			{
				await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Hashing, null, ct);
				item.Hash = await ComputeHashForItemAsync(item, ct);

				if (string.IsNullOrWhiteSpace(item.Hash))
				{
					await _logService.LogWarningAsync("Upload", $"Failed to compute hash for {item.FileName}");
					await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, "Hash computation failed", ct);

					var failed = Interlocked.Increment(ref _failedCount);
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						CurrentState.FailedItems = failed;
						CurrentState.UpdateProgress();
					});
					return;
				}
			}

			await UploadItemWithRetryAsync(item, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Upload", $"Failed to upload {item.FileName}", ex);
			await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Failed, ex.Message, ct);

			var failed = Interlocked.Increment(ref _failedCount);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				CurrentState.FailedItems = failed;
				CurrentState.UpdateProgress();
			});
		}
		finally
		{
			var remaining = Interlocked.Decrement(ref _activeCount);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				CurrentState.ActiveUploads = remaining;
			});

			// Periodically save state (not after every item to reduce I/O)
			if (Interlocked.Increment(ref _stateSaveCounter) % StateSaveBatchSize == 0)
			{
				await _stateRepository.SaveStateAsync(CurrentState, CancellationToken.None);
			}
		}
	}

	private async Task UploadItemWithRetryAsync(SyncQueueItem item, CancellationToken ct)
	{
		for (int attempt = 0; attempt <= MaxRetries; attempt++)
		{
			try
			{
				await UploadItemAsync(item, ct);
				return;
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

					var failed = Interlocked.Increment(ref _failedCount);
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						CurrentState.FailedItems = failed;
						CurrentState.UpdateProgress();
					});
					throw;
				}

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

		await _logService.LogInfoAsync("Upload", $"Creating upload session for {item.FileName} (hash: {item.Hash}, size: {item.SizeBytes})");

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
		await _logService.LogInfoAsync("Upload", $"Upload session created: {sessionResponse.UploadSessionId}, AlreadyExists={sessionResponse.AlreadyExists}");

		if (sessionResponse.AlreadyExists)
		{
			await _logService.LogInfoAsync("Upload", $"Photo {item.FileName} already exists on server (hash: {item.Hash})");
			await _queueService.UpdateItemStatusAsync(item.Id, QueueItemStatus.Skipped, null, ct);
			await UpdateCacheAsSyncedAsync(item, ct);

			var completed = Interlocked.Increment(ref _completedCount);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				CurrentState.CompletedItems = completed;
				CurrentState.UpdateProgress();
			});
			return;
		}

		await UploadFileInChunksAsync(photoItem, sessionResponse.UploadSessionId, item.SizeBytes, ct);

		await _logService.LogInfoAsync("Upload", $"Completing upload session {sessionResponse.UploadSessionId}");
		var completeRequest = new UploadCompleteRequest(StorageKey: $"uploads/{item.Hash}");
		await _apiClient.CompleteUploadAsync(sessionResponse.UploadSessionId, completeRequest, ct);

		await _logService.LogInfoAsync("Upload", $"Successfully uploaded {item.FileName} ({item.SizeBytes} bytes)");
		await _queueService.MarkAsUploadedAsync(item.Id, ct);
		await UpdateCacheAsSyncedAsync(item, ct);

		var completedAfterUpload = Interlocked.Increment(ref _completedCount);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			CurrentState.CompletedItems = completedAfterUpload;
			CurrentState.UpdateProgress();
		});
	}

	private async Task UploadFileInChunksAsync(Photos.Models.PhotoItem photoItem, Guid sessionId, long totalBytes, CancellationToken ct)
	{
		await _logService.LogInfoAsync("Upload", $"Starting chunked upload for photo {photoItem.Id}, session {sessionId}");

		await using var photoStream = await _libraryService.GetPhotoStreamAsync(photoItem, ct);
		if (photoStream == null)
			throw new InvalidOperationException($"Failed to get photo stream for {photoItem.Id}");

		// Use a pooled buffer to reduce GC pressure across parallel uploads
		var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent((int)UploadChunkSizeBytes);
		try
		{
			// Copy platform stream to MemoryStream to avoid timeout issues with PHAsset/MediaStore
			// Pre-allocate capacity to avoid resizing for known file sizes
			var capacity = totalBytes > 0 && totalBytes <= int.MaxValue ? (int)totalBytes : 0;
			await using var memoryStream = new MemoryStream(capacity);
			await photoStream.CopyToAsync(memoryStream, 81920, ct);
			memoryStream.Position = 0;

			long offset = 0;
			int chunkNumber = 0;

			while (true)
			{
				var bytesRead = await memoryStream.ReadAsync(buffer, 0, (int)UploadChunkSizeBytes, ct);
				if (bytesRead == 0)
					break;

				chunkNumber++;

				await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
				await _apiClient.UploadChunkAsync(sessionId, offset, chunkStream, ct);

				offset += bytesRead;

				// Update aggregate byte counters for parallel progress
				var uploaded = Interlocked.Add(ref _totalBytesUploaded, bytesRead);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					CurrentState.TotalBytesUploaded = uploaded;
					CurrentState.TotalBytesToUpload = Interlocked.Read(ref _totalBytesToUpload);

					// For sequential mode, also update per-file progress
					if (CurrentState.ParallelUploadCount <= 1)
					{
						CurrentState.CurrentFileUploadedBytes = offset;
						CurrentState.CurrentFileProgressPercentage = totalBytes > 0 ? (double)offset / totalBytes : 0;
					}

					CurrentState.UpdateProgress();
				});
			}
		}
		finally
		{
			System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private async Task<string?> ComputeHashForItemAsync(SyncQueueItem item, CancellationToken ct)
	{
		try
		{
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
