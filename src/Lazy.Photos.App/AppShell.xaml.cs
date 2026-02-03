namespace Lazy.Photos.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

		// Register route-based pages
		Routing.RegisterRoute(nameof(Features.Photos.PhotoViewerPage), typeof(Features.Photos.PhotoViewerPage));
		Routing.RegisterRoute("onboarding", typeof(Features.Onboarding.OnboardingPage));
	}
}
