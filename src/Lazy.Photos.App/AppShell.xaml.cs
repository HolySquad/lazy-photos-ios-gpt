namespace Lazy.Photos.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		Routing.RegisterRoute(nameof(Features.Photos.PhotoViewerPage), typeof(Features.Photos.PhotoViewerPage));
		Routing.RegisterRoute(nameof(Features.SignIn.SignInPage), typeof(Features.SignIn.SignInPage));
	}

	public AppShell()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<AppShellViewModel>()
			?? new AppShellViewModel(new Features.SignIn.Services.NullSignInPopupService()))
	{
	}
}
