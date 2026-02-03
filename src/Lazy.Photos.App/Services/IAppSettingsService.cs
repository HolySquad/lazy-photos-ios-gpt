namespace Lazy.Photos.App.Services;

/// <summary>
/// Persistent storage for API URL, authentication tokens, and user preferences
/// </summary>
public interface IAppSettingsService
{
	// API Configuration
	Task<string?> GetApiUrlAsync();
	Task SetApiUrlAsync(string url);
	Task<bool> HasApiUrlConfiguredAsync();

	// Authentication
	Task<string?> GetAccessTokenAsync();
	Task SetAccessTokenAsync(string token);
	Task<string?> GetRefreshTokenAsync();
	Task SetRefreshTokenAsync(string token);
	Task ClearTokensAsync();

	// User State
	Task<bool> IsFirstLaunchAsync();
	Task SetFirstLaunchCompleteAsync();
	Task<string?> GetUserEmailAsync();
	Task SetUserEmailAsync(string email);
}
