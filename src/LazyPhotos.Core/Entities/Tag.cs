namespace LazyPhotos.Core.Entities;

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation properties
    public ICollection<PhotoTag> PhotoTags { get; set; } = new List<PhotoTag>();
}
