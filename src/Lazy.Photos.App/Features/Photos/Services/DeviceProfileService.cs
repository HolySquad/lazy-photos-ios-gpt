namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Cross-platform device profile service.
/// Platform-specific implementations provide actual device detection.
/// </summary>
public partial class DeviceProfileService : IDeviceProfileService
{
	private DeviceProfile? _cachedProfile;

	public DeviceProfile GetProfile()
	{
		return _cachedProfile ??= DetectProfileCore();
	}

	/// <summary>
	/// Platform-specific profile detection.
	/// </summary>
	private partial DeviceProfile DetectProfileCore();
}
