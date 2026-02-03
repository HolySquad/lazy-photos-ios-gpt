using Lazy.Photos.Core.Models;
using Lazy.Photos.Data.Contracts;

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

	public Task<UploadSessionResponse> CreateUploadSessionAsync(UploadSessionRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Upload session not implemented yet");

	public Task UploadChunkAsync(Guid uploadSessionId, long offset, Stream content, CancellationToken ct) =>
		throw new NotImplementedException("Chunked upload not implemented yet");

	public Task<UploadCompleteResponse> CompleteUploadAsync(Guid uploadSessionId, UploadCompleteRequest request, CancellationToken ct) =>
		throw new NotImplementedException("Upload complete not implemented yet");

	public Task<AlbumListResponse> GetAlbumsAsync(CancellationToken ct) =>
		throw new NotImplementedException("Albums not implemented yet");

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
