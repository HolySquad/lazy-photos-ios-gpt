namespace LazyPhotos.Core.Entities;

public class UploadSession
{
	public Guid Id { get; set; }
	public int UserId { get; set; }
	public required string Hash { get; set; }
	public long SizeBytes { get; set; }
	public required string MimeType { get; set; }
	public DateTime? CapturedAt { get; set; }
	public int? Width { get; set; }
	public int? Height { get; set; }
	public double? LocationLat { get; set; }
	public double? LocationLon { get; set; }
	public string? StorageKey { get; set; }
	public long BytesUploaded { get; set; }
	public bool IsCompleted { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? CompletedAt { get; set; }

	// Foreign keys
	public User User { get; set; } = null!;
}
