using LazyPhotos.Core.Entities;

namespace LazyPhotos.Core.Interfaces;

public interface IUploadSessionRepository
{
	Task<UploadSession?> GetByIdAsync(Guid id, int userId, CancellationToken cancellationToken = default);
	Task<UploadSession> CreateAsync(UploadSession session, CancellationToken cancellationToken = default);
	Task<UploadSession> UpdateAsync(UploadSession session, CancellationToken cancellationToken = default);
	Task DeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default);
	Task<IEnumerable<UploadSession>> GetExpiredSessionsAsync(DateTime expirationTime, CancellationToken cancellationToken = default);
}
