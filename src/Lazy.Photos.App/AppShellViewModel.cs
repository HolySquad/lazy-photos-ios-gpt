using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lazy.Photos.App.Features.SignIn.Services;

namespace Lazy.Photos.App;

public partial class AppShellViewModel : ObservableObject
{
	private readonly ISignInPopupService _signInPopupService;

	public AppShellViewModel(ISignInPopupService signInPopupService)
	{
		_signInPopupService = signInPopupService;
	}

	[RelayCommand]
	private Task ShowSignInAsync()
	{
		return _signInPopupService.ShowAsync();
	}
}
