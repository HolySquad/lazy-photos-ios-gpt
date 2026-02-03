namespace Lazy.Photos.App.Features.Albums;

public partial class AlbumDetailPage : ContentPage, IQueryAttributable
{
	private readonly AlbumDetailViewModel _viewModel;

	public AlbumDetailPage(AlbumDetailViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public AlbumDetailPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetRequiredService<AlbumDetailViewModel>()
			?? throw new InvalidOperationException("AlbumDetailViewModel not registered in DI container"))
	{
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("albumId", out var idValue) && idValue is string albumId)
		{
			var albumName = query.TryGetValue("albumName", out var nameValue) ? nameValue as string : null;
			_viewModel.SetAlbum(albumId, albumName);
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.RefreshCommand.ExecuteAsync(null);
	}

	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);
		if (width <= 0)
			return;

		var columns = width switch
		{
			<= 400 => 2,
			<= 700 => 3,
			<= 1100 => 4,
			_ => 5
		};

		const double horizontalPadding = 16;
		const double spacing = 4;
		var totalSpacing = (columns - 1) * spacing;
		var available = Math.Max(0, width - horizontalPadding - totalSpacing);
		var cellSize = Math.Floor(available / columns);

		_viewModel.UpdateGrid(columns, cellSize);
	}
}
