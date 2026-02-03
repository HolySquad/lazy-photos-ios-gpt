namespace Lazy.Photos.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

		// Register route-based pages
		Routing.RegisterRoute(nameof(Features.Photos.PhotoViewerPage), typeof(Features.Photos.PhotoViewerPage));
		Routing.RegisterRoute(nameof(Features.Albums.AlbumDetailPage), typeof(Features.Albums.AlbumDetailPage));
		Routing.RegisterRoute(nameof(Features.Albums.AlbumPhotoPickerPage), typeof(Features.Albums.AlbumPhotoPickerPage));
		Routing.RegisterRoute("onboarding", typeof(Features.Onboarding.OnboardingPage));
	}
}
