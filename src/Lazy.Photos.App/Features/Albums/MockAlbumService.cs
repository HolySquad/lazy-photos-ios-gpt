using System.Collections.Concurrent;

namespace Lazy.Photos.App.Features.Albums;

public sealed class MockAlbumService : IAlbumService
{
    private readonly ConcurrentDictionary<string, AlbumDto> _albums = new();
    private readonly ConcurrentDictionary<string, int> _photoCounts = new();

    public MockAlbumService()
    {
        // Seed with sample albums
        var sampleAlbums = new[]
        {
            CreateAlbum("Vacation 2024", 24),
            CreateAlbum("Family Photos", 156),
            CreateAlbum("Work Events", 12),
        };

        foreach (var (album, count) in sampleAlbums)
        {
            _albums[album.Id] = album;
            _photoCounts[album.Id] = count;
        }
    }

    private static (AlbumDto Album, int PhotoCount) CreateAlbum(string name, int photoCount)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        return (new AlbumDto(id, name, null, now, now), photoCount);
    }

    public Task<IReadOnlyList<AlbumDto>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simulate network delay
        var albums = _albums.Values
            .OrderByDescending(a => a.UpdatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<AlbumDto>>(albums);
    }

    public Task<AlbumDto?> GetAlbumAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _albums.TryGetValue(id, out var album);
        return Task.FromResult(album);
    }

    public Task<AlbumDto> CreateAlbumAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var album = new AlbumDto(id, name.Trim(), null, now, now);

        _albums[id] = album;
        _photoCounts[id] = 0;

        return Task.FromResult(album);
    }

    public Task<AlbumDto> UpdateAlbumAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_albums.TryGetValue(id, out var existing))
        {
            throw new InvalidOperationException($"Album with id '{id}' not found");
        }

        var updated = existing with
        {
            Name = name.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _albums[id] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAlbumAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _albums.TryRemove(id, out _);
        _photoCounts.TryRemove(id, out _);

        return Task.CompletedTask;
    }

    public Task<int> GetPhotoCountAsync(string albumId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _photoCounts.TryGetValue(albumId, out var count);
        return Task.FromResult(count);
    }
}
