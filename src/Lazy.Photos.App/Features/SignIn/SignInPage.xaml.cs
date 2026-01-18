namespace Lazy.Photos.App.Features.SignIn;

public partial class SignInPage : ContentPage
{
	public SignInPage(SignInViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	public SignInPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<SignInViewModel>()
			?? new SignInViewModel())
	{
	}
}
