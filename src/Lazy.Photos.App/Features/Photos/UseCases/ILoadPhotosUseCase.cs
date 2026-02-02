using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.UseCases;

/// <summary>
/// Use case for loading photos (Clean Architecture - Application Layer).
/// Single Responsibility: Orchestrating the photo loading workflow.
/// </summary>
public interface ILoadPhotosUseCase
{
	/// <summary>
	/// Executes the initial photo load from cache, remote, and device.
	/// </summary>
	Task<LoadPhotosResult> ExecuteAsync(CancellationToken ct);

	/// <summary>
	/// Loads the next page of photos for infinite scrolling.
	/// </summary>
	Task<LoadNextPageResult> LoadNextPageAsync(int targetDisplayCount, CancellationToken ct);
}

/// <summary>
/// Result of the initial photo load operation.
/// </summary>
public sealed record LoadPhotosResult(
	IReadOnlyList<PhotoItem> CachedPhotos,
	IReadOnlyList<PhotoItem> MergedPhotos,
	IReadOnlyList<PhotoItem> DisplayPhotos,
	bool DeviceAccessGranted,
	string? ErrorMessage);

/// <summary>
/// Result of loading the next page.
/// </summary>
public sealed record LoadNextPageResult(
	IReadOnlyList<PhotoItem> DisplayPhotos,
	int PreviousDisplayCount,
	bool HasChanges);
