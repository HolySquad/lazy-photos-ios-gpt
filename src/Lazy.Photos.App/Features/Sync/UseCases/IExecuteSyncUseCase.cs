namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for starting photo sync from device to server.
/// Orchestrates: device photo detection, hash computation, queue building, upload initiation.
/// </summary>
public interface IExecuteSyncUseCase
{
	Task<ExecuteSyncResult> ExecuteAsync(CancellationToken ct);
}

public sealed record ExecuteSyncResult(
	bool Success,
	int PhotosQueued,
	string? ErrorMessage);
