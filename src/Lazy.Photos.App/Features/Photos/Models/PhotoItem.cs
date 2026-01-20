namespace Lazy.Photos.App.Features.Photos.Models;

public sealed class PhotoItem
{
	public string? Id { get; set; }
	public string? DisplayName { get; set; }
	public DateTimeOffset? TakenAt { get; set; }
	public string? Hash { get; set; }
	public string? FolderName { get; set; }
	public ImageSource? Thumbnail { get; set; }
	public ImageSource? FullImage { get; set; }
	public bool IsSynced { get; set; }
}
