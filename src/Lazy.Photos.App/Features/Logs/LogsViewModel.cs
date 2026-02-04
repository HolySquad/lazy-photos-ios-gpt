using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.Logs.Models;
using Lazy.Photos.App.Features.Logs.Services;
using System.Collections.ObjectModel;

namespace Lazy.Photos.App.Features.Logs;

/// <summary>
/// ViewModel for the logs page.
/// Provides log viewing with filtering and pagination.
/// </summary>
public partial class LogsViewModel : ObservableObject
{
	private readonly ILogRepository _logRepository;
	private const int PageSize = 50;

	public LogsViewModel(ILogRepository logRepository)
	{
		_logRepository = logRepository;
	}

	[ObservableProperty]
	private ObservableCollection<LogEntry> _logs = new();

	[ObservableProperty]
	private LogLevel _filterLevel = LogLevel.Debug;

	[ObservableProperty]
	private string? _filterCategory;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private long _databaseSize;

	private int _currentOffset;

	partial void OnFilterLevelChanged(LogLevel value)
	{
		// Reload logs when filter changes
		_ = LoadLogsAsync();
	}

	[RelayCommand]
	private async Task LoadLogsAsync()
	{
		if (IsLoading)
			return;

		IsLoading = true;
		try
		{
			_currentOffset = 0;
			var entries = await _logRepository.GetLogsAsync(
				PageSize,
				_currentOffset,
				FilterLevel,
				FilterCategory,
				CancellationToken.None);

			Logs.Clear();
			foreach (var entry in entries)
				Logs.Add(entry);

			_currentOffset += entries.Count;
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task LoadMoreLogsAsync()
	{
		if (IsLoading)
			return;

		IsLoading = true;
		try
		{
			var entries = await _logRepository.GetLogsAsync(
				PageSize,
				_currentOffset,
				FilterLevel,
				FilterCategory,
				CancellationToken.None);

			foreach (var entry in entries)
				Logs.Add(entry);

			_currentOffset += entries.Count;
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task ClearLogsAsync()
	{
		await _logRepository.ClearAllLogsAsync(CancellationToken.None);
		Logs.Clear();
		_currentOffset = 0;
		await RefreshDatabaseSizeAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		await LoadLogsAsync();
		await RefreshDatabaseSizeAsync();
	}

	private async Task RefreshDatabaseSizeAsync()
	{
		DatabaseSize = await _logRepository.GetLogDatabaseSizeAsync(CancellationToken.None);
	}

	public async Task OnAppearingAsync()
	{
		await LoadLogsAsync();
		await RefreshDatabaseSizeAsync();
	}
}
