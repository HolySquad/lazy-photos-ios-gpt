namespace LazyPhotos.Core.Entities;

public class SharedLink
{
    public int Id { get; set; }
    public required string Token { get; set; }  // Unique token for the shared link
    public string? Password { get; set; }  // Optional password protection
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int AccessCount { get; set; }

    // Foreign keys - can share either an album or individual photo
    public int? AlbumId { get; set; }
    public Album? Album { get; set; }

    public int? PhotoId { get; set; }
    public Photo? Photo { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
