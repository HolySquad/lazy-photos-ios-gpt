namespace Lazy.Photos.App.Features.Logs.Models;

/// <summary>
/// Represents a single log entry in the application.
/// </summary>
public sealed class LogEntry
{
	public long Id { get; set; }
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
	public LogLevel Level { get; set; }
	public string Category { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public string? Exception { get; set; }
	public string? Context { get; set; }
}
