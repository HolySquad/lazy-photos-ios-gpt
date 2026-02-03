namespace LazyPhotos.Core.Entities;

public class Photo
{
    public int Id { get; set; }
    public required string Sha256Hash { get; set; }
    public required string StoragePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public required string OriginalFilename { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime TakenAt { get; set; }
    public DateTime UploadedAt { get; set; }

    // EXIF Metadata (nullable)
    public string? CameraModel { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public int? Iso { get; set; }
    public string? FocalLength { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation properties
    public ICollection<PhotoAlbum> PhotoAlbums { get; set; } = new List<PhotoAlbum>();
    public ICollection<PhotoTag> PhotoTags { get; set; } = new List<PhotoTag>();
}
