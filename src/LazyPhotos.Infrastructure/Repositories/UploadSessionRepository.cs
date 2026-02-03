using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using LazyPhotos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LazyPhotos.Infrastructure.Repositories;

public class UploadSessionRepository : IUploadSessionRepository
{
	private readonly LazyPhotosDbContext _context;

	public UploadSessionRepository(LazyPhotosDbContext context)
	{
		_context = context;
	}

	public async Task<UploadSession?> GetByIdAsync(Guid id, int userId, CancellationToken cancellationToken = default)
	{
		return await _context.UploadSessions
			.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
	}

	public async Task<UploadSession> CreateAsync(UploadSession session, CancellationToken cancellationToken = default)
	{
		_context.UploadSessions.Add(session);
		await _context.SaveChangesAsync(cancellationToken);
		return session;
	}

	public async Task<UploadSession> UpdateAsync(UploadSession session, CancellationToken cancellationToken = default)
	{
		_context.UploadSessions.Update(session);
		await _context.SaveChangesAsync(cancellationToken);
		return session;
	}

	public async Task DeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default)
	{
		var session = await GetByIdAsync(id, userId, cancellationToken);
		if (session != null)
		{
			_context.UploadSessions.Remove(session);
			await _context.SaveChangesAsync(cancellationToken);
		}
	}

	public async Task<IEnumerable<UploadSession>> GetExpiredSessionsAsync(DateTime expirationTime, CancellationToken cancellationToken = default)
	{
		return await _context.UploadSessions
			.Where(s => !s.IsCompleted && s.CreatedAt < expirationTime)
			.ToListAsync(cancellationToken);
	}
}
