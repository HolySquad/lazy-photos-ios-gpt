#if IOS
using Foundation;
using ObjCRuntime;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class DeviceProfileService
{
	/// <summary>
	/// Detects device class based on device model and available memory.
	/// iOS devices are well-known, so we can classify by model.
	/// </summary>
	private partial DeviceProfile DetectProfileCore()
	{
		try
		{
			// Get physical memory
			var physicalMemory = NSProcessInfo.ProcessInfo.PhysicalMemory;

			// Classification thresholds:
			// High: 4GB+ RAM (iPhone 11+, iPad Pro)
			// Medium: 3GB RAM (iPhone X/XS/XR, iPhone 8 Plus)
			// Low: 2GB or less (iPhone 8, older)

			const ulong HighRamThreshold = 4UL * 1024 * 1024 * 1024; // 4GB
			const ulong MediumRamThreshold = 3UL * 1024 * 1024 * 1024; // 3GB

			if (physicalMemory >= HighRamThreshold)
				return DeviceProfile.HighEnd;

			if (physicalMemory >= MediumRamThreshold)
				return DeviceProfile.MidRange;

			return DeviceProfile.LowEnd;
		}
		catch
		{
			// If detection fails, default to low-end for iPhone 8 target
			return DeviceProfile.LowEnd;
		}
	}
}
#endif
