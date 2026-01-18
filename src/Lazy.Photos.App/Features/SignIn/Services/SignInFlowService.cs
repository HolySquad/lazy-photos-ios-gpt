using Lazy.Photos.App.Features.SignIn;

namespace Lazy.Photos.App.Features.SignIn.Services;

public sealed class SignInFlowService : ISignInFlowService
{
	public Task ShowSignInAsync()
	{
		return Shell.Current.GoToAsync(nameof(SignInPage), true);
	}
}
