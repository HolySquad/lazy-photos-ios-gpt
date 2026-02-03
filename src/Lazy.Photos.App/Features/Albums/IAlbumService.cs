using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Albums;

public interface IAlbumService
{
    Task<IReadOnlyList<AlbumDto>> GetAlbumsAsync(CancellationToken cancellationToken = default);
    Task<AlbumDto?> GetAlbumAsync(string id, CancellationToken cancellationToken = default);
    Task<AlbumDto> CreateAlbumAsync(string name, CancellationToken cancellationToken = default);
    Task<AlbumDto> UpdateAlbumAsync(string id, string name, CancellationToken cancellationToken = default);
    Task DeleteAlbumAsync(string id, CancellationToken cancellationToken = default);
    Task<int> GetPhotoCountAsync(string albumId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhotoItem>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default);
    Task AddPhotoToAlbumAsync(string albumId, string photoId, CancellationToken cancellationToken = default);
    Task RemovePhotoFromAlbumAsync(string albumId, string photoId, CancellationToken cancellationToken = default);
}

public sealed record AlbumDto(
    string Id,
    string Name,
    string? CoverPhotoId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
