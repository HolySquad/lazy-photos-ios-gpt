using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using Lazy.Photos.App.Features.Photos.Permissions;
#endif

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of photo permission service.
/// Single Responsibility: Handling platform-specific photo library permissions.
/// </summary>
public sealed class PhotoPermissionService : IPhotoPermissionService
{
	public async Task<bool> EnsurePhotosPermissionAsync()
	{
#if ANDROID
		PermissionStatus status;
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			status = await MauiPermissions.CheckStatusAsync<ReadMediaImagesPermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadMediaImagesPermission>();
		}
		else
		{
			status = await MauiPermissions.CheckStatusAsync<ReadExternalStoragePermission>();
			if (status != PermissionStatus.Granted)
				status = await MauiPermissions.RequestAsync<ReadExternalStoragePermission>();
		}
#else
		var status = await MauiPermissions.CheckStatusAsync<MauiPermissions.Photos>();
		if (status != PermissionStatus.Granted)
			status = await MauiPermissions.RequestAsync<MauiPermissions.Photos>();
#endif
		return status == PermissionStatus.Granted;
	}
}
