namespace Lazy.Photos.Data;

public interface IAuthTokenProvider
{
	Task<string?> GetAccessTokenAsync(CancellationToken ct);
}

public sealed class NullAuthTokenProvider : IAuthTokenProvider
{
	public Task<string?> GetAccessTokenAsync(CancellationToken ct) => Task.FromResult<string?>(null);
}

/// <summary>
/// Implementation that retrieves tokens from SecureStorage via IAppSettingsService
/// Note: This creates a dependency from Data layer to App layer, which violates Clean Architecture.
/// Consider moving IAppSettingsService to Core layer or creating an abstraction.
/// </summary>
public sealed class SecureAuthTokenProvider : IAuthTokenProvider
{
	private readonly Func<Task<string?>> _getTokenFunc;

	// Constructor accepting a function to avoid direct dependency on IAppSettingsService
	public SecureAuthTokenProvider(Func<Task<string?>> getTokenFunc)
	{
		_getTokenFunc = getTokenFunc;
	}

	public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
	{
		return await _getTokenFunc();
	}
}
