namespace LazyPhotos.Core.Entities;

/// <summary>
/// Many-to-many join entity between Photo and Album
/// </summary>
public class PhotoAlbum
{
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = null!;

    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}
