namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Device performance classification for adaptive tuning.
/// </summary>
public enum DeviceClass
{
	/// <summary>Low-end devices (2GB RAM, older CPUs). Conservative settings.</summary>
	Low,

	/// <summary>Mid-range devices (3-4GB RAM). Balanced settings.</summary>
	Medium,

	/// <summary>High-end devices (6GB+ RAM, modern CPUs). Aggressive settings.</summary>
	High
}

/// <summary>
/// Performance profile containing tuning parameters for different device classes.
/// </summary>
public sealed record DeviceProfile(
	DeviceClass Class,
	int ThumbnailConcurrency,
	int LowQualityThumbnailSize,
	int HighQualityThumbnailSize,
	int ChunkSize,
	int ApplyBatchSize,
	long MemoryThresholdBytes,
	int ChunkDelayMs,
	int ScrollPauseDelayMs,
	int PriorityWindow,
	int PriorityLead,
	bool UseHighQualityBitmapConfig)
{
	/// <summary>Conservative profile for low-end devices (iPhone 8 class, 2GB RAM).</summary>
	public static DeviceProfile LowEnd { get; } = new(
		Class: DeviceClass.Low,
		ThumbnailConcurrency: 1,
		LowQualityThumbnailSize: 128,
		HighQualityThumbnailSize: 256,
		ChunkSize: 6,
		ApplyBatchSize: 10,
		MemoryThresholdBytes: 32 * 1024 * 1024,
		ChunkDelayMs: 50,
		ScrollPauseDelayMs: 50,
		PriorityWindow: 24,
		PriorityLead: 8,
		UseHighQualityBitmapConfig: false);

	/// <summary>Balanced profile for mid-range devices (3-4GB RAM).</summary>
	public static DeviceProfile MidRange { get; } = new(
		Class: DeviceClass.Medium,
		ThumbnailConcurrency: 2,
		LowQualityThumbnailSize: 192,
		HighQualityThumbnailSize: 384,
		ChunkSize: 10,
		ApplyBatchSize: 15,
		MemoryThresholdBytes: 64 * 1024 * 1024,
		ChunkDelayMs: 30,
		ScrollPauseDelayMs: 30,
		PriorityWindow: 36,
		PriorityLead: 12,
		UseHighQualityBitmapConfig: false);

	/// <summary>Aggressive profile for high-end devices (Galaxy Note 9 class, 6GB+ RAM).</summary>
	public static DeviceProfile HighEnd { get; } = new(
		Class: DeviceClass.High,
		ThumbnailConcurrency: 4,
		LowQualityThumbnailSize: 256,
		HighQualityThumbnailSize: 512,
		ChunkSize: 16,
		ApplyBatchSize: 20,
		MemoryThresholdBytes: 128 * 1024 * 1024,
		ChunkDelayMs: 10,
		ScrollPauseDelayMs: 20,
		PriorityWindow: 48,
		PriorityLead: 16,
		UseHighQualityBitmapConfig: true);
}

/// <summary>
/// Service for detecting device capabilities and providing appropriate tuning parameters.
/// Single Responsibility: Device capability detection and profile selection.
/// </summary>
public interface IDeviceProfileService
{
	/// <summary>
	/// Gets the performance profile for the current device.
	/// </summary>
	DeviceProfile GetProfile();
}
