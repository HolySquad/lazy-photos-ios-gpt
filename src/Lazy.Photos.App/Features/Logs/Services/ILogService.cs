namespace Lazy.Photos.App.Features.Logs.Services;

/// <summary>
/// Application-wide logging service.
/// Single Responsibility: Provide logging API with categorization.
/// </summary>
public interface ILogService
{
	/// <summary>
	/// Logs an informational message.
	/// </summary>
	Task LogInfoAsync(string category, string message, string? context = null);

	/// <summary>
	/// Logs a warning message.
	/// </summary>
	Task LogWarningAsync(string category, string message, string? context = null);

	/// <summary>
	/// Logs an error message with optional exception.
	/// </summary>
	Task LogErrorAsync(string category, string message, Exception? exception = null, string? context = null);

	/// <summary>
	/// Logs a debug message (only in debug builds).
	/// </summary>
	Task LogDebugAsync(string category, string message, string? context = null);
}
