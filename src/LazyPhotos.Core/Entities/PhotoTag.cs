namespace LazyPhotos.Core.Entities;

/// <summary>
/// Many-to-many join entity between Photo and Tag
/// </summary>
public class PhotoTag
{
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}
