namespace Lazy.Photos.App.Features.Logs.Models;

/// <summary>
/// Represents the severity level of a log entry.
/// </summary>
public enum LogLevel
{
	/// <summary>Detailed debug information (only in debug builds).</summary>
	Debug,

	/// <summary>Informational messages.</summary>
	Info,

	/// <summary>Warning messages.</summary>
	Warning,

	/// <summary>Error messages.</summary>
	Error
}
