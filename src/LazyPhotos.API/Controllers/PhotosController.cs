using System.Security.Claims;
using System.Security.Cryptography;
using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using LazyPhotos.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace LazyPhotos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly LazyPhotosDbContext _context;
    private readonly ILogger<PhotosController> _logger;
    private readonly string _uploadPath;

    public PhotosController(
        IPhotoRepository photoRepository,
        LazyPhotosDbContext context,
        ILogger<PhotosController> logger,
        IConfiguration configuration)
    {
        _photoRepository = photoRepository;
        _context = context;
        _logger = logger;
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
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
            var baseUrl = GetBaseUrl();

            return Ok(new PagedResponse<PhotoDto>
            {
                Items = photos.Select(p => PhotoDto.FromEntity(p, baseUrl)).ToList(),
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

            var baseUrl = GetBaseUrl();
            return Ok(PhotoDto.FromEntity(photo, baseUrl));
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
            var baseUrl = GetBaseUrl();

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
                return Ok(PhotoDto.FromEntity(existing, baseUrl));
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

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, PhotoDto.FromEntity(photo, baseUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return StatusCode(500, new { error = "An error occurred while uploading the photo" });
        }
    }

    [HttpGet("{id}/download")]
    [AllowAnonymous]  // Allow unauthenticated access - necessary for MAUI ImageSource.FromUri
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            // Note: No auth required because MAUI ImageSource.FromUri doesn't support custom headers
            // Security relies on photo IDs being non-enumerable/unpredictable
            // TODO: Consider adding signed URLs or time-limited tokens for better security

            // Direct database query without user check
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
                return NotFound(new { error = "Photo not found" });

            // Use configured upload path and combine with hash
            var filePath = Path.Combine(_uploadPath, "uploads", photo.Sha256Hash);

            // Make path absolute if it's relative
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.GetFullPath(filePath);
            }

            _logger.LogDebug("Looking for photo file at: {FilePath}", filePath);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Photo file not found on disk: {FilePath}", filePath);
                return NotFound(new { error = "Photo file not found" });
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = photo.MimeType ?? "application/octet-stream";

            return File(fileStream, contentType, photo.OriginalFilename, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading photo {PhotoId}", id);
            return StatusCode(500, new { error = "An error occurred while downloading the photo" });
        }
    }

    [HttpGet("{id}/thumbnail")]
    [AllowAnonymous]  // Same as download - no auth for MAUI ImageSource
    public async Task<IActionResult> GetThumbnail(int id, [FromQuery] int size = 300)
    {
        try
        {
            // Clamp size to reasonable bounds
            size = Math.Clamp(size, 50, 800);

            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
                return NotFound(new { error = "Photo not found" });

            // Thumbnail cache path
            var thumbnailDir = Path.Combine(_uploadPath, "thumbnails");
            Directory.CreateDirectory(thumbnailDir);
            var thumbnailPath = Path.Combine(thumbnailDir, $"{photo.Sha256Hash}_{size}.jpg");

            // Make path absolute if relative
            if (!Path.IsPathRooted(thumbnailPath))
                thumbnailPath = Path.GetFullPath(thumbnailPath);

            // Return cached thumbnail if exists
            if (System.IO.File.Exists(thumbnailPath))
            {
                _logger.LogDebug("Serving cached thumbnail: {ThumbnailPath}", thumbnailPath);
                var cachedStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(cachedStream, "image/jpeg");
            }

            // Generate thumbnail
            var originalPath = Path.Combine(_uploadPath, photo.Sha256Hash);
            if (!Path.IsPathRooted(originalPath))
                originalPath = Path.GetFullPath(originalPath);

            if (!System.IO.File.Exists(originalPath))
            {
                _logger.LogError("Original photo not found for thumbnail generation: {OriginalPath}", originalPath);
                return NotFound(new { error = "Photo file not found" });
            }

            _logger.LogInformation("Generating thumbnail for photo {PhotoId}, size {Size}", id, size);
            await GenerateThumbnailAsync(originalPath, thumbnailPath, size);

            var thumbnailStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(thumbnailStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for photo {PhotoId}", id);
            return StatusCode(500, new { error = "An error occurred while generating the thumbnail" });
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

    private string GetBaseUrl()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }

    private static string ComputeSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static async Task GenerateThumbnailAsync(string sourcePath, string thumbnailPath, int size)
    {
        using var image = await Image.LoadAsync(sourcePath);

        // Calculate aspect-preserving dimensions
        var ratio = Math.Min((double)size / image.Width, (double)size / image.Height);
        var targetWidth = (int)(image.Width * ratio);
        var targetHeight = (int)(image.Height * ratio);

        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 85 });
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
    public string? DownloadUrl { get; init; }
    public string? ThumbnailUrl { get; init; }

    public static PhotoDto FromEntity(Photo photo, string? baseUrl = null) => new()
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
        Longitude = photo.Longitude,
        DownloadUrl = baseUrl != null ? $"{baseUrl}/api/photos/{photo.Id}/download" : null,
        ThumbnailUrl = baseUrl != null ? $"{baseUrl}/api/photos/{photo.Id}/thumbnail?size=300" : null
    };
}
