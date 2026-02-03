using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Sync.Services;

namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for resuming a paused sync operation.
/// </summary>
public sealed class ResumeSyncUseCase : IResumeSyncUseCase
{
	private readonly ISyncOrchestrationService _orchestrationService;
	private readonly ILogService _logService;

	public ResumeSyncUseCase(
		ISyncOrchestrationService orchestrationService,
		ILogService logService)
	{
		_orchestrationService = orchestrationService;
		_logService = logService;
	}

	public async Task ExecuteAsync(CancellationToken ct)
	{
		await _logService.LogInfoAsync("Sync", "Resume requested by user");
		await _orchestrationService.ResumeSyncAsync(ct);
	}
}
