using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lazy.Photos.App.Features.Albums;

public sealed partial class AlbumsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<AlbumDisplayItem> albums = new();

    public AlbumsViewModel()
    {
        // Albums will be loaded from API/cache when implemented
    }

    [RelayCommand]
    private async Task CreateAlbumAsync()
    {
        // TODO: Show create album dialog
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenAlbumAsync(AlbumDisplayItem? album)
    {
        if (album is null)
            return;

        // TODO: Navigate to album detail page
        await Task.CompletedTask;
    }
}

public sealed class AlbumDisplayItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int PhotoCount { get; init; }
    public ImageSource? CoverThumbnail { get; init; }
}
