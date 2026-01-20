using Lazy.Photos.App.Features.Photos.Services;
using Lazy.Photos.Data;

namespace Lazy.Photos.App.Features.Photos;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public MainPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<MainPageViewModel>()
			?? new MainPageViewModel(new Services.PhotoLibraryService(), new Services.PhotoNavigationService(), new PhotoSyncService(new PhotosApiClientStub()), new Services.PhotoCacheService(Path.Combine(FileSystem.AppDataDirectory, "photos-cache.db3"))))
	{
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (BindingContext is MainPageViewModel vm)
			await vm.EnsureLoadedAsync();
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		if (width <= 0)
			return;

		var columns = width switch
		{
			<= 360 => 3,
			<= 600 => 4,
			<= 900 => 5,
			_ => 6
		};

		const double horizontalPadding = 16;
		const double spacing = 2;
		var totalSpacing = (columns - 1) * spacing;
		var available = Math.Max(0, width - horizontalPadding - totalSpacing);
		var cellSize = Math.Floor(available / columns);

		if (BindingContext is MainPageViewModel vm)
			vm.UpdateGrid(columns, cellSize);
	}
}
