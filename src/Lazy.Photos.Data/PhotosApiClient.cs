using Lazy.Photos.Core.Models;
using Lazy.Photos.Data.Contracts;
using Refit;

namespace Lazy.Photos.Data;

/// <summary>
/// Adapter that implements IPhotosApiClient using Refit's ILazyPhotosApi
/// Handles DTO mapping between backend and mobile formats
/// </summary>
public sealed class PhotosApiClient : IPhotosApiClient
{
	private readonly ILazyPhotosApi _api;

	public PhotosApiClient(ILazyPhotosApi api)
	{
		_api = api;
	}

	public async Task<Contracts.LoginResponse> LoginAsync(Contracts.LoginRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendLoginRequest(request.Email, request.Password);
		var response = await _api.LoginAsync(backendRequest);

		return new Contracts.LoginResponse(
			AccessToken: response.Token,
			RefreshToken: response.Token, // Backend doesn't have refresh token yet
			User: new User
			{
				Id = ConvertIntToGuid(response.User.Id),
				Email = response.User.Email,
				CreatedAt = DateTimeOffset.UtcNow
			});
	}

	public async Task<Contracts.RegisterResponse> RegisterAsync(Contracts.RegisterRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendRegisterRequest(request.Email, request.Password, request.DisplayName);
		var response = await _api.RegisterAsync(backendRequest);

		return new Contracts.RegisterResponse(
			AccessToken: response.Token,
			RefreshToken: response.Token, // Backend doesn't have refresh token yet
			User: new User
			{
				Id = ConvertIntToGuid(response.User.Id),
				Email = response.User.Email,
				CreatedAt = DateTimeOffset.UtcNow
			});
	}

	public async Task<PhotosPageResponse> GetPhotosAsync(string? cursor, int? limit, CancellationToken ct)
	{
		var page = string.IsNullOrEmpty(cursor) ? 1 : int.Parse(cursor);
		var response = await _api.GetPhotosAsync(page, limit ?? 30);

		return new PhotosPageResponse(
			Cursor: response.Page < response.TotalPages ? (response.Page + 1).ToString() : null,
			HasMore: response.Page < response.TotalPages,
			Photos: response.Items.Select(MapToPhotoDto).ToArray());
	}

	public async Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken ct)
	{
		try
		{
			var id = ConvertGuidToInt(photoId);
			var response = await _api.GetPhotoByIdAsync(id);
			return MapToPhotoDto(response);
		}
		catch
		{
			return null;
		}
	}

	public async Task DeletePhotoAsync(Guid photoId, CancellationToken ct)
	{
		var id = ConvertGuidToInt(photoId);
		await _api.DeletePhotoAsync(id);
	}

	public async Task<AlbumListResponse> GetAlbumsAsync(CancellationToken ct)
	{
		var response = await _api.GetAlbumsAsync();
		var albums = response.Albums.Select(MapToAlbumDto).ToArray();
		return new AlbumListResponse(albums);
	}

	public async Task<AlbumDto> CreateAlbumAsync(AlbumCreateRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendAlbumCreateRequest(request.Name);
		var response = await _api.CreateAlbumAsync(backendRequest);
		return MapToAlbumDto(response);
	}

	public async Task<AlbumDto> UpdateAlbumAsync(Guid albumId, AlbumUpdateRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendAlbumUpdateRequest(
			request.Name,
			request.CoverPhotoId.HasValue ? ConvertGuidToInt(request.CoverPhotoId.Value) : null);

		var id = ConvertGuidToInt(albumId);
		var response = await _api.UpdateAlbumAsync(id, backendRequest);
		return MapToAlbumDto(response);
	}

	public async Task DeleteAlbumAsync(Guid albumId, CancellationToken ct)
	{
		var id = ConvertGuidToInt(albumId);
		await _api.DeleteAlbumAsync(id);
	}

	public async Task<AlbumPhotosResponse> GetAlbumPhotosAsync(Guid albumId, CancellationToken ct)
	{
		var id = ConvertGuidToInt(albumId);
		var response = await _api.GetAlbumPhotosAsync(id);
		var photos = response.Select(MapToPhotoDto).ToArray();
		return new AlbumPhotosResponse(photos);
	}

	public async Task AddPhotoToAlbumAsync(Guid albumId, AlbumItemRequest request, CancellationToken ct)
	{
		var id = ConvertGuidToInt(albumId);
		var backendRequest = new BackendAlbumItemRequest(ConvertGuidToInt(request.PhotoId));
		await _api.AddPhotoToAlbumAsync(id, backendRequest);
	}

	public async Task RemovePhotoFromAlbumAsync(Guid albumId, Guid photoId, CancellationToken ct)
	{
		var id = ConvertGuidToInt(albumId);
		var backendPhotoId = ConvertGuidToInt(photoId);
		await _api.RemovePhotoFromAlbumAsync(id, backendPhotoId);
	}

	// DTO Mapping Methods
	private static PhotoDto MapToPhotoDto(BackendPhotoDto backend)
	{
		return new PhotoDto(
			Id: ConvertIntToGuid(backend.Id),
			UserId: Guid.Empty, // Not returned by backend
			StorageKey: backend.Sha256Hash,
			Hash: backend.Sha256Hash,
			FileName: backend.OriginalFilename,
			SizeBytes: backend.FileSize,
			CapturedAt: backend.TakenAt,
			UploadedAt: backend.UploadedAt,
			Width: backend.Width,
			Height: backend.Height,
			MimeType: backend.MimeType ?? "image/jpeg",
			LocationLat: backend.Latitude,
			LocationLon: backend.Longitude,
			CameraMake: null,
			CameraModel: backend.CameraModel,
			IsDeleted: false,
			UpdatedAt: backend.UploadedAt,
			Thumbnails: null,
			DownloadUrl: null);
	}

	private static AlbumDto MapToAlbumDto(BackendAlbumDto backend)
	{
		var updatedAt = backend.UpdatedAt == default ? backend.CreatedAt : backend.UpdatedAt;
		return new AlbumDto(
			Id: ConvertIntToGuid(backend.Id),
			UserId: ConvertIntToGuid(backend.UserId),
			Name: backend.Name,
			CoverPhotoId: ConvertNullableIntToGuid(backend.CoverPhotoId),
			CreatedAt: backend.CreatedAt,
			UpdatedAt: updatedAt,
			IsDeleted: backend.IsDeleted);
	}

	private static Guid? ConvertNullableIntToGuid(int? id)
	{
		return id.HasValue ? ConvertIntToGuid(id.Value) : null;
	}

	// ID Conversion Helpers (deterministic mapping)
	private static Guid ConvertIntToGuid(int id)
	{
		// Deterministic conversion: int -> Guid
		var bytes = new byte[16];
		BitConverter.GetBytes(id).CopyTo(bytes, 0);
		return new Guid(bytes);
	}

	private static int ConvertGuidToInt(Guid guid)
	{
		// Reverse conversion: Guid -> int
		var bytes = guid.ToByteArray();
		return BitConverter.ToInt32(bytes, 0);
	}

	// Stub implementations for not-yet-implemented endpoints
	public Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Token refresh not implemented yet");

	public Task<DeviceRegisterResponse> RegisterDeviceAsync(DeviceRegisterRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Device registration not implemented yet");

	public Task<ServerClaimResponse> ClaimServerAsync(ServerClaimRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Server claim not implemented yet");

	public async Task<UploadSessionResponse> CreateUploadSessionAsync(UploadSessionRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendUploadSessionRequest(
			Hash: request.Hash,
			SizeBytes: request.SizeBytes,
			MimeType: request.MimeType,
			CapturedAt: request.CapturedAt?.DateTime,
			Width: request.Width,
			Height: request.Height,
			LocationLat: request.LocationLat,
			LocationLon: request.LocationLon);

		var response = await _api.CreateUploadSessionAsync(backendRequest);

		Uri? uploadUrl = null;
		if (!string.IsNullOrEmpty(response.UploadUrl))
		{
			uploadUrl = new Uri(response.UploadUrl);
		}

		return new UploadSessionResponse(
			UploadSessionId: Guid.Parse(response.UploadSessionId),
			UploadUrl: uploadUrl,
			ChunkSize: response.ChunkSize,
			AlreadyExists: response.AlreadyExists);
	}

	public async Task UploadChunkAsync(Guid uploadSessionId, long offset, Stream content, CancellationToken ct)
	{
		var streamPart = new StreamPart(content, "chunk", "application/octet-stream");
		await _api.UploadChunkAsync(uploadSessionId, offset, streamPart);
	}

	public async Task<UploadCompleteResponse> CompleteUploadAsync(Guid uploadSessionId, UploadCompleteRequest request, CancellationToken ct)
	{
		var backendRequest = new BackendUploadCompleteRequest(request.StorageKey);
		var response = await _api.CompleteUploadAsync(uploadSessionId, backendRequest);

		return new UploadCompleteResponse(ConvertIntToGuid(response.PhotoId));
	}

	public Task<FeedResponse> GetFeedAsync(string? cursor, int? limit, CancellationToken ct) =>
		throw new NotImplementedException("Feed not implemented yet");

	public Task<ShareLinkResponse> CreateShareLinkAsync(ShareLinkCreateRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Share links not implemented yet");

	public Task RevokeShareLinkAsync(Guid shareLinkId, CancellationToken ct) =>
		throw new NotImplementedException("Share link revoke not implemented yet");

	public Task<ImportCreateResponse> CreateImportAsync(ImportCreateRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Import not implemented yet");

	public Task<ImportStatusResponse> GetImportStatusAsync(Guid importId, CancellationToken ct) =>
		throw new NotImplementedException("Import status not implemented yet");
}
