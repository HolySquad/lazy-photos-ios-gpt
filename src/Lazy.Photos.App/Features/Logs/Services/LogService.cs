using Lazy.Photos.App.Features.Logs.Models;
using System.Diagnostics;

namespace Lazy.Photos.App.Features.Logs.Services;

/// <summary>
/// Application-wide logging service with SQLite persistence.
/// Uses fire-and-forget pattern for non-blocking log writes.
/// </summary>
public sealed class LogService : ILogService
{
	private readonly ILogRepository _repository;

	public LogService(ILogRepository repository)
	{
		_repository = repository;
	}

	public Task LogInfoAsync(string category, string message, string? context = null)
	{
		return LogAsync(LogLevel.Info, category, message, null, context);
	}

	public Task LogWarningAsync(string category, string message, string? context = null)
	{
		return LogAsync(LogLevel.Warning, category, message, null, context);
	}

	public Task LogErrorAsync(string category, string message, Exception? exception = null, string? context = null)
	{
		return LogAsync(LogLevel.Error, category, message, exception, context);
	}

	public Task LogDebugAsync(string category, string message, string? context = null)
	{
#if DEBUG
		return LogAsync(LogLevel.Debug, category, message, null, context);
#else
		return Task.CompletedTask;
#endif
	}

	private Task LogAsync(LogLevel level, string category, string message, Exception? exception, string? context)
	{
		var entry = new LogEntry
		{
			Timestamp = DateTimeOffset.UtcNow,
			Level = level,
			Category = category,
			Message = message,
			Exception = exception?.ToString(),
			Context = context
		};

		// Fire-and-forget for performance - logging should not block main operations
		_ = Task.Run(async () =>
		{
			try
			{
				await _repository.AddLogAsync(entry, CancellationToken.None);
			}
			catch (Exception ex)
			{
				// Swallow logging exceptions silently to prevent cascading failures
				Debug.WriteLine($"Failed to write log: {ex.Message}");
			}
		});

#if DEBUG
		// Also write to debug output for immediate visibility during development
		var levelPrefix = level switch
		{
			LogLevel.Error => "[ERROR]",
			LogLevel.Warning => "[WARN]",
			LogLevel.Info => "[INFO]",
			LogLevel.Debug => "[DEBUG]",
			_ => "[LOG]"
		};
		Debug.WriteLine($"{levelPrefix} [{category}] {message}");
		if (exception != null)
		{
			Debug.WriteLine($"  Exception: {exception.Message}");
		}
#endif

		return Task.CompletedTask;
	}
}
