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
		var shell = Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<AppShell>() ?? new AppShell();
		return new Window(shell);
	}
}
