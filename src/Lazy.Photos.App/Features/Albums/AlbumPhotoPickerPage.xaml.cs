namespace Lazy.Photos.App.Features.Albums;

public partial class AlbumPhotoPickerPage : ContentPage, IQueryAttributable
{
	private readonly AlbumPhotoPickerViewModel _viewModel;

	public AlbumPhotoPickerPage(AlbumPhotoPickerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public AlbumPhotoPickerPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetRequiredService<AlbumPhotoPickerViewModel>()
			?? throw new InvalidOperationException("AlbumPhotoPickerViewModel not registered in DI container"))
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
		await _viewModel.EnsureLoadedAsync();
	}
}
