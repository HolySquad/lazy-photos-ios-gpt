using Refit;

namespace Lazy.Photos.Data;

/// <summary>
/// Refit interface for Lazy Photos Backend API
/// Maps directly to backend endpoints
/// </summary>
public interface ILazyPhotosApi
{
	// Authentication endpoints
	[Post("/api/auth/register")]
	Task<BackendAuthResponse> RegisterAsync([Body] BackendRegisterRequest request);

	[Post("/api/auth/login")]
	Task<BackendAuthResponse> LoginAsync([Body] BackendLoginRequest request);

	// Photo endpoints (require authentication)
	[Get("/api/photos")]
	[Headers("Authorization: Bearer")]
	Task<BackendPagedResponse<BackendPhotoDto>> GetPhotosAsync(
		[Query] int page = 1,
		[Query] int pageSize = 30);

	[Get("/api/photos/{id}")]
	[Headers("Authorization: Bearer")]
	Task<BackendPhotoDto> GetPhotoByIdAsync(int id);

	[Post("/api/photos/upload")]
	[Headers("Authorization: Bearer")]
	[Multipart]
	Task<BackendPhotoDto> UploadPhotoAsync([AliasAs("file")] StreamPart file);

	[Delete("/api/photos/{id}")]
	[Headers("Authorization: Bearer")]
	Task DeletePhotoAsync(int id);

	// Album endpoints (require authentication)
	[Get("/api/albums")]
	[Headers("Authorization: Bearer")]
	Task<BackendAlbumListResponse> GetAlbumsAsync();

	[Post("/api/albums")]
	[Headers("Authorization: Bearer")]
	Task<BackendAlbumDto> CreateAlbumAsync([Body] BackendAlbumCreateRequest request);

	[Put("/api/albums/{id}")]
	[Headers("Authorization: Bearer")]
	Task<BackendAlbumDto> UpdateAlbumAsync(int id, [Body] BackendAlbumUpdateRequest request);

	[Delete("/api/albums/{id}")]
	[Headers("Authorization: Bearer")]
	Task DeleteAlbumAsync(int id);

	[Get("/api/albums/{id}/items")]
	[Headers("Authorization: Bearer")]
	Task<List<BackendPhotoDto>> GetAlbumPhotosAsync(int id);

	[Post("/api/albums/{id}/items")]
	[Headers("Authorization: Bearer")]
	Task AddPhotoToAlbumAsync(int id, [Body] BackendAlbumItemRequest request);

	[Delete("/api/albums/{id}/items/{photoId}")]
	[Headers("Authorization: Bearer")]
	Task RemovePhotoFromAlbumAsync(int id, int photoId);

	// Upload session endpoints (chunked upload)
	[Post("/api/upload-sessions")]
	[Headers("Authorization: Bearer")]
	Task<BackendUploadSessionResponse> CreateUploadSessionAsync([Body] BackendUploadSessionRequest request);

	[Put("/api/upload-sessions/{id}/chunks")]
	[Headers("Authorization: Bearer", "Content-Type: application/octet-stream")]
	Task UploadChunkAsync(Guid id, [Query] long offset, [Body] Stream content);

	[Post("/api/upload-sessions/{id}/complete")]
	[Headers("Authorization: Bearer")]
	Task<BackendUploadCompleteResponse> CompleteUploadAsync(Guid id, [Body] BackendUploadCompleteRequest request);

	// Health check
	[Get("/health")]
	Task<HealthResponse> GetHealthAsync();
}

// Backend DTOs (match API response structure)
public record BackendAuthResponse(
	string Token,
	BackendUserDto User);

public record BackendUserDto(
	int Id,
	string Email,
	string DisplayName);

public record BackendPagedResponse<T>(
	List<T> Items,
	int Page,
	int PageSize,
	int TotalCount,
	int TotalPages);

public record BackendPhotoDto(
	int Id,
	string Sha256Hash,
	string OriginalFilename,
	string? MimeType,
	long FileSize,
	int Width,
	int Height,
	DateTime TakenAt,
	DateTime UploadedAt,
	string? CameraModel,
	double? Latitude,
	double? Longitude,
	string? DownloadUrl,
	string? ThumbnailUrl);

public record BackendAlbumListResponse(
	List<BackendAlbumDto> Albums);

public record BackendAlbumDto(
	int Id,
	int UserId,
	string Name,
	int? CoverPhotoId,
	DateTime CreatedAt,
	DateTime UpdatedAt,
	bool IsDeleted);

public record HealthResponse(
	string Status,
	DateTime Timestamp,
	string Environment);

public record BackendRegisterRequest(
	string Email,
	string Password,
	string DisplayName);

public record BackendLoginRequest(
	string Email,
	string Password);

public record BackendAlbumCreateRequest(
	string Name);

public record BackendAlbumUpdateRequest(
	string? Name,
	int? CoverPhotoId);

public record BackendAlbumItemRequest(
	int PhotoId);

public record BackendUploadSessionRequest(
	string Hash,
	long SizeBytes,
	string MimeType,
	DateTime? CapturedAt,
	int? Width,
	int? Height,
	double? LocationLat,
	double? LocationLon);

public record BackendUploadSessionResponse(
	string UploadSessionId,
	string? UploadUrl,
	int ChunkSize,
	bool AlreadyExists);

public record BackendUploadCompleteRequest(
	string StorageKey);

public record BackendUploadCompleteResponse(
	int PhotoId);
