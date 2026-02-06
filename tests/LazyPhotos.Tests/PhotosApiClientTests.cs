using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;
using Refit;

namespace LazyPhotos.Tests;

public sealed class PhotosApiClientTests
{
	[Fact]
	public async Task RegisterAsync_SendsRegisterRequest_AndMapsResponse()
	{
		var api = new FakeLazyPhotosApi();
		var client = new PhotosApiClient(api);

		var response = await client.RegisterAsync(
			new RegisterRequest("test@example.com", "pass123", "Test User"),
			CancellationToken.None);

		Assert.NotNull(api.RegisterRequest);
		Assert.Equal("test@example.com", api.RegisterRequest!.Email);
		Assert.Equal("pass123", api.RegisterRequest!.Password);
		Assert.Equal("Test User", api.RegisterRequest!.DisplayName);
		Assert.Equal("token-123", response.AccessToken);
		Assert.Equal("token-123", response.RefreshToken);
		Assert.Equal("test@example.com", response.User.Email);
		Assert.NotEqual(Guid.Empty, response.User.Id);
		Assert.True(response.User.CreatedAt > DateTimeOffset.MinValue);
	}

	[Fact]
	public async Task GetAlbumsAsync_MapsAlbums()
	{
		var api = new FakeLazyPhotosApi();
		var client = new PhotosApiClient(api);

		api.Albums = new List<BackendAlbumDto>
		{
			new(
				Id: 12,
				UserId: 7,
				Name: "Trips",
				CoverPhotoId: 3,
				CreatedAt: new DateTime(2024, 10, 2),
				UpdatedAt: new DateTime(2024, 10, 3),
				IsDeleted: false)
		};

		var response = await client.GetAlbumsAsync(CancellationToken.None);

		Assert.Single(response.Albums);
		var album = response.Albums[0];
		Assert.Equal("Trips", album.Name);
		Assert.NotEqual(Guid.Empty, album.Id);
		Assert.NotEqual(Guid.Empty, album.UserId);
		Assert.NotNull(album.CoverPhotoId);
		Assert.False(album.IsDeleted);
		Assert.Equal(new DateTimeOffset(new DateTime(2024, 10, 2)), album.CreatedAt);
		Assert.Equal(new DateTimeOffset(new DateTime(2024, 10, 3)), album.UpdatedAt);
	}

	[Fact]
	public async Task GetAlbumPhotosAsync_MapsPhotos()
	{
		var api = new FakeLazyPhotosApi();
		var client = new PhotosApiClient(api);

		api.AlbumPhotos = new List<BackendPhotoDto>
		{
			new(
				Id: 5,
				Sha256Hash: "abc",
				OriginalFilename: "pic.jpg",
				MimeType: "image/jpeg",
				FileSize: 10,
				Width: 100,
				Height: 200,
				TakenAt: new DateTime(2024, 9, 1),
				UploadedAt: new DateTime(2024, 9, 2),
				CameraModel: "X",
				Latitude: 1.0,
				Longitude: 2.0)
		};

		var response = await client.GetAlbumPhotosAsync(Guid.NewGuid(), CancellationToken.None);

		Assert.Single(response.Photos);
		var photo = response.Photos[0];
		Assert.Equal("pic.jpg", photo.FileName);
		Assert.Equal("abc", photo.Hash);
		Assert.Equal("image/jpeg", photo.MimeType);
		Assert.Equal(100, photo.Width);
		Assert.Equal(200, photo.Height);
		Assert.True(photo.IsDeleted == false);
	}

	[Fact]
	public async Task AddPhotoToAlbumAsync_MapsIds()
	{
		var api = new FakeLazyPhotosApi();
		var client = new PhotosApiClient(api);

		var albumId = Guid.NewGuid();
		var photoId = Guid.NewGuid();

		await client.AddPhotoToAlbumAsync(albumId, new AlbumItemRequest(photoId), CancellationToken.None);

		Assert.NotNull(api.AddedPhoto);
		Assert.Equal(MapGuid(albumId), api.AddedPhoto?.AlbumId);
		Assert.Equal(MapGuid(photoId), api.AddedPhoto?.PhotoId);
	}

	[Fact]
	public async Task RemovePhotoFromAlbumAsync_MapsIds()
	{
		var api = new FakeLazyPhotosApi();
		var client = new PhotosApiClient(api);

		var albumId = Guid.NewGuid();
		var photoId = Guid.NewGuid();

		await client.RemovePhotoFromAlbumAsync(albumId, photoId, CancellationToken.None);

		Assert.NotNull(api.RemovedPhoto);
		Assert.Equal(MapGuid(albumId), api.RemovedPhoto?.AlbumId);
		Assert.Equal(MapGuid(photoId), api.RemovedPhoto?.PhotoId);
	}

	private sealed class FakeLazyPhotosApi : ILazyPhotosApi
	{
		public BackendRegisterRequest? RegisterRequest { get; private set; }
		public List<BackendAlbumDto> Albums { get; set; } = new();
		public List<BackendPhotoDto> AlbumPhotos { get; set; } = new();
		public (Guid AlbumId, Guid PhotoId)? AddedPhoto { get; private set; }
		public (Guid AlbumId, Guid PhotoId)? RemovedPhoto { get; private set; }

		public Task<BackendAuthResponse> RegisterAsync(BackendRegisterRequest request)
		{
			RegisterRequest = request;
			return Task.FromResult(new BackendAuthResponse(
				Token: "token-123",
				User: new BackendUserDto(42, request.Email, request.DisplayName)));
		}

		public Task<BackendAuthResponse> LoginAsync(BackendLoginRequest request) =>
			throw new NotImplementedException();

		public Task<BackendPagedResponse<BackendPhotoDto>> GetPhotosAsync(int page = 1, int pageSize = 30) =>
			throw new NotImplementedException();

		public Task<BackendPhotoDto> GetPhotoByIdAsync(int id) =>
			throw new NotImplementedException();

		public Task<BackendPhotoDto> UploadPhotoAsync(StreamPart file) =>
			throw new NotImplementedException();

		public Task DeletePhotoAsync(int id) =>
			throw new NotImplementedException();

		public Task<BackendAlbumListResponse> GetAlbumsAsync()
		{
			return Task.FromResult(new BackendAlbumListResponse(Albums));
		}

		public Task<BackendAlbumDto> CreateAlbumAsync(BackendAlbumCreateRequest request) =>
			throw new NotImplementedException();

		public Task<BackendAlbumDto> UpdateAlbumAsync(int id, BackendAlbumUpdateRequest request) =>
			throw new NotImplementedException();

		public Task DeleteAlbumAsync(int id) =>
			throw new NotImplementedException();

		public Task<List<BackendPhotoDto>> GetAlbumPhotosAsync(int id) =>
			Task.FromResult(AlbumPhotos);

		public Task AddPhotoToAlbumAsync(int id, BackendAlbumItemRequest request)
		{
			AddedPhoto = (ToGuid(id), ToGuid(request.PhotoId));
			return Task.CompletedTask;
		}

		public Task RemovePhotoFromAlbumAsync(int id, int photoId)
		{
			RemovedPhoto = (ToGuid(id), ToGuid(photoId));
			return Task.CompletedTask;
		}

		public Task<HealthResponse> GetHealthAsync() =>
			throw new NotImplementedException();

		public Task<BackendUploadSessionResponse> CreateUploadSessionAsync(BackendUploadSessionRequest request) =>
			throw new NotImplementedException();

		public Task UploadChunkAsync(Guid sessionId, long offset, Stream chunk) =>
			throw new NotImplementedException();

		public Task<BackendUploadCompleteResponse> CompleteUploadAsync(Guid sessionId, BackendUploadCompleteRequest request) =>
			throw new NotImplementedException();

		private static Guid ToGuid(int value)
		{
			var bytes = new byte[16];
			BitConverter.GetBytes(value).CopyTo(bytes, 0);
			return new Guid(bytes);
		}
	}

	private static Guid MapGuid(Guid guid)
	{
		var bytes = guid.ToByteArray();
		var value = BitConverter.ToInt32(bytes, 0);
		var mapped = new byte[16];
		BitConverter.GetBytes(value).CopyTo(mapped, 0);
		return new Guid(mapped);
	}
}
