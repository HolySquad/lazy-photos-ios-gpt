using Lazy.Photos.App.Services;
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
		var services = IPlatformApplication.Current.Services;
		var shell = services.GetService<AppShell>();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

		var window = new Window(shell!);

		// Schedule first-launch check after window is created and shell is initialized
		// Cannot await in synchronous CreateWindow, so we use MainThread.BeginInvokeOnMainThread
		window.Created += async (s, e) =>
		{
			var settingsService = services.GetRequiredService<IAppSettingsService>();
			var isFirstLaunch = await settingsService.IsFirstLaunchAsync();

			if (isFirstLaunch)
			{
				// Navigate to onboarding using relative navigation (pushes onto existing stack)
				await Shell.Current.GoToAsync("onboarding", animate: false);
			}
		};

		return window;
	}
}
