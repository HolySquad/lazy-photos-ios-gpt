using System.Security.Claims;
using LazyPhotos.Core.Entities;
using LazyPhotos.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace LazyPhotos.API.Controllers;

[ApiController]
[Route("api/upload-sessions")]
[Authorize]
public class UploadSessionsController : ControllerBase
{
	private readonly IUploadSessionRepository _sessionRepository;
	private readonly IPhotoRepository _photoRepository;
	private readonly IStorageService _storageService;
	private readonly ILogger<UploadSessionsController> _logger;
	private readonly string _uploadPath;
	private const int DefaultChunkSize = 1024 * 1024; // 1MB
	private const int DefaultThumbnailSize = 300;

	public UploadSessionsController(
		IUploadSessionRepository sessionRepository,
		IPhotoRepository photoRepository,
		IStorageService storageService,
		ILogger<UploadSessionsController> logger,
		IConfiguration configuration)
	{
		_sessionRepository = sessionRepository;
		_photoRepository = photoRepository;
		_storageService = storageService;
		_logger = logger;
		_uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
	}

	[HttpPost]
	public async Task<ActionResult<UploadSessionResponse>> CreateSession([FromBody] UploadSessionRequest request)
	{
		try
		{
			var userId = GetUserId();

			// Check for deduplication - if photo with this hash already exists
			var existingPhoto = await _photoRepository.GetByHashAsync(request.Hash, userId);
			if (existingPhoto != null)
			{
				_logger.LogInformation("Photo with hash {Hash} already exists for user {UserId}", request.Hash, userId);
				return Ok(new UploadSessionResponse
				{
					UploadSessionId = Guid.Empty.ToString(),
					UploadUrl = null,
					ChunkSize = DefaultChunkSize,
					AlreadyExists = true
				});
			}

			// Create new upload session
			var session = new UploadSession
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Hash = request.Hash,
				SizeBytes = request.SizeBytes,
				MimeType = request.MimeType,
				CapturedAt = request.CapturedAt,
				Width = request.Width,
				Height = request.Height,
				LocationLat = request.LocationLat,
				LocationLon = request.LocationLon,
				BytesUploaded = 0,
				IsCompleted = false,
				CreatedAt = DateTime.UtcNow
			};

			await _sessionRepository.CreateAsync(session);

			_logger.LogInformation("Created upload session {SessionId} for user {UserId}", session.Id, userId);

			return Ok(new UploadSessionResponse
			{
				UploadSessionId = session.Id.ToString(),
				UploadUrl = null, // Client will use chunks endpoint
				ChunkSize = DefaultChunkSize,
				AlreadyExists = false
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating upload session");
			return StatusCode(500, new { error = "An error occurred while creating the upload session" });
		}
	}

	[HttpPut("{id}/chunks")]
	[RequestSizeLimit(10_000_000)] // 10MB chunk limit
	public async Task<IActionResult> UploadChunk(Guid id, [FromQuery] long offset)
	{
		try
		{
			var userId = GetUserId();
			var session = await _sessionRepository.GetByIdAsync(id, userId);

			if (session == null)
			{
				return NotFound(new { error = "Upload session not found" });
			}

			if (session.IsCompleted)
			{
				return BadRequest(new { error = "Upload session already completed" });
			}

			// Read the chunk from request body
			await using var stream = Request.Body;
			await _storageService.SaveChunkAsync(id, offset, stream);

			// Update session progress (approximate)
			var chunkSize = Request.ContentLength ?? 0;
			session.BytesUploaded += chunkSize;
			await _sessionRepository.UpdateAsync(session);

			_logger.LogDebug("Uploaded chunk at offset {Offset} for session {SessionId}", offset, id);

			return NoContent();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading chunk for session {SessionId}", id);
			return StatusCode(500, new { error = "An error occurred while uploading the chunk" });
		}
	}

	[HttpPost("{id}/complete")]
	public async Task<ActionResult<UploadCompleteResponse>> CompleteUpload(Guid id, [FromBody] UploadCompleteRequest request)
	{
		try
		{
			var userId = GetUserId();
			var session = await _sessionRepository.GetByIdAsync(id, userId);

			if (session == null)
			{
				return NotFound(new { error = "Upload session not found" });
			}

			if (session.IsCompleted)
			{
				return BadRequest(new { error = "Upload session already completed" });
			}

			// Finalize the upload by combining chunks
			var finalPath = await _storageService.FinalizeUploadAsync(id, request.StorageKey);

			// Create photo record
			var photo = new Photo
			{
				UserId = userId,
				Sha256Hash = session.Hash,
				StoragePath = request.StorageKey,
				OriginalFilename = $"{session.Hash}{GetExtensionFromMimeType(session.MimeType)}",
				MimeType = session.MimeType,
				FileSize = session.SizeBytes,
				Width = session.Width ?? 0,
				Height = session.Height ?? 0,
				TakenAt = session.CapturedAt ?? DateTime.UtcNow,
				UploadedAt = DateTime.UtcNow,
				Latitude = session.LocationLat,
				Longitude = session.LocationLon
			};

			await _photoRepository.AddAsync(photo);

			// Generate thumbnail in background (don't block the response)
			_ = Task.Run(async () =>
			{
				try
				{
					await GenerateThumbnailAsync(finalPath, session.Hash, DefaultThumbnailSize);
					_logger.LogInformation("Generated thumbnail for photo {PhotoId}", photo.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to generate thumbnail for photo {PhotoId}", photo.Id);
				}
			});

			// Mark session as completed
			session.IsCompleted = true;
			session.CompletedAt = DateTime.UtcNow;
			session.StorageKey = request.StorageKey;
			await _sessionRepository.UpdateAsync(session);

			_logger.LogInformation("Completed upload session {SessionId}, created photo {PhotoId}", id, photo.Id);

			return Ok(new UploadCompleteResponse
			{
				PhotoId = photo.Id
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error completing upload for session {SessionId}", id);
			return StatusCode(500, new { error = "An error occurred while completing the upload" });
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

	private static string GetExtensionFromMimeType(string mimeType)
	{
		return mimeType.ToLowerInvariant() switch
		{
			"image/jpeg" => ".jpg",
			"image/png" => ".png",
			"image/heic" => ".heic",
			"image/heif" => ".heif",
			"image/webp" => ".webp",
			_ => ".jpg"
		};
	}

	private async Task GenerateThumbnailAsync(string sourcePath, string hash, int size)
	{
		try
		{
			var thumbnailDir = Path.Combine(_uploadPath, "thumbnails");
			Directory.CreateDirectory(thumbnailDir);
			var thumbnailPath = Path.Combine(thumbnailDir, $"{hash}_{size}.jpg");

			// Make paths absolute if relative
			if (!Path.IsPathRooted(sourcePath))
				sourcePath = Path.GetFullPath(sourcePath);
			if (!Path.IsPathRooted(thumbnailPath))
				thumbnailPath = Path.GetFullPath(thumbnailPath);

			// Skip if thumbnail already exists
			if (System.IO.File.Exists(thumbnailPath))
			{
				_logger.LogDebug("Thumbnail already exists: {ThumbnailPath}", thumbnailPath);
				return;
			}

			using var image = await Image.LoadAsync(sourcePath);

			// Calculate aspect-preserving dimensions
			var ratio = Math.Min((double)size / image.Width, (double)size / image.Height);
			var targetWidth = (int)(image.Width * ratio);
			var targetHeight = (int)(image.Height * ratio);

			image.Mutate(x => x.Resize(targetWidth, targetHeight));

			await image.SaveAsJpegAsync(thumbnailPath, new JpegEncoder { Quality = 85 });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating thumbnail for hash {Hash}", hash);
			throw;
		}
	}
}

// DTOs
public record UploadSessionRequest
{
	public required string Hash { get; init; }
	public long SizeBytes { get; init; }
	public required string MimeType { get; init; }
	public DateTime? CapturedAt { get; init; }
	public int? Width { get; init; }
	public int? Height { get; init; }
	public double? LocationLat { get; init; }
	public double? LocationLon { get; init; }
}

public record UploadSessionResponse
{
	public required string UploadSessionId { get; init; }
	public string? UploadUrl { get; init; }
	public int ChunkSize { get; init; }
	public bool AlreadyExists { get; init; }
}

public record UploadCompleteRequest
{
	public required string StorageKey { get; init; }
}

public record UploadCompleteResponse
{
	public int PhotoId { get; init; }
}
