using Microsoft.Extensions.DependencyInjection;

namespace Lazy.Photos.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		var shell = IPlatformApplication.Current.Services.GetService<AppShell>();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		return new Window(shell!);
	}
}
