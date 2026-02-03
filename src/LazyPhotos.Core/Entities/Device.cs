namespace LazyPhotos.Core.Entities;

public class Device
{
    public int Id { get; set; }
    public required string DeviceId { get; set; }  // Unique device identifier
    public required string DeviceName { get; set; }
    public required string Platform { get; set; }  // iOS, Android, Web
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastSyncAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
