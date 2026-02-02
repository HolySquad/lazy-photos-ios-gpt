namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of memory monitoring.
/// Single Responsibility: Platform-specific memory monitoring.
/// </summary>
public sealed class MemoryMonitor : IMemoryMonitor
{
	private const long DefaultThresholdBytes = 32 * 1024 * 1024; // 32 MB

	public long ThrottleThresholdBytes => DefaultThresholdBytes;

	public bool ShouldThrottle()
	{
#if ANDROID
		try
		{
			var runtime = Java.Lang.Runtime.GetRuntime();
			var free = runtime.FreeMemory();
			var total = runtime.TotalMemory();
			var max = runtime.MaxMemory();
			var available = max - (total - free);
			return available < ThrottleThresholdBytes;
		}
		catch
		{
			return false;
		}
#else
		return false;
#endif
	}
}
