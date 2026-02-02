using CommunityToolkit.Maui;
using Lazy.Photos.App.Features.Albums;
using Lazy.Photos.App.Features.Photos;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Photos.UseCases;
using Lazy.Photos.App.Features.Settings;
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
				fonts.AddFont("materialdesignicons-webfont.ttf", "MaterialDesignIcons");
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
		// Album Services (Mock - will be replaced with real API)
		builder.Services.AddSingleton<IAlbumService, MockAlbumService>();
		builder.Services.AddTransient<AlbumsViewModel>();
		builder.Services.AddTransient<AlbumsPage>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddSingleton<IPhotoNavigationService, PhotoNavigationService>();

		// SOLID Architecture Services
		builder.Services.AddSingleton<IPhotoStateRepository, PhotoStateRepository>();
		builder.Services.AddSingleton<IPhotoSectionBuilder, PhotoSectionBuilder>();
		builder.Services.AddSingleton<IMemoryMonitor, MemoryMonitor>();
		builder.Services.AddSingleton<IPhotoPermissionService, PhotoPermissionService>();
		builder.Services.AddSingleton<IPaginationManager, PaginationManager>();
		builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();

		// Use Cases (Clean Architecture - Application Layer)
		builder.Services.AddSingleton<ICachePersistenceUseCase, CachePersistenceUseCase>();
		builder.Services.AddTransient<ILoadPhotosUseCase, LoadPhotosUseCase>();

		
		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<PhotoViewerViewModel>();
		builder.Services.AddTransient<PhotoViewerPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
