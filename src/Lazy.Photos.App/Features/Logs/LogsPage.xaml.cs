namespace Lazy.Photos.App.Features.Logs;

public partial class LogsPage : ContentPage
{
	private readonly LogsViewModel _viewModel;

	public LogsPage(LogsViewModel viewModel)
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
