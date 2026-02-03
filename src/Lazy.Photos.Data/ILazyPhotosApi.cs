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
	double? Longitude);

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
