using System.Security.Claims;
using System.Security.Cryptography;
using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LazyPhotos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(
        IPhotoRepository photoRepository,
        ILogger<PhotosController> logger)
    {
        _photoRepository = photoRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PhotoDto>>> GetPhotos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        try
        {
            var userId = GetUserId();
            var skip = (page - 1) * pageSize;

            var photos = await _photoRepository.GetUserPhotosAsync(userId, skip, pageSize);
            var total = await _photoRepository.GetUserPhotoCountAsync(userId);

            return Ok(new PagedResponse<PhotoDto>
            {
                Items = photos.Select(PhotoDto.FromEntity).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos");
            return StatusCode(500, new { error = "An error occurred while retrieving photos" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PhotoDto>> GetPhoto(int id)
    {
        try
        {
            var userId = GetUserId();
            var photo = await _photoRepository.GetByIdAsync(id, userId);

            if (photo == null)
                return NotFound(new { error = "Photo not found" });

            return Ok(PhotoDto.FromEntity(photo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo {PhotoId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the photo" });
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    public async Task<ActionResult<PhotoDto>> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var userId = GetUserId();

            // Compute SHA256 hash for deduplication
            string sha256Hash;
            using (var stream = file.OpenReadStream())
            {
                sha256Hash = ComputeSha256Hash(stream);
            }

            // Check if photo already exists (deduplication)
            var existing = await _photoRepository.GetByHashAsync(sha256Hash, userId);
            if (existing != null)
            {
                _logger.LogInformation("Duplicate photo detected: {Hash}", sha256Hash);
                return Ok(PhotoDto.FromEntity(existing));
            }

            // For now, create a simple storage path (will be enhanced with storage service)
            var storagePath = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{sha256Hash}{Path.GetExtension(file.FileName)}";

            var photo = new Photo
            {
                UserId = userId,
                Sha256Hash = sha256Hash,
                StoragePath = storagePath,
                OriginalFilename = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                Width = 0, // Will be populated by metadata service
                Height = 0, // Will be populated by metadata service
                TakenAt = DateTime.UtcNow, // Will be extracted from EXIF
                UploadedAt = DateTime.UtcNow
            };

            await _photoRepository.AddAsync(photo);

            _logger.LogInformation("Photo uploaded successfully: {PhotoId}", photo.Id);

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, PhotoDto.FromEntity(photo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return StatusCode(500, new { error = "An error occurred while uploading the photo" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var photo = await _photoRepository.GetByIdAsync(id, userId);

            if (photo == null)
                return NotFound(new { error = "Photo not found" });

            await _photoRepository.DeleteAsync(id, userId);

            _logger.LogInformation("Photo deleted: {PhotoId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo {PhotoId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the photo" });
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

    private static string ComputeSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

// DTOs
public record PagedResponse<T>
{
    public required List<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record PhotoDto
{
    public int Id { get; init; }
    public required string Sha256Hash { get; init; }
    public required string OriginalFilename { get; init; }
    public string? MimeType { get; init; }
    public long FileSize { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public DateTime TakenAt { get; init; }
    public DateTime UploadedAt { get; init; }
    public string? CameraModel { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    public static PhotoDto FromEntity(Photo photo) => new()
    {
        Id = photo.Id,
        Sha256Hash = photo.Sha256Hash,
        OriginalFilename = photo.OriginalFilename,
        MimeType = photo.MimeType,
        FileSize = photo.FileSize,
        Width = photo.Width,
        Height = photo.Height,
        TakenAt = photo.TakenAt,
        UploadedAt = photo.UploadedAt,
        CameraModel = photo.CameraModel,
        Latitude = photo.Latitude,
        Longitude = photo.Longitude
    };
}
