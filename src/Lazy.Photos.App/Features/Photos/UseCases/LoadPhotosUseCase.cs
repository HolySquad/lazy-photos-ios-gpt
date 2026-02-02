using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;

namespace Lazy.Photos.App.Features.Photos.UseCases;

/// <summary>
/// Implementation of photo loading use case.
/// Clean Architecture: Application Layer - orchestrates business logic.
/// Single Responsibility: Coordinating the photo loading workflow.
/// Dependency Inversion: Depends on abstractions, not concrete implementations.
/// </summary>
public sealed class LoadPhotosUseCase : ILoadPhotosUseCase
{
	private readonly IPhotoCacheService _photoCacheService;
	private readonly IPhotoStateRepository _photoStateRepository;
	private readonly IPaginationManager _paginationManager;
	private readonly IPhotoPermissionService _permissionService;

	public LoadPhotosUseCase(
		IPhotoCacheService photoCacheService,
		IPhotoStateRepository photoStateRepository,
		IPaginationManager paginationManager,
		IPhotoPermissionService permissionService)
	{
		_photoCacheService = photoCacheService;
		_photoStateRepository = photoStateRepository;
		_paginationManager = paginationManager;
		_permissionService = permissionService;
	}

	public async Task<LoadPhotosResult> ExecuteAsync(CancellationToken ct)
	{
		string? errorMessage = null;

		// Step 1: Reset state
		await _paginationManager.ResetAsync();
		_photoStateRepository.Reset();

		// Step 2: Load from cache
		var cached = await _photoCacheService.GetCachedPhotosAsync(ct);
		_photoStateRepository.InitializeFromCache(cached);
		_photoStateRepository.EnsureDisplayCountInitialized(cached.Count, _paginationManager.PageSize);

		// Step 3: Check permissions
		var deviceAccessGranted = await _permissionService.EnsurePhotosPermissionAsync();
		_paginationManager.SetDeviceAccessGranted(deviceAccessGranted);

		if (!deviceAccessGranted)
		{
			errorMessage = "Showing cloud photos. Photo permission is required to show device photos.";
		}

		// Step 4: Load from remote and device in parallel
		var remoteTask = _paginationManager.LoadRemotePageAsync(ct);
		var deviceTask = _paginationManager.FetchDevicePageAsync(_paginationManager.PageSize, ct);
		await Task.WhenAll(remoteTask, deviceTask);

		var remotePage = remoteTask.Result;
		_paginationManager.UpdateFromRemotePage(remotePage);

		// Step 5: Merge and sort
		var merged = _photoStateRepository.MergeAndSort(remotePage.Items, deviceTask.Result);
		_photoStateRepository.EnsureDisplayCountInitialized(merged.Count, _paginationManager.PageSize);

		// Step 6: Build display snapshot
		var displayItems = _photoStateRepository.BuildDisplaySnapshot();

		return new LoadPhotosResult(
			CachedPhotos: cached,
			MergedPhotos: merged,
			DisplayPhotos: displayItems,
			DeviceAccessGranted: deviceAccessGranted,
			ErrorMessage: errorMessage);
	}

	public async Task<LoadNextPageResult> ExecuteNextPageAsync(int targetDisplayCount, CancellationToken ct)
	{
		return await LoadNextPageAsync(targetDisplayCount, ct);
	}

	public async Task<LoadNextPageResult> LoadNextPageAsync(int targetDisplayCount, CancellationToken ct)
	{
		var previousCount = _photoStateRepository.DisplayCount;

		// Load more from remote if needed
		if (targetDisplayCount > _photoStateRepository.TotalCount && _paginationManager.RemoteHasMore)
		{
			var remotePage = await _paginationManager.LoadRemotePageAsync(ct);
			_paginationManager.UpdateFromRemotePage(remotePage);
			if (remotePage.Items.Count > 0)
				_photoStateRepository.MergeAndSort(remotePage.Items);
		}

		// Load more from device if needed
		if (targetDisplayCount > _photoStateRepository.TotalCount && !_paginationManager.DeviceEnumerationComplete)
		{
			var devicePage = await _paginationManager.FetchDevicePageAsync(_paginationManager.PageSize, ct);
			if (devicePage.Count > 0)
				_photoStateRepository.MergeAndSort(devicePage);
		}

		// Try to increment display count
		var hasChanges = _photoStateRepository.TryIncrementDisplayCount(_paginationManager.PageSize);
		var displayItems = _photoStateRepository.BuildDisplaySnapshot();

		return new LoadNextPageResult(
			DisplayPhotos: displayItems,
			PreviousDisplayCount: previousCount,
			HasChanges: hasChanges);
	}
}
