using CommunityToolkit.Maui.Extensions;
using Lazy.Photos.App.Features.SignIn;
using Microsoft.Extensions.DependencyInjection;

namespace Lazy.Photos.App.Features.SignIn.Services;

public sealed class SignInPopupService : ISignInPopupService
{
	private readonly IServiceProvider _serviceProvider;

	public SignInPopupService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public Task ShowAsync()
	{
		if (Shell.Current == null)
			return Task.CompletedTask;

		var popup = _serviceProvider.GetRequiredService<SignInPopup>();
		return Shell.Current.ShowPopupAsync(popup);
	}
}
