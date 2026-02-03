using Lazy.Photos.Core.Models;

namespace Lazy.Photos.App.Services;

/// <summary>
/// Authentication state management service
/// </summary>
public interface IAuthenticationService
{
	Task<bool> IsAuthenticatedAsync();
	Task<AuthResult> LoginAsync(string email, string password);
	Task<AuthResult> RegisterAsync(string email, string password, string displayName);
	Task LogoutAsync();
	Task<string?> GetCurrentUserEmailAsync();
}

public record AuthResult(bool Success, string? ErrorMessage, User? User);
