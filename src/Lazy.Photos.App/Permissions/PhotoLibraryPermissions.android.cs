using Microsoft.Maui.ApplicationModel;

namespace Lazy.Photos.App;

public sealed class ReadMediaImagesPermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[]
		{
			(Android.Manifest.Permission.ReadMediaImages, true)
		};
}

public sealed class ReadExternalStoragePermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new[]
		{
			(Android.Manifest.Permission.ReadExternalStorage, true)
		};
}
