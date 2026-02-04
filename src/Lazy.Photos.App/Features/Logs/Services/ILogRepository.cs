using Lazy.Photos.App.Features.Logs.Models;

namespace Lazy.Photos.App.Features.Logs.Services;

/// <summary>
/// SQLite persistence for log entries.
/// Single Responsibility: Log storage and retrieval.
/// </summary>
public interface ILogRepository
{
	/// <summary>
	/// Adds a log entry to the database.
	/// </summary>
	Task AddLogAsync(LogEntry entry, CancellationToken ct);

	/// <summary>
	/// Gets log entries with filtering and pagination.
	/// </summary>
	Task<IReadOnlyList<LogEntry>> GetLogsAsync(
		int limit,
		int offset,
		LogLevel? minLevel,
		string? categoryFilter,
		CancellationToken ct);

	/// <summary>
	/// Gets the total count of log entries matching the filter.
	/// </summary>
	Task<int> GetLogCountAsync(LogLevel? minLevel, string? categoryFilter, CancellationToken ct);

	/// <summary>
	/// Clears logs older than the specified number of days.
	/// </summary>
	Task ClearOldLogsAsync(int maxDays, CancellationToken ct);

	/// <summary>
	/// Clears all log entries.
	/// </summary>
	Task ClearAllLogsAsync(CancellationToken ct);

	/// <summary>
	/// Gets the size of the log database file in bytes.
	/// </summary>
	Task<long> GetLogDatabaseSizeAsync(CancellationToken ct);
}
