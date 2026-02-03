using LazyPhotos.Core.Entities;

namespace LazyPhotos.Core.Interfaces;

public interface IAlbumRepository
{
    Task<Album?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Album>> GetUserAlbumsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default);
    Task<Album> UpdateAsync(Album album, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task AddPhotoToAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default);
    Task RemovePhotoFromAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Photo>> GetAlbumPhotosAsync(int albumId, int userId, CancellationToken cancellationToken = default);
}
