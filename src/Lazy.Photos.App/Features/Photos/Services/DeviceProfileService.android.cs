#if ANDROID
namespace Lazy.Photos.App.Features.Photos.Services;

public partial class DeviceProfileService
{
	/// <summary>
	/// Detects device class based on available memory and API level.
	/// </summary>
	private partial DeviceProfile DetectProfileCore()
	{
		try
		{
			var runtime = Java.Lang.Runtime.GetRuntime();
			var maxMemory = runtime.MaxMemory();

			// Also check total device RAM via ActivityManager for more accurate classification
			var context = Android.App.Application.Context;
			var activityManager = context.GetSystemService(Android.Content.Context.ActivityService) as Android.App.ActivityManager;

			long totalDeviceRam = 0;
			if (activityManager != null)
			{
				var memInfo = new Android.App.ActivityManager.MemoryInfo();
				activityManager.GetMemoryInfo(memInfo);
				totalDeviceRam = memInfo.TotalMem;
			}

			// Classification thresholds:
			// High: 4GB+ device RAM or 384MB+ heap
			// Medium: 2GB+ device RAM or 192MB+ heap
			// Low: Everything else

			const long HighDeviceRamThreshold = 4L * 1024 * 1024 * 1024; // 4GB
			const long MediumDeviceRamThreshold = 2L * 1024 * 1024 * 1024; // 2GB
			const long HighHeapThreshold = 384L * 1024 * 1024; // 384MB heap
			const long MediumHeapThreshold = 192L * 1024 * 1024; // 192MB heap

			if (totalDeviceRam >= HighDeviceRamThreshold || maxMemory >= HighHeapThreshold)
				return DeviceProfile.HighEnd;

			if (totalDeviceRam >= MediumDeviceRamThreshold || maxMemory >= MediumHeapThreshold)
				return DeviceProfile.MidRange;

			return DeviceProfile.LowEnd;
		}
		catch
		{
			// If detection fails, default to mid-range for safety
			return DeviceProfile.MidRange;
		}
	}
}
#endif
