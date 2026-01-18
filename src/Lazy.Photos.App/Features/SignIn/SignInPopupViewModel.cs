using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lazy.Photos.App.Features.SignIn;

public partial class SignInPopupViewModel : ObservableObject
{
	private readonly Services.ISignInFlowService _signInFlowService;

	public SignInPopupViewModel(Services.ISignInFlowService signInFlowService)
	{
		_signInFlowService = signInFlowService;
	}

	[RelayCommand]
	private void Close()
	{
		OnRequestClose?.Invoke();
	}

	[RelayCommand]
	private async Task SignInAsync()
	{
		OnRequestClose?.Invoke();
		await _signInFlowService.ShowSignInAsync();
	}

	[RelayCommand]
	private void OpenSettings()
	{
	}

	[RelayCommand]
	private void OpenHelp()
	{
	}

	[RelayCommand]
	private void OpenPrivacy()
	{
	}

	[RelayCommand]
	private void OpenTerms()
	{
	}

	public event Action? OnRequestClose;
}
