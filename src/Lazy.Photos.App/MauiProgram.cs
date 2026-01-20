using CommunityToolkit.Maui;
using Lazy.Photos.App.Features.Photos;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.SignIn;
using Lazy.Photos.App.Features.SignIn.Services;
using Lazy.Photos.App.Features.DevStats;
using Lazy.Photos.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Lazy.Photos.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IPhotoLibraryService, PhotoLibraryService>();
		builder.Services.AddSingleton<IPhotoSyncService, PhotoSyncService>();
		builder.Services.AddSingleton<IPhotosApiClient, PhotosApiClientStub>();
		builder.Services.AddSingleton<IPhotoCacheService>(_ =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3");
			return new PhotoCacheService(dbPath);
		});
		builder.Services.AddSingleton<IPhotoCacheMaintenance>(sp =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3");
			return new PhotoCacheService(dbPath);
		});
		builder.Services.AddTransient<DevStatsViewModel>();
		builder.Services.AddTransient<DevStatsPage>();
		builder.Services.AddSingleton<IPhotoNavigationService, PhotoNavigationService>();
		builder.Services.AddSingleton<ISignInPopupService, SignInPopupService>();
		builder.Services.AddSingleton<ISignInFlowService, SignInFlowService>();
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<PhotoViewerViewModel>();
		builder.Services.AddTransient<PhotoViewerPage>();
		builder.Services.AddTransient<SignInPopupViewModel>();
		builder.Services.AddTransient<SignInPopup>();
		builder.Services.AddTransient<SignInViewModel>();
		builder.Services.AddTransient<SignInPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
