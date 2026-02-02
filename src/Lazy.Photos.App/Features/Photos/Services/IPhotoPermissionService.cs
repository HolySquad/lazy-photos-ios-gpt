namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Service for handling photo library permissions.
/// Single Responsibility: Platform-specific permission management.
/// </summary>
public interface IPhotoPermissionService
{
	/// <summary>
	/// Requests and checks photo library permission.
	/// </summary>
	/// <returns>True if permission is granted.</returns>
	Task<bool> EnsurePhotosPermissionAsync();
}
