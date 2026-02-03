using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using LazyPhotos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LazyPhotos.Infrastructure.Repositories;

public class AlbumRepository : IAlbumRepository
{
    private readonly LazyPhotosDbContext _context;

    public AlbumRepository(LazyPhotosDbContext context)
    {
        _context = context;
    }

    public async Task<Album?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .Include(a => a.CoverPhoto)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<Album>> GetUserAlbumsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .Include(a => a.CoverPhoto)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default)
    {
        _context.Albums.Add(album);
        await _context.SaveChangesAsync(cancellationToken);
        return album;
    }

    public async Task<Album> UpdateAsync(Album album, CancellationToken cancellationToken = default)
    {
        _context.Albums.Update(album);
        await _context.SaveChangesAsync(cancellationToken);
        return album;
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var album = await GetByIdAsync(id, userId, cancellationToken);
        if (album != null)
        {
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddPhotoToAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default)
    {
        // Verify album belongs to user
        var album = await GetByIdAsync(albumId, userId, cancellationToken);
        if (album == null)
            throw new UnauthorizedAccessException("Album not found or access denied");

        // Verify photo belongs to user
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId, cancellationToken);
        if (photo == null)
            throw new UnauthorizedAccessException("Photo not found or access denied");

        // Check if photo is already in album
        var exists = await _context.PhotoAlbums
            .AnyAsync(pa => pa.AlbumId == albumId && pa.PhotoId == photoId, cancellationToken);
        if (exists)
            return;

        var photoAlbum = new PhotoAlbum
        {
            AlbumId = albumId,
            PhotoId = photoId,
            AddedAt = DateTime.UtcNow
        };

        _context.PhotoAlbums.Add(photoAlbum);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePhotoFromAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default)
    {
        // Verify album belongs to user
        var album = await GetByIdAsync(albumId, userId, cancellationToken);
        if (album == null)
            throw new UnauthorizedAccessException("Album not found or access denied");

        var photoAlbum = await _context.PhotoAlbums
            .FirstOrDefaultAsync(pa => pa.AlbumId == albumId && pa.PhotoId == photoId, cancellationToken);

        if (photoAlbum != null)
        {
            _context.PhotoAlbums.Remove(photoAlbum);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Photo>> GetAlbumPhotosAsync(int albumId, int userId, CancellationToken cancellationToken = default)
    {
        // Verify album belongs to user
        var album = await GetByIdAsync(albumId, userId, cancellationToken);
        if (album == null)
            throw new UnauthorizedAccessException("Album not found or access denied");

        return await _context.PhotoAlbums
            .Where(pa => pa.AlbumId == albumId)
            .Include(pa => pa.Photo)
            .Select(pa => pa.Photo)
            .OrderByDescending(p => p.TakenAt)
            .ToListAsync(cancellationToken);
    }
}
