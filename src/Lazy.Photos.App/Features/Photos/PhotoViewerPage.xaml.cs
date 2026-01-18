using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos;

public partial class PhotoViewerPage : ContentPage, IQueryAttributable
{
	private readonly PhotoViewerViewModel _viewModel;

	public PhotoViewerPage(PhotoViewerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	public PhotoViewerPage()
		: this(Microsoft.Maui.IPlatformApplication.Current?.Services?.GetService<PhotoViewerViewModel>()
			?? new PhotoViewerViewModel())
	{
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("photo", out var value) && value is PhotoItem photo)
			_viewModel.SetPhoto(photo);
	}
}
