namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of memory monitoring with device-adaptive thresholds.
/// Single Responsibility: Platform-specific memory monitoring.
/// Dependency Inversion: Uses IDeviceProfileService for adaptive thresholds.
/// </summary>
public sealed class MemoryMonitor : IMemoryMonitor
{
	private readonly IDeviceProfileService _deviceProfileService;

	public MemoryMonitor(IDeviceProfileService deviceProfileService)
	{
		_deviceProfileService = deviceProfileService;
	}

	public long ThrottleThresholdBytes => _deviceProfileService.GetProfile().MemoryThresholdBytes;

	public bool ShouldThrottle()
	{
#if IOS
		try
		{
			// Check managed memory usage on iOS
			var memoryInfo = GC.GetTotalMemory(false);
			return memoryInfo > ThrottleThresholdBytes;
		}
		catch
		{
			return false;
		}
#elif ANDROID
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
