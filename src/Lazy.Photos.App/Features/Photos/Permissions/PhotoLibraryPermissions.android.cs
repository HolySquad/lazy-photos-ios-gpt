#if ANDROID
using Microsoft.Maui.ApplicationModel;

namespace Lazy.Photos.App.Features.Photos.Permissions;

public sealed class ReadMediaImagesPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[]
		{
			(Android.Manifest.Permission.ReadMediaImages, true)
		};
}

public sealed class ReadExternalStoragePermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[]
		{
			(Android.Manifest.Permission.ReadExternalStorage, true)
		};
}
#endif
