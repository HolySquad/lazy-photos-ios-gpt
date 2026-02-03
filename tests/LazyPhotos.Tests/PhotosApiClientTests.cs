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

	private sealed class FakeLazyPhotosApi : ILazyPhotosApi
	{
		public BackendRegisterRequest? RegisterRequest { get; private set; }

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

		public Task<HealthResponse> GetHealthAsync() =>
			throw new NotImplementedException();
	}
}
