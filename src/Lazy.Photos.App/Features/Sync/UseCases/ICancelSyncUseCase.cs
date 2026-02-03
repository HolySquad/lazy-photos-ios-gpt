namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for cancelling the current sync operation.
/// </summary>
public interface ICancelSyncUseCase
{
	Task ExecuteAsync();
}
