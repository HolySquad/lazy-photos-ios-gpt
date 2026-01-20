namespace Lazy.Photos.App.Features.DevStats;

public partial class DevStatsPage : ContentPage
{
	public DevStatsPage(DevStatsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public DevStatsPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<DevStatsViewModel>()
			?? new DevStatsViewModel(
				new Photos.Services.PhotoCacheService(Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3")),
				new Photos.Services.PhotoLibraryService(),
				new Photos.Services.PhotoCacheService(Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3"))))
	{
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is DevStatsViewModel vm)
			await vm.RefreshCommand.ExecuteAsync(null);
	}
}
