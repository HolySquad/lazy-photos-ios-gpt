namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for pausing the current sync operation.
/// </summary>
public interface IPauseSyncUseCase
{
	Task ExecuteAsync();
}
