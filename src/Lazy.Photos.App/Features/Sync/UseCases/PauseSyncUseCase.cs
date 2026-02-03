using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Sync.Services;

namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for pausing the current sync operation.
/// </summary>
public sealed class PauseSyncUseCase : IPauseSyncUseCase
{
	private readonly ISyncOrchestrationService _orchestrationService;
	private readonly ILogService _logService;

	public PauseSyncUseCase(
		ISyncOrchestrationService orchestrationService,
		ILogService logService)
	{
		_orchestrationService = orchestrationService;
		_logService = logService;
	}

	public async Task ExecuteAsync()
	{
		await _logService.LogInfoAsync("Sync", "Pause requested by user");
		await _orchestrationService.PauseSyncAsync();
	}
}
