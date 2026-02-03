using Lazy.Photos.Data.Contracts;

namespace Lazy.Photos.Data;

public interface IPhotosApiClient
{
	Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
	Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
	Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
	Task<DeviceRegisterResponse> RegisterDeviceAsync(DeviceRegisterRequest request, CancellationToken ct);
	Task<ServerClaimResponse> ClaimServerAsync(ServerClaimRequest request, CancellationToken ct);
	Task<UploadSessionResponse> CreateUploadSessionAsync(UploadSessionRequest request, CancellationToken ct);
	Task UploadChunkAsync(Guid uploadSessionId, long offset, Stream content, CancellationToken ct);
	Task<UploadCompleteResponse> CompleteUploadAsync(Guid uploadSessionId, UploadCompleteRequest request, CancellationToken ct);
	Task<PhotosPageResponse> GetPhotosAsync(string? cursor, int? limit, CancellationToken ct);
	Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken ct);
	Task DeletePhotoAsync(Guid photoId, CancellationToken ct);
	Task<AlbumListResponse> GetAlbumsAsync(CancellationToken ct);
	Task<FeedResponse> GetFeedAsync(string? cursor, int? limit, CancellationToken ct);
	Task<ShareLinkResponse> CreateShareLinkAsync(ShareLinkCreateRequest request, CancellationToken ct);
	Task RevokeShareLinkAsync(Guid shareLinkId, CancellationToken ct);
	Task<ImportCreateResponse> CreateImportAsync(ImportCreateRequest request, CancellationToken ct);
	Task<ImportStatusResponse> GetImportStatusAsync(Guid importId, CancellationToken ct);
}

public sealed class PhotosApiClientStub : IPhotosApiClient
{
	public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct) =>
		Task.FromResult(new LoginResponse("stub-token", "stub-refresh", new Lazy.Photos.Core.Models.User
		{
			Id = Guid.Empty,
			Email = request.Email,
			CreatedAt = DateTimeOffset.UtcNow
		}));

	public Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct) =>
		Task.FromResult(new RegisterResponse("stub-token", "stub-refresh", new Lazy.Photos.Core.Models.User
		{
			Id = Guid.Empty,
			Email = request.Email,
			CreatedAt = DateTimeOffset.UtcNow
		}));

	public Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken ct) =>
		Task.FromResult(new RefreshResponse("stub-token"));

	public Task<DeviceRegisterResponse> RegisterDeviceAsync(DeviceRegisterRequest request, CancellationToken ct) =>
		Task.FromResult(new DeviceRegisterResponse(Guid.Empty));

	public Task<ServerClaimResponse> ClaimServerAsync(ServerClaimRequest request, CancellationToken ct) =>
		Task.FromResult(new ServerClaimResponse(Guid.Empty));

	public Task<UploadSessionResponse> CreateUploadSessionAsync(UploadSessionRequest request, CancellationToken ct) =>
		Task.FromResult(new UploadSessionResponse(Guid.NewGuid(), new Uri("https://example.invalid/upload"), 512 * 1024, false));

	public Task UploadChunkAsync(Guid uploadSessionId, long offset, Stream content, CancellationToken ct) =>
		Task.CompletedTask;

	public Task<UploadCompleteResponse> CompleteUploadAsync(Guid uploadSessionId, UploadCompleteRequest request, CancellationToken ct) =>
		Task.FromResult(new UploadCompleteResponse(Guid.NewGuid()));

	public Task<PhotosPageResponse> GetPhotosAsync(string? cursor, int? limit, CancellationToken ct) =>
		Task.FromResult(new PhotosPageResponse(cursor, false, Array.Empty<PhotoDto>()));

	public Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken ct) =>
		Task.FromResult<PhotoDto?>(null);

	public Task DeletePhotoAsync(Guid photoId, CancellationToken ct) =>
		Task.CompletedTask;

	public Task<AlbumListResponse> GetAlbumsAsync(CancellationToken ct) =>
		Task.FromResult(new AlbumListResponse(Array.Empty<AlbumDto>()));

	public Task<FeedResponse> GetFeedAsync(string? cursor, int? limit, CancellationToken ct) =>
		Task.FromResult(new FeedResponse(cursor, false, Array.Empty<FeedItemDto>()));

	public Task<ShareLinkResponse> CreateShareLinkAsync(ShareLinkCreateRequest request, CancellationToken ct) =>
		Task.FromResult(new ShareLinkResponse(new Lazy.Photos.Core.Models.ShareLink
		{
			Id = Guid.NewGuid(),
			UserId = Guid.Empty,
			AlbumId = request.AlbumId,
			PhotoId = request.PhotoId,
			Token = "stub",
			Url = new Uri("https://example.invalid/share"),
			ExpiresAt = request.ExpiresAt,
			CreatedAt = DateTimeOffset.UtcNow
		}));

	public Task RevokeShareLinkAsync(Guid shareLinkId, CancellationToken ct) =>
		Task.CompletedTask;

	public Task<ImportCreateResponse> CreateImportAsync(ImportCreateRequest request, CancellationToken ct) =>
		Task.FromResult(new ImportCreateResponse(Guid.NewGuid()));

	public Task<ImportStatusResponse> GetImportStatusAsync(Guid importId, CancellationToken ct) =>
		Task.FromResult(new ImportStatusResponse(importId, "pending", 0, 0, Array.Empty<string>()));
}
