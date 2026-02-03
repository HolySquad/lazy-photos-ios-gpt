using LazyPhotos.Core.Entities;

namespace LazyPhotos.Core.Interfaces;

public interface IPhotoRepository
{
    Task<Photo?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<Photo?> GetByHashAsync(string sha256Hash, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Photo>> GetUserPhotosAsync(int userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IEnumerable<Photo>> SearchPhotosAsync(int userId, DateTime? startDate, DateTime? endDate, string? searchTerm, CancellationToken cancellationToken = default);
    Task<Photo> AddAsync(Photo photo, CancellationToken cancellationToken = default);
    Task<Photo> UpdateAsync(Photo photo, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<int> GetUserPhotoCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByHashAsync(string sha256Hash, int userId, CancellationToken cancellationToken = default);
}
