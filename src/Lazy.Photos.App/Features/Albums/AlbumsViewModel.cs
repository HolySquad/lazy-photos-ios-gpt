using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lazy.Photos.App.Features.Albums;

public sealed partial class AlbumsViewModel : ObservableObject
{
    private readonly IAlbumService _albumService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    private ObservableCollection<AlbumDisplayItem> albums = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveAlbumCommand))]
    private string editingAlbumName = string.Empty;

    [ObservableProperty]
    private string? editingAlbumId;

    [ObservableProperty]
    private bool isEditPopupVisible;

    public bool IsEditing => EditingAlbumId is not null;
    public string EditPopupTitle => IsEditing ? "Rename Album" : "New Album";

    public AlbumsViewModel(IAlbumService albumService)
    {
        _albumService = albumService;
    }

    partial void OnEditingAlbumIdChanged(string? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(EditPopupTitle));
    }

    [RelayCommand]
    private async Task LoadAlbumsAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        try
        {
            IsLoading = Albums.Count == 0;
            ErrorMessage = null;

            var albumDtos = await _albumService.GetAlbumsAsync(ct);
            var items = new List<AlbumDisplayItem>();

            foreach (var dto in albumDtos)
            {
                var count = await _albumService.GetPhotoCountAsync(dto.Id, ct);
                items.Add(new AlbumDisplayItem
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    PhotoCount = count,
                    CoverThumbnail = null // Will be loaded from cache when implemented
                });
            }

            Albums = new ObservableCollection<AlbumDisplayItem>(items);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load albums: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadAlbumsAsync();
    }

    [RelayCommand]
    private void ShowCreateAlbumPopup()
    {
        EditingAlbumId = null;
        EditingAlbumName = string.Empty;
        IsEditPopupVisible = true;
    }

    [RelayCommand]
    private void ShowEditAlbumPopup(AlbumDisplayItem? album)
    {
        if (album is null)
            return;

        EditingAlbumId = album.Id;
        EditingAlbumName = album.Name;
        IsEditPopupVisible = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditPopupVisible = false;
        EditingAlbumId = null;
        EditingAlbumName = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSaveAlbum))]
    private async Task SaveAlbumAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingAlbumName))
            return;

        try
        {
            if (IsEditing)
            {
                await _albumService.UpdateAlbumAsync(EditingAlbumId!, EditingAlbumName);
            }
            else
            {
                await _albumService.CreateAlbumAsync(EditingAlbumName);
            }

            IsEditPopupVisible = false;
            EditingAlbumId = null;
            EditingAlbumName = string.Empty;

            await LoadAlbumsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save album: {ex.Message}";
        }
    }

    private bool CanSaveAlbum() => !string.IsNullOrWhiteSpace(EditingAlbumName);

    [RelayCommand]
    private async Task DeleteAlbumAsync(AlbumDisplayItem? album)
    {
        if (album is null)
            return;

        try
        {
            await _albumService.DeleteAlbumAsync(album.Id);
            Albums.Remove(album);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete album: {ex.Message}";
        }
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
