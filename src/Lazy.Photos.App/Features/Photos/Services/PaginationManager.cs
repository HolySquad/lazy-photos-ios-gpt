using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of pagination management.
/// Single Responsibility: Managing pagination state for remote and device photos.
/// Dependency Inversion: Depends on abstractions (IPhotoLibraryService, IPhotoSyncService).
/// </summary>
public sealed class PaginationManager : IPaginationManager
{
	private readonly IPhotoLibraryService _photoLibraryService;
	private readonly IPhotoSyncService _photoSyncService;
	private readonly SemaphoreSlim _devicePageSemaphore = new(1);

	private IAsyncEnumerator<PhotoItem>? _deviceEnumerator;
	private string? _remoteCursor;
	private bool _remoteHasMore = true;
	private bool _deviceEnumerationComplete;
	private bool _deviceAccessGranted;

	public bool RemoteHasMore => _remoteHasMore;
	public bool DeviceEnumerationComplete => _deviceEnumerationComplete;
	public bool DeviceAccessGranted => _deviceAccessGranted;
	public string? RemoteCursor => _remoteCursor;
	public int PageSize => 30;
	public int ScrollLoadThreshold => 8;

	public PaginationManager(
		IPhotoLibraryService photoLibraryService,
		IPhotoSyncService photoSyncService)
	{
		_photoLibraryService = photoLibraryService;
		_photoSyncService = photoSyncService;
	}

	public void SetDeviceAccessGranted(bool granted)
	{
		_deviceAccessGranted = granted;
		if (!granted)
			_deviceEnumerationComplete = true;
	}

	public void UpdateFromRemotePage(PhotoPage page)
	{
		_remoteCursor = page.Cursor;
		_remoteHasMore = page.HasMore;
		if (page.Items.Count == 0 && !page.HasMore)
			_remoteHasMore = false;
	}

	public async Task<List<PhotoItem>> FetchDevicePageAsync(int pageSize, CancellationToken ct)
	{
		if (pageSize <= 0 || _deviceEnumerationComplete || !_deviceAccessGranted)
			return new List<PhotoItem>();

		await _devicePageSemaphore.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (_deviceEnumerationComplete || !_deviceAccessGranted)
				return new List<PhotoItem>();

			_deviceEnumerator ??= _photoLibraryService.StreamRecentPhotosAsync(int.MaxValue, ct).GetAsyncEnumerator(ct);

			var page = new List<PhotoItem>(pageSize);
			while (page.Count < pageSize && !ct.IsCancellationRequested)
			{
				if (_deviceEnumerator == null)
					break;

				if (!await _deviceEnumerator.MoveNextAsync().ConfigureAwait(false))
				{
					_deviceEnumerationComplete = true;
					break;
				}

				page.Add(_deviceEnumerator.Current);
			}

			return page;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			_deviceEnumerationComplete = true;
			return new List<PhotoItem>();
		}
		finally
		{
			_devicePageSemaphore.Release();
		}
	}

	public async Task<PhotoPage> LoadRemotePageAsync(CancellationToken ct)
	{
		try
		{
			return await _photoSyncService.GetPageAsync(_remoteCursor, PageSize, ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return new PhotoPage(Array.Empty<PhotoItem>(), _remoteCursor, false);
		}
	}

	public bool ShouldLoadMore(int lastVisibleIndex, int displayCount)
	{
		return displayCount > 0 && lastVisibleIndex >= Math.Max(0, displayCount - ScrollLoadThreshold);
	}

	public bool HasMoreContent()
	{
		return _remoteHasMore || !_deviceEnumerationComplete;
	}

	public async Task ResetAsync()
	{
		_deviceEnumerationComplete = false;
		_deviceAccessGranted = false;
		_remoteCursor = null;
		_remoteHasMore = true;

		if (_deviceEnumerator != null)
		{
			try
			{
				await _deviceEnumerator.DisposeAsync().ConfigureAwait(false);
			}
			catch
			{
				// Best-effort cleanup
			}
			finally
			{
				_deviceEnumerator = null;
			}
		}
	}
}
