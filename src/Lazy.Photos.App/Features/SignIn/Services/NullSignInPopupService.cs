namespace Lazy.Photos.App.Features.SignIn.Services;

public sealed class NullSignInPopupService : ISignInPopupService
{
	public Task ShowAsync()
	{
		return Task.CompletedTask;
	}
}
