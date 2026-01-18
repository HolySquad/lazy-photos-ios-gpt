using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lazy.Photos.App.Features.SignIn;

public partial class SignInViewModel : ObservableObject
{
	[ObservableProperty]
	private string email = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[RelayCommand]
	private Task ContinueAsync()
	{
		return Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private Task GoBackAsync()
	{
		return Shell.Current.GoToAsync("..");
	}
}
