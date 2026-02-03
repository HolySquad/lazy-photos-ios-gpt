namespace Lazy.Photos.App.Features.Sync;

public partial class SyncPage : ContentPage
{
	private readonly SyncViewModel _viewModel;

	public SyncPage(SyncViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.OnAppearingAsync();
	}
}
