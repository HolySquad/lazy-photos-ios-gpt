using System.ComponentModel;

namespace Lazy.Photos.App.Features.Photos.Models;

public sealed class PhotoItem : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	public event EventHandler? ThumbnailReady;

	public string? Id { get; set; }
	public string? DisplayName { get; set; }
	public DateTimeOffset? TakenAt { get; set; }
	public string? Hash { get; set; }
	public string? UniqueKey { get; set; }
	public string? FolderName { get; set; }
	public double AspectRatio { get; set; } = 1.0; // width / height

	private ImageSource? _thumbnail;
	public ImageSource? Thumbnail
	{
		get => _thumbnail;
		set
		{
			if (_thumbnail == value)
				return;
			_thumbnail = value;
			OnPropertyChanged(nameof(Thumbnail));
			if (value != null)
				ThumbnailReady?.Invoke(this, EventArgs.Empty);
		}
	}

	public ImageSource? FullImage { get; set; }
	public bool IsSynced { get; set; }

	private void OnPropertyChanged(string propertyName) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
