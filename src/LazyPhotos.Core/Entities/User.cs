namespace LazyPhotos.Core.Entities;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Album> Albums { get; set; } = new List<Album>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
