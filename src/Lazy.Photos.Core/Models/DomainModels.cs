namespace Lazy.Photos.Core.Models;

public sealed class User
{
	public Guid Id { get; set; }
	public string Email { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class Device
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Platform { get; set; } = string.Empty;
	public string Model { get; set; } = string.Empty;
	public string AppVersion { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset LastSeenAt { get; set; }
}

public sealed class Photo
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid? DeviceId { get; set; }
	public string StorageKey { get; set; } = string.Empty;
	public string Hash { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public DateTimeOffset? CapturedAt { get; set; }
	public DateTimeOffset UploadedAt { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public string MimeType { get; set; } = string.Empty;
	public double? LocationLat { get; set; }
	public double? LocationLon { get; set; }
	public string? CameraMake { get; set; }
	public string? CameraModel { get; set; }
	public bool IsDeleted { get; set; }
	public DateTimeOffset? DeletedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	public Dictionary<string, Uri>? Thumbnails { get; set; }
}

public sealed class Album
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Name { get; set; } = string.Empty;
	public Guid? CoverPhotoId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	public bool IsDeleted { get; set; }
	public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class AlbumItem
{
	public Guid AlbumId { get; set; }
	public Guid PhotoId { get; set; }
	public int Position { get; set; }
	public DateTimeOffset AddedAt { get; set; }
}

public sealed class Tag
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Label { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PhotoTag
{
	public Guid PhotoId { get; set; }
	public Guid TagId { get; set; }
}

public sealed class ShareLink
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid? AlbumId { get; set; }
	public Guid? PhotoId { get; set; }
	public string Token { get; set; } = string.Empty;
	public Uri Url { get; set; } = new("https://example.invalid");
	public DateTimeOffset? ExpiresAt { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class UploadSession
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid DeviceId { get; set; }
	public string Hash { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public int ChunkSize { get; set; }
	public string Status { get; set; } = "pending";
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? CompletedAt { get; set; }
	public string? Error { get; set; }
}

public sealed class FeedItem
{
	public FeedItemType Type { get; set; }
	public DateTimeOffset UpdatedAt { get; set; }
	public Photo? Photo { get; set; }
	public Album? Album { get; set; }
	public Tombstone? Tombstone { get; set; }
}

public sealed class Tombstone
{
	public string Entity { get; set; } = string.Empty;
	public Guid Id { get; set; }
}

public enum FeedItemType
{
	Photo,
	Album,
	Tombstone
}
