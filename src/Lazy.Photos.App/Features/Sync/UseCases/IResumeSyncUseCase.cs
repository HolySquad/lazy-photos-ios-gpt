namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for resuming a paused sync operation.
/// </summary>
public interface IResumeSyncUseCase
{
	Task ExecuteAsync(CancellationToken ct);
}
