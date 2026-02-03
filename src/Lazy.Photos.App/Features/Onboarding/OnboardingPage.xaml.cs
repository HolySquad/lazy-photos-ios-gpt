namespace Lazy.Photos.App.Features.Onboarding;

public partial class OnboardingPage : ContentPage
{
	public OnboardingPage(OnboardingViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
