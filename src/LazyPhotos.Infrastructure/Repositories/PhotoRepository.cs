using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using LazyPhotos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LazyPhotos.Infrastructure.Repositories;

public class PhotoRepository : IPhotoRepository
{
    private readonly LazyPhotosDbContext _context;

    public PhotoRepository(LazyPhotosDbContext context)
    {
        _context = context;
    }

    public async Task<Photo?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
    }

    public async Task<Photo?> GetByHashAsync(string sha256Hash, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Photos
            .FirstOrDefaultAsync(p => p.Sha256Hash == sha256Hash && p.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<Photo>> GetUserPhotosAsync(int userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Photos
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.TakenAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Photo>> SearchPhotosAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Photos.Where(p => p.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(p => p.TakenAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.TakenAt <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.OriginalFilename.Contains(searchTerm) ||
                (p.CameraModel != null && p.CameraModel.Contains(searchTerm)));
        }

        return await query
            .OrderByDescending(p => p.TakenAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Photo> AddAsync(Photo photo, CancellationToken cancellationToken = default)
    {
        _context.Photos.Add(photo);
        await _context.SaveChangesAsync(cancellationToken);
        return photo;
    }

    public async Task<Photo> UpdateAsync(Photo photo, CancellationToken cancellationToken = default)
    {
        _context.Photos.Update(photo);
        await _context.SaveChangesAsync(cancellationToken);
        return photo;
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var photo = await GetByIdAsync(id, userId, cancellationToken);
        if (photo != null)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUserPhotoCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Photos
            .Where(p => p.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsByHashAsync(string sha256Hash, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Photos
            .AnyAsync(p => p.Sha256Hash == sha256Hash && p.UserId == userId, cancellationToken);
    }
}
