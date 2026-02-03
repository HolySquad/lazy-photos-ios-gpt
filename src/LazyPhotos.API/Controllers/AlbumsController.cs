using System.Security.Claims;
using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyPhotos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlbumsController : ControllerBase
{
    private readonly IAlbumRepository _albumRepository;
    private readonly ILogger<AlbumsController> _logger;

    public AlbumsController(
        IAlbumRepository albumRepository,
        ILogger<AlbumsController> logger)
    {
        _albumRepository = albumRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<AlbumListResponse>> GetAlbums()
    {
        try
        {
            var userId = GetUserId();
            var albums = await _albumRepository.GetUserAlbumsAsync(userId);

            return Ok(new AlbumListResponse
            {
                Albums = albums.Select(AlbumDto.FromEntity).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving albums");
            return StatusCode(500, new { error = "An error occurred while retrieving albums" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AlbumDto>> Create([FromBody] AlbumCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Album name is required" });

        try
        {
            var userId = GetUserId();
            var now = DateTime.UtcNow;

            var album = new Album
            {
                UserId = userId,
                Name = request.Name.Trim(),
                CreatedAt = now
            };

            var created = await _albumRepository.CreateAsync(album);
            return Ok(AlbumDto.FromEntity(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating album");
            return StatusCode(500, new { error = "An error occurred while creating the album" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AlbumDto>> Update(int id, [FromBody] AlbumUpdateRequest request)
    {
        try
        {
            var userId = GetUserId();
            var album = await _albumRepository.GetByIdAsync(id, userId);

            if (album == null)
                return NotFound(new { error = "Album not found" });

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                album.Name = request.Name.Trim();
            }

            if (request.CoverPhotoId.HasValue)
            {
                album.CoverPhotoId = request.CoverPhotoId;
            }

            var updated = await _albumRepository.UpdateAsync(album);
            return Ok(AlbumDto.FromEntity(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating album {AlbumId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the album" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var album = await _albumRepository.GetByIdAsync(id, userId);

            if (album == null)
                return NotFound(new { error = "Album not found" });

            await _albumRepository.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting album {AlbumId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the album" });
        }
    }

    [HttpGet("{id}/items")]
    public async Task<ActionResult<List<PhotoDto>>> GetAlbumPhotos(int id)
    {
        try
        {
            var userId = GetUserId();
            var photos = await _albumRepository.GetAlbumPhotosAsync(id, userId);
            var items = photos.Select(PhotoDto.FromEntity).ToList();
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { error = "Album not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving album photos {AlbumId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving album photos" });
        }
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddPhotoToAlbum(int id, [FromBody] AlbumItemRequest request)
    {
        if (request.PhotoId <= 0)
            return BadRequest(new { error = "Photo ID is required" });

        try
        {
            var userId = GetUserId();
            await _albumRepository.AddPhotoToAlbumAsync(id, request.PhotoId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { error = "Album or photo not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding photo {PhotoId} to album {AlbumId}", request.PhotoId, id);
            return StatusCode(500, new { error = "An error occurred while updating the album" });
        }
    }

    [HttpDelete("{id}/items/{photoId}")]
    public async Task<IActionResult> RemovePhotoFromAlbum(int id, int photoId)
    {
        if (photoId <= 0)
            return BadRequest(new { error = "Photo ID is required" });

        try
        {
            var userId = GetUserId();
            await _albumRepository.RemovePhotoFromAlbumAsync(id, photoId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { error = "Album or photo not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo {PhotoId} from album {AlbumId}", photoId, id);
            return StatusCode(500, new { error = "An error occurred while updating the album" });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}

public record AlbumListResponse
{
    public required List<AlbumDto> Albums { get; init; }
}

public record AlbumCreateRequest(string Name);

public record AlbumUpdateRequest(string? Name, int? CoverPhotoId);

public record AlbumItemRequest(int PhotoId);

public record AlbumDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public required string Name { get; init; }
    public int? CoverPhotoId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }

    public static AlbumDto FromEntity(Album album) => new()
    {
        Id = album.Id,
        UserId = album.UserId,
        Name = album.Name,
        CoverPhotoId = album.CoverPhotoId,
        CreatedAt = album.CreatedAt,
        UpdatedAt = album.CreatedAt,
        IsDeleted = false
    };
}
