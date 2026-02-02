using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Photos.UseCases;

namespace Lazy.Photos.App.Features.Photos;

/// <summary>
/// ViewModel for the main photos page.
///
/// SOLID Principles Applied:
/// - Single Responsibility: Only handles UI state and user interactions, delegates business logic to use cases
/// - Open/Closed: Extended through new use cases without modifying this class
/// - Liskov Substitution: All dependencies are interfaces that can be substituted
/// - Interface Segregation: Uses focused interfaces for each concern
/// - Dependency Inversion: Depends on abstractions, not concrete implementations
///
/// Clean Architecture:
/// - This is the Presentation Layer (ViewModel)
/// - Depends on Application Layer (Use Cases) and Domain Layer (Models)
/// - Business logic is in Use Cases and Services
/// </summary>
public partial class MainPageViewModel : ObservableObject
{
	private readonly IPhotoNavigationService _photoNavigationService;
	private readonly IPhotoStateRepository _photoStateRepository;
	private readonly IPhotoSectionBuilder _sectionBuilder;
	private readonly IThumbnailService _thumbnailService;
	private readonly IPaginationManager _paginationManager;
	private readonly ILoadPhotosUseCase _loadPhotosUseCase;
	private readonly ICachePersistenceUseCase _cachePersistenceUseCase;

	private readonly SemaphoreSlim _pageLoadSemaphore = new(1);
	private CancellationTokenSource? _loadCts;
	private CancellationTokenSource? _scrollQuietCts;

	private volatile bool _isScrolling;
	private volatile int _visibleStartIndex;
	private volatile int _visibleEndIndex;
	private bool _loadedOnce;

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
		IPhotoNavigationService photoNavigationService,
		IPhotoStateRepository photoStateRepository,
		IPhotoSectionBuilder sectionBuilder,
		IThumbnailService thumbnailService,
		IPaginationManager paginationManager,
		ILoadPhotosUseCase loadPhotosUseCase,
		ICachePersistenceUseCase cachePersistenceUseCase)
	{
		_photoNavigationService = photoNavigationService;
		_photoStateRepository = photoStateRepository;
		_sectionBuilder = sectionBuilder;
		_thumbnailService = thumbnailService;
		_paginationManager = paginationManager;
		_loadPhotosUseCase = loadPhotosUseCase;
		_cachePersistenceUseCase = cachePersistenceUseCase;
	}

	/// <summary>
	/// Notifies the ViewModel of scroll position changes for thumbnail optimization and infinite scroll.
	/// </summary>
	public void NotifyScrolled(int firstVisibleIndex, int lastVisibleIndex)
	{
		_isScrolling = true;
		_visibleStartIndex = Math.Max(0, firstVisibleIndex);
		_visibleEndIndex = Math.Max(_visibleStartIndex, lastVisibleIndex);

		ResetScrollQuietTimer();

		if (_paginationManager.ShouldLoadMore(lastVisibleIndex, _photoStateRepository.DisplayCount))
			_ = LoadNextPageAsync();
	}

	partial void OnErrorMessageChanged(string? value)
	{
		HasError = !string.IsNullOrWhiteSpace(value);
	}

	/// <summary>
	/// Ensures photos are loaded at least once.
	/// </summary>
	public async Task EnsureLoadedAsync()
	{
		if (_loadedOnce)
			return;

		_loadedOnce = true;
		await RefreshAsync();
	}

	/// <summary>
	/// Updates the grid layout configuration.
	/// </summary>
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

		CancelCurrentLoad();
		_loadCts = new CancellationTokenSource();

		try
		{
			var result = await _loadPhotosUseCase.ExecuteAsync(_loadCts.Token);

			// Apply cached photos immediately
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ApplySnapshot(result.CachedPhotos);
				_sectionBuilder.RebuildSections(Photos, PhotoSections);
			});

			// Start thumbnail fill for cached photos
			StartThumbnailFill();

			// Set error message if device access was denied
			if (!string.IsNullOrEmpty(result.ErrorMessage))
				ErrorMessage = result.ErrorMessage;

			// Apply merged results
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ApplySnapshot(result.DisplayPhotos);
				_sectionBuilder.RebuildSections(Photos, PhotoSections);
			});

			// Queue cache persistence and start thumbnail fill
			_cachePersistenceUseCase.QueuePersist(result.MergedPhotos, _loadCts.Token);
			StartThumbnailFill();

			IsInitializing = false;
		}
		catch (OperationCanceledException)
		{
			// Ignore cancellation
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

	[RelayCommand]
	private async Task OpenPhotoAsync(PhotoItem? photo)
	{
		if (photo == null)
			return;

		await _photoNavigationService.ShowPhotoAsync(photo);
	}

	private async Task LoadNextPageAsync()
	{
		if (IsBusy || _loadCts == null)
			return;

		if (!await _pageLoadSemaphore.WaitAsync(0).ConfigureAwait(false))
			return;

		try
		{
			var ct = _loadCts.Token;
			if (ct.IsCancellationRequested)
				return;

			var targetCount = _photoStateRepository.DisplayCount + _paginationManager.PageSize;
			var result = await _loadPhotosUseCase.LoadNextPageAsync(targetCount, ct);

			if (!result.HasChanges)
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ApplySnapshot(result.DisplayPhotos);

				if (result.PreviousDisplayCount <= 0 || PhotoSections.Count == 0)
					_sectionBuilder.RebuildSections(Photos, PhotoSections);
				else
					_sectionBuilder.AppendSections(Photos, PhotoSections, result.PreviousDisplayCount, result.DisplayPhotos.Count);
			});

			// Queue cache persistence and start thumbnail fill
			_cachePersistenceUseCase.QueuePersist(_photoStateRepository.BuildOrderedSnapshot(), ct);
			StartThumbnailFill();
		}
		catch (OperationCanceledException)
		{
			// Ignore cancellation
		}
		finally
		{
			_pageLoadSemaphore.Release();
		}
	}

	private void ResetScrollQuietTimer()
	{
		_scrollQuietCts?.Cancel();
		_scrollQuietCts = new CancellationTokenSource();
		_ = HandleScrollQuietAsync(_scrollQuietCts.Token);
	}

	private async Task HandleScrollQuietAsync(CancellationToken ct)
	{
		try
		{
			await Task.Delay(250, ct).ConfigureAwait(false);
			_isScrolling = false;

			var visiblePhotos = await MainThread.InvokeOnMainThreadAsync(() => Photos.ToList());
			await _thumbnailService.UpgradeVisibleThumbnailsAsync(
				visiblePhotos,
				_visibleStartIndex,
				_visibleEndIndex,
				ct);
		}
		catch (OperationCanceledException)
		{
			// Scrolling continued
		}
	}

	private void StartThumbnailFill()
	{
		if (_loadCts == null)
			return;

		_ = _thumbnailService.StartThumbnailFillAsync(
			Photos,
			_visibleStartIndex,
			_visibleEndIndex,
			() => _isScrolling,
			_loadCts.Token);
	}

	private void CancelCurrentLoad()
	{
		_loadCts?.Cancel();
		_thumbnailService.CancelPendingOperations();
		_cachePersistenceUseCase.CancelPending();
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
}
