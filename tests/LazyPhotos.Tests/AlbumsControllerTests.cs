using System.Security.Claims;
using LazyPhotos.API.Controllers;
using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LazyPhotos.Tests;

public sealed class AlbumsControllerTests
{
    [Fact]
    public async Task GetAlbums_ReturnsAlbumsForUser()
    {
        var repository = new FakeAlbumRepository();
        repository.Albums.Add(new Album
        {
            Id = 1,
            Name = "Trips",
            CreatedAt = new DateTime(2024, 10, 2),
            UserId = 5
        });

        var controller = CreateController(repository, userId: 5);

        var result = await controller.GetAlbums();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AlbumListResponse>(ok.Value);
        Assert.Single(response.Albums);
        Assert.Equal("Trips", response.Albums[0].Name);
        Assert.Equal(5, repository.LastUserId);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameMissing()
    {
        var repository = new FakeAlbumRepository();
        var controller = CreateController(repository, userId: 3);

        var result = await controller.Create(new AlbumCreateRequest(string.Empty));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(repository.Albums);
    }

    [Fact]
    public async Task Create_PersistsAlbumAndReturnsDto()
    {
        var repository = new FakeAlbumRepository();
        var controller = CreateController(repository, userId: 9);

        var result = await controller.Create(new AlbumCreateRequest("  Events "));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AlbumDto>(ok.Value);
        Assert.Equal("Events", dto.Name);
        Assert.Equal(9, dto.UserId);
        Assert.Single(repository.Albums);
    }

    [Fact]
    public async Task GetAlbumPhotos_ReturnsPhotosForUser()
    {
        var repository = new FakeAlbumRepository();
        repository.AlbumPhotos.Add(new Photo
        {
            Id = 12,
            UserId = 4,
            Sha256Hash = "hash",
            StoragePath = "uploads/pic.jpg",
            OriginalFilename = "pic.jpg",
            FileSize = 123,
            Width = 100,
            Height = 200,
            TakenAt = new DateTime(2024, 8, 1),
            UploadedAt = new DateTime(2024, 8, 2)
        });

        var controller = CreateController(repository, userId: 4);

        var result = await controller.GetAlbumPhotos(3);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var photos = Assert.IsType<List<PhotoDto>>(ok.Value);
        Assert.Single(photos);
        Assert.Equal("pic.jpg", photos[0].OriginalFilename);
        Assert.Equal(4, repository.LastUserId);
    }

    private static AlbumsController CreateController(FakeAlbumRepository repository, int userId)
    {
        var controller = new AlbumsController(repository, NullLogger<AlbumsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    private sealed class FakeAlbumRepository : IAlbumRepository
    {
        private int _nextId = 100;
        public int? LastUserId { get; private set; }
        public List<Album> Albums { get; } = new();
        public List<Photo> AlbumPhotos { get; } = new();

        public Task<Album?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            var album = Albums.FirstOrDefault(a => a.Id == id && a.UserId == userId);
            return Task.FromResult(album);
        }

        public Task<IEnumerable<Album>> GetUserAlbumsAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            var albums = Albums.Where(a => a.UserId == userId);
            return Task.FromResult<IEnumerable<Album>>(albums);
        }

        public Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default)
        {
            if (album.Id == 0)
            {
                album.Id = _nextId++;
            }

            Albums.Add(album);
            return Task.FromResult(album);
        }

        public Task<Album> UpdateAsync(Album album, CancellationToken cancellationToken = default)
        {
            var index = Albums.FindIndex(a => a.Id == album.Id);
            if (index >= 0)
            {
                Albums[index] = album;
            }
            return Task.FromResult(album);
        }

        public Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            Albums.RemoveAll(a => a.Id == id && a.UserId == userId);
            return Task.CompletedTask;
        }

        public Task AddPhotoToAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemovePhotoFromAlbumAsync(int albumId, int photoId, int userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IEnumerable<Photo>> GetAlbumPhotosAsync(int albumId, int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult<IEnumerable<Photo>>(AlbumPhotos.Where(p => p.UserId == userId));
        }
    }
}
