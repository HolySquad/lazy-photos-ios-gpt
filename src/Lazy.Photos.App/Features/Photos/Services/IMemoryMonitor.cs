namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Service for monitoring memory usage and throttling operations.
/// Single Responsibility: Memory monitoring and throttle decisions.
/// Open/Closed: Platform-specific implementations can be provided.
/// </summary>
public interface IMemoryMonitor
{
	/// <summary>
	/// Checks if operations should be throttled due to memory pressure.
	/// </summary>
	bool ShouldThrottle();

	/// <summary>
	/// Gets the minimum free memory threshold in bytes before throttling.
	/// </summary>
	long ThrottleThresholdBytes { get; }
}
