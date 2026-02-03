using CommunityToolkit.Maui;
using Lazy.Photos.App.Features.Albums;
using Lazy.Photos.App.Features.Logs;
using Lazy.Photos.App.Features.Logs.Services;
using Lazy.Photos.App.Features.Photos;
using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.App.Features.Photos.UseCases;
using Lazy.Photos.App.Features.Settings;
using Lazy.Photos.App.Features.Sync;
using Lazy.Photos.App.Features.Sync.Services;
using Lazy.Photos.App.Features.Sync.UseCases;
using Lazy.Photos.App.Services;
using Lazy.Photos.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Refit;

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

		// Device Profile Service (must be registered first - other services depend on it)
		builder.Services.AddSingleton<IDeviceProfileService, DeviceProfileService>();

		// App Settings and Authentication
		builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
		builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

		// Auth Token Provider with function-based injection to avoid circular dependencies
		builder.Services.AddSingleton<IAuthTokenProvider>(sp =>
		{
			var settingsService = sp.GetRequiredService<IAppSettingsService>();
			return new SecureAuthTokenProvider(() => settingsService.GetAccessTokenAsync());
		});

		// Register AuthorizationHandler as transient
		builder.Services.AddTransient<AuthorizationHandler>();

		// Configure Refit API client
		// Note: HttpClient configuration happens lazily when first requested, not at startup
		builder.Services.AddRefitClient<ILazyPhotosApi>()
			.ConfigureHttpClient((sp, client) =>
			{
				// Get API URL from Preferences (synchronous operation)
				var apiUrl = Preferences.Default.Get("api_url", "http://localhost:5000");
				client.BaseAddress = new Uri(apiUrl ?? "http://localhost:5000");
				client.Timeout = TimeSpan.FromSeconds(30);
			})
			.AddHttpMessageHandler<AuthorizationHandler>()
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				AutomaticDecompression = System.Net.DecompressionMethods.All
			});

		// Register adapter that wraps Refit client
		builder.Services.AddSingleton<IPhotosApiClient, PhotosApiClient>();

		builder.Services.AddSingleton<IPhotoLibraryService, PhotoLibraryService>();
		builder.Services.AddSingleton<IPhotoSyncService, PhotoSyncService>();
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
		// Album Services
		builder.Services.AddSingleton<IAlbumService, ApiAlbumService>();
		builder.Services.AddTransient<AlbumsViewModel>();
		builder.Services.AddTransient<AlbumsPage>();
		builder.Services.AddTransient<AlbumDetailViewModel>();
		builder.Services.AddTransient<AlbumDetailPage>();
		builder.Services.AddTransient<AlbumPhotoPickerViewModel>();
		builder.Services.AddTransient<AlbumPhotoPickerPage>();
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

		// Sync Feature
		builder.Services.AddSingleton<ISyncStateRepository>(sp =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sync-state.db3");
			return new SyncStateRepository(dbPath);
		});
		builder.Services.AddSingleton<IUploadQueueService>(sp =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sync-queue.db3");
			return new UploadQueueService(dbPath);
		});
		builder.Services.AddSingleton<ISyncOrchestrationService, SyncOrchestrationService>();
		builder.Services.AddTransient<IExecuteSyncUseCase, ExecuteSyncUseCase>();
		builder.Services.AddTransient<IPauseSyncUseCase, PauseSyncUseCase>();
		builder.Services.AddTransient<IResumeSyncUseCase, ResumeSyncUseCase>();
		builder.Services.AddTransient<ICancelSyncUseCase, CancelSyncUseCase>();
		builder.Services.AddTransient<SyncViewModel>();
		builder.Services.AddTransient<SyncPage>();

		// Logs Feature
		builder.Services.AddSingleton<ILogRepository>(sp =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "logs.db3");
			return new LogRepository(dbPath);
		});
		builder.Services.AddSingleton<ILogService, LogService>();
		builder.Services.AddTransient<LogsViewModel>();
		builder.Services.AddTransient<LogsPage>();


		builder.Services.AddSingleton<AppShellViewModel>();
		builder.Services.AddTransient<MainPageViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<PhotoViewerViewModel>();
		builder.Services.AddTransient<PhotoViewerPage>();

		// Onboarding
		builder.Services.AddTransient<Features.Onboarding.OnboardingViewModel>();
		builder.Services.AddTransient<Features.Onboarding.OnboardingPage>();

		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
