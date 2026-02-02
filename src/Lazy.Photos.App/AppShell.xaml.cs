namespace Lazy.Photos.App;

public partial class AppShell : Shell
{
	public AppShell(AppShellViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		Routing.RegisterRoute(nameof(Features.Photos.PhotoViewerPage), typeof(Features.Photos.PhotoViewerPage));
	}
}
