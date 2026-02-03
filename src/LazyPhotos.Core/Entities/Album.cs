namespace LazyPhotos.Core.Entities;

public class Album
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CoverPhotoId { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation properties
    public Photo? CoverPhoto { get; set; }
    public ICollection<PhotoAlbum> PhotoAlbums { get; set; } = new List<PhotoAlbum>();
}
