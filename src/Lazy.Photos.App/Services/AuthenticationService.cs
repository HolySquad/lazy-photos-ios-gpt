using Lazy.Photos.Core.Models;
using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;

namespace Lazy.Photos.App.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps API client calls
/// and manages authentication state
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
	private readonly IPhotosApiClient _apiClient;
	private readonly IAppSettingsService _settingsService;

	public AuthenticationService(IPhotosApiClient apiClient, IAppSettingsService settingsService)
	{
		_apiClient = apiClient;
		_settingsService = settingsService;
	}

	public async Task<bool> IsAuthenticatedAsync()
	{
		var token = await _settingsService.GetAccessTokenAsync();
		return !string.IsNullOrEmpty(token);
	}

	public async Task<AuthResult> LoginAsync(string email, string password)
	{
		try
		{
			var request = new LoginRequest(email, password);
			var response = await _apiClient.LoginAsync(request, CancellationToken.None);

			// Store tokens
			await _settingsService.SetAccessTokenAsync(response.AccessToken);
			await _settingsService.SetRefreshTokenAsync(response.RefreshToken);
			await _settingsService.SetUserEmailAsync(email);

			return new AuthResult(true, null, response.User);
		}
		catch (Exception ex)
		{
			return new AuthResult(false, ex.Message, null);
		}
	}

	public Task<AuthResult> RegisterAsync(string email, string password, string displayName)
	{
		// TODO: Add RegisterAsync to IPhotosApiClient interface
		// For now, registration is not implemented
		return Task.FromResult(new AuthResult(false, "Registration not yet implemented in mobile client. Please use login.", null));
	}

	public async Task LogoutAsync()
	{
		await _settingsService.ClearTokensAsync();
	}

	public async Task<string?> GetCurrentUserEmailAsync()
	{
		return await _settingsService.GetUserEmailAsync();
	}
}
