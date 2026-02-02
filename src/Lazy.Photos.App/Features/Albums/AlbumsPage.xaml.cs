namespace Lazy.Photos.App.Features.Albums;

public partial class AlbumsPage : ContentPage
{
    public AlbumsPage(AlbumsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
