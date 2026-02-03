using Lazy.Photos.Core.Models;

namespace Lazy.Photos.Data.Contracts;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken, User User);

public record RegisterRequest(string Email, string Password, string DisplayName);

public record RegisterResponse(string AccessToken, string RefreshToken, User User);

public record RefreshRequest(string RefreshToken);

public record RefreshResponse(string AccessToken);

public record DeviceRegisterRequest(string Platform, string Model, string AppVersion);

public record DeviceRegisterResponse(Guid DeviceId);

public record ServerClaimRequest(string OwnerEmail, string? Code);

public record ServerClaimResponse(Guid ServerId);

public record UploadSessionRequest(
	string Hash,
	long SizeBytes,
	string MimeType,
	DateTimeOffset? CapturedAt,
	int? Width,
	int? Height,
	double? LocationLat,
	double? LocationLon);

public record UploadSessionResponse(
	Guid UploadSessionId,
	Uri UploadUrl,
	int ChunkSize,
	bool AlreadyExists);

public record UploadCompleteRequest(string StorageKey);

public record UploadCompleteResponse(Guid PhotoId);

public record PhotoMetadataPatchRequest(
	IReadOnlyList<string>? Tags,
	string? CameraMake,
	string? CameraModel);

public record AlbumCreateRequest(string Name);

public record AlbumUpdateRequest(string? Name, Guid? CoverPhotoId);

public record AlbumItemRequest(Guid PhotoId);

public record ShareLinkCreateRequest(Guid? PhotoId, Guid? AlbumId, DateTimeOffset? ExpiresAt);

public record ShareLinkResponse(ShareLink ShareLink);

public record ImportCreateRequest(string Source, string Hash);

public record ImportCreateResponse(Guid ImportJobId);

public record ImportStatusResponse(Guid Id, string Status, int Processed, int Total, IReadOnlyList<string> Errors);

public record PhotosPageResponse(string? Cursor, bool HasMore, IReadOnlyList<PhotoDto> Photos);

public record AlbumListResponse(IReadOnlyList<AlbumDto> Albums);

public record FeedResponse(string? Cursor, bool HasMore, IReadOnlyList<FeedItemDto> Items);

public record DownloadResponse(Uri Url);

public record PhotoDto(
	Guid Id,
	Guid UserId,
	string StorageKey,
	string Hash,
	string FileName,
	long SizeBytes,
	DateTimeOffset? CapturedAt,
	DateTimeOffset UploadedAt,
	int Width,
	int Height,
	string MimeType,
	double? LocationLat,
	double? LocationLon,
	string? CameraMake,
	string? CameraModel,
	bool IsDeleted,
	DateTimeOffset UpdatedAt,
	Dictionary<string, string>? Thumbnails,
	Uri? DownloadUrl);

public record AlbumDto(
	Guid Id,
	Guid UserId,
	string Name,
	Guid? CoverPhotoId,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt,
	bool IsDeleted);

public record FeedItemDto(
	string Type,
	DateTimeOffset UpdatedAt,
	PhotoDto? Photo,
	AlbumDto? Album,
	TombstoneDto? Tombstone);

public record TombstoneDto(string Entity, Guid Id);
