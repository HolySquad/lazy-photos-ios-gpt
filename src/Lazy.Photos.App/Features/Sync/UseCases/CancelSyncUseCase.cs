using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Sync.Services;

namespace Lazy.Photos.App.Features.Sync.UseCases;

/// <summary>
/// Use case for cancelling the current sync operation.
/// </summary>
public sealed class CancelSyncUseCase : ICancelSyncUseCase
{
	private readonly ISyncOrchestrationService _orchestrationService;
	private readonly IUploadQueueService _queueService;
	private readonly ILogService _logService;

	public CancelSyncUseCase(
		ISyncOrchestrationService orchestrationService,
		IUploadQueueService queueService,
		ILogService logService)
	{
		_orchestrationService = orchestrationService;
		_queueService = queueService;
		_logService = logService;
	}

	public async Task ExecuteAsync()
	{
		await _logService.LogInfoAsync("Sync", "Cancel requested by user");
		await _orchestrationService.CancelSyncAsync();

		// Clear completed items from queue
		await _queueService.ClearCompletedItemsAsync(CancellationToken.None);
		await _logService.LogInfoAsync("Sync", "Cleared completed items from queue");
	}
}
