using CommunityToolkit.Maui.Extensions;

namespace Lazy.Photos.App.Features.SignIn;

public partial class SignInPopup : CommunityToolkit.Maui.Views.Popup
{
	private readonly SignInPopupViewModel _viewModel;

	public SignInPopup(SignInPopupViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		_viewModel.OnRequestClose += RequestClose;
		Closed += OnPopupClosed;
	}

	public SignInPopup()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<SignInPopupViewModel>()
			?? new SignInPopupViewModel(new Services.SignInFlowService()))
	{
	}

	private void OnPopupClosed(object? sender, EventArgs e)
	{
		Closed -= OnPopupClosed;
		_viewModel.OnRequestClose -= RequestClose;
	}

	private void RequestClose()
	{
		if (Shell.Current != null)
			Shell.Current.ClosePopupAsync();
	}
}
