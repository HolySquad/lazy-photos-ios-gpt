namespace Lazy.Photos.Data;

public interface IAuthTokenProvider
{
	Task<string?> GetAccessTokenAsync(CancellationToken ct);
}

public sealed class NullAuthTokenProvider : IAuthTokenProvider
{
	public Task<string?> GetAccessTokenAsync(CancellationToken ct) => Task.FromResult<string?>(null);
}
