using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Sync.Models;
using Lazy.Photos.App.Features.Sync.Services;
using Lazy.Photos.App.Features.Sync.UseCases;
using Lazy.Photos.App.Services;

namespace Lazy.Photos.App.Features.Sync;

/// <summary>
/// ViewModel for the sync page.
/// Provides sync controls, status display, and parallel upload configuration.
/// </summary>
public partial class SyncViewModel : ObservableObject
{
	private readonly IExecuteSyncUseCase _executeSyncUseCase;
	private readonly IPauseSyncUseCase _pauseSyncUseCase;
	private readonly IResumeSyncUseCase _resumeSyncUseCase;
	private readonly ICancelSyncUseCase _cancelSyncUseCase;
	private readonly ISyncOrchestrationService _orchestrationService;
	private readonly IUploadQueueService _queueService;
	private readonly ILogService _logService;
	private readonly IAppSettingsService _settingsService;

	private CancellationTokenSource? _cts;

	public SyncViewModel(
		IExecuteSyncUseCase executeSyncUseCase,
		IPauseSyncUseCase pauseSyncUseCase,
		IResumeSyncUseCase resumeSyncUseCase,
		ICancelSyncUseCase cancelSyncUseCase,
		ISyncOrchestrationService orchestrationService,
		IUploadQueueService queueService,
		ILogService logService,
		IAppSettingsService settingsService)
	{
		_executeSyncUseCase = executeSyncUseCase;
		_pauseSyncUseCase = pauseSyncUseCase;
		_resumeSyncUseCase = resumeSyncUseCase;
		_cancelSyncUseCase = cancelSyncUseCase;
		_orchestrationService = orchestrationService;
		_queueService = queueService;
		_logService = logService;
		_settingsService = settingsService;

		// Bind to orchestration service's state
		CurrentState = _orchestrationService.CurrentState;

		// Watch for state changes to update command availability
		CurrentState.PropertyChanged += (s, e) =>
		{
			StartSyncCommand.NotifyCanExecuteChanged();
			PauseSyncCommand.NotifyCanExecuteChanged();
			ResumeSyncCommand.NotifyCanExecuteChanged();
			CancelSyncCommand.NotifyCanExecuteChanged();
		};
	}

	[ObservableProperty]
	private SyncState _currentState;

	[ObservableProperty]
	private QueueStatistics? _queueStats;

	[ObservableProperty]
	private double _parallelUploadCount = 2;

	partial void OnParallelUploadCountChanged(double value)
	{
		var clamped = Math.Clamp((int)value, 1, 128);
		_ = _settingsService.SetParallelUploadCountAsync(clamped);
		CurrentState.ParallelUploadCount = clamped;
	}

	[RelayCommand(CanExecute = nameof(CanStartSync))]
	private async Task StartSyncAsync()
	{
		_cts = new CancellationTokenSource();

		try
		{
			var result = await _executeSyncUseCase.ExecuteAsync(_cts.Token);

			if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
			{
				CurrentState.ErrorMessage = result.ErrorMessage;
			}
		}
		catch (Exception ex)
		{
			CurrentState.ErrorMessage = ex.Message;
			await _logService.LogErrorAsync("Sync", "Failed to start sync", ex);
		}
		finally
		{
			await RefreshQueueStatsAsync();
		}
	}

	private bool CanStartSync() => _orchestrationService.CanStart;

	[RelayCommand(CanExecute = nameof(CanPauseSync))]
	private async Task PauseSyncAsync()
	{
		try
		{
			await _pauseSyncUseCase.ExecuteAsync();
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to pause sync", ex);
		}
		finally
		{
			await RefreshQueueStatsAsync();
		}
	}

	private bool CanPauseSync() => _orchestrationService.CanPause;

	[RelayCommand(CanExecute = nameof(CanResumeSync))]
	private async Task ResumeSyncAsync()
	{
		_cts = new CancellationTokenSource();

		try
		{
			await _resumeSyncUseCase.ExecuteAsync(_cts.Token);
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to resume sync", ex);
		}
		finally
		{
			await RefreshQueueStatsAsync();
		}
	}

	private bool CanResumeSync() => _orchestrationService.CanResume;

	[RelayCommand(CanExecute = nameof(CanCancelSync))]
	private async Task CancelSyncAsync()
	{
		try
		{
			await _cancelSyncUseCase.ExecuteAsync();
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to cancel sync", ex);
		}
		finally
		{
			await RefreshQueueStatsAsync();
		}
	}

	private bool CanCancelSync() => _orchestrationService.CanCancel;

	[RelayCommand]
	private async Task ClearQueueAsync()
	{
		try
		{
			await _queueService.ClearCompletedItemsAsync(CancellationToken.None);
			await _logService.LogInfoAsync("Sync", "Cleared completed queue items");
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to clear queue", ex);
		}
		finally
		{
			await RefreshQueueStatsAsync();
		}
	}

	private async Task RefreshQueueStatsAsync()
	{
		try
		{
			QueueStats = await _queueService.GetStatisticsAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			await _logService.LogErrorAsync("Sync", "Failed to refresh queue stats", ex);
		}
	}

	public async Task OnAppearingAsync()
	{
		await RefreshQueueStatsAsync();

		// Load saved parallel upload count
		var count = await _settingsService.GetParallelUploadCountAsync();
		ParallelUploadCount = count;
	}
}
