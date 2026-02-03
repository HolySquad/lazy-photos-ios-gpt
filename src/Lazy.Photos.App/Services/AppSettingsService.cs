namespace Lazy.Photos.App.Services;

/// <summary>
/// Implementation of IAppSettingsService using SecureStorage for tokens and Preferences for non-sensitive settings
/// </summary>
public sealed class AppSettingsService : IAppSettingsService
{
	// Keys for storage
	private const string ApiUrlKey = "api_url";
	private const string AccessTokenKey = "access_token";
	private const string RefreshTokenKey = "refresh_token";
	private const string UserEmailKey = "user_email";
	private const string FirstLaunchCompleteKey = "first_launch_complete";

	// Default API URL for development
	private const string DefaultApiUrl = "http://localhost:5000";

	// API Configuration
	public Task<string?> GetApiUrlAsync()
	{
		var url = Preferences.Default.Get(ApiUrlKey, DefaultApiUrl);
		return Task.FromResult<string?>(url);
	}

	public Task SetApiUrlAsync(string url)
	{
		Preferences.Default.Set(ApiUrlKey, url);
		return Task.CompletedTask;
	}

	public async Task<bool> HasApiUrlConfiguredAsync()
	{
		var url = await GetApiUrlAsync();
		return !string.IsNullOrEmpty(url) && url != DefaultApiUrl;
	}

	// Authentication (using SecureStorage for encryption)
	public async Task<string?> GetAccessTokenAsync()
	{
		try
		{
			return await SecureStorage.Default.GetAsync(AccessTokenKey);
		}
		catch
		{
			// SecureStorage can throw on some platforms if not available
			return null;
		}
	}

	public async Task SetAccessTokenAsync(string token)
	{
		try
		{
			await SecureStorage.Default.SetAsync(AccessTokenKey, token);
		}
		catch
		{
			// SecureStorage can throw on some platforms if not available
		}
	}

	public async Task<string?> GetRefreshTokenAsync()
	{
		try
		{
			return await SecureStorage.Default.GetAsync(RefreshTokenKey);
		}
		catch
		{
			return null;
		}
	}

	public async Task SetRefreshTokenAsync(string token)
	{
		try
		{
			await SecureStorage.Default.SetAsync(RefreshTokenKey, token);
		}
		catch
		{
			// SecureStorage can throw on some platforms if not available
		}
	}

	public Task ClearTokensAsync()
	{
		try
		{
			SecureStorage.Default.Remove(AccessTokenKey);
			SecureStorage.Default.Remove(RefreshTokenKey);
		}
		catch
		{
			// SecureStorage can throw on some platforms if not available
		}

		return Task.CompletedTask;
	}

	// User State
	public Task<bool> IsFirstLaunchAsync()
	{
		var isComplete = Preferences.Default.Get(FirstLaunchCompleteKey, false);
		return Task.FromResult(!isComplete);
	}

	public Task SetFirstLaunchCompleteAsync()
	{
		Preferences.Default.Set(FirstLaunchCompleteKey, true);
		return Task.CompletedTask;
	}

	public Task<string?> GetUserEmailAsync()
	{
		var email = Preferences.Default.Get(UserEmailKey, string.Empty);
		return Task.FromResult<string?>(string.IsNullOrEmpty(email) ? null : email);
	}

	public Task SetUserEmailAsync(string email)
	{
		Preferences.Default.Set(UserEmailKey, email);
		return Task.CompletedTask;
	}
}
