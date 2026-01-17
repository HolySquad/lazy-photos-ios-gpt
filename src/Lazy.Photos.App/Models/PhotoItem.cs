namespace Lazy.Photos.App.Models;

public sealed class PhotoItem
{
	public string? Id { get; set; }
	public string? DisplayName { get; set; }
	public DateTimeOffset? TakenAt { get; set; }
	public ImageSource? Thumbnail { get; set; }
}
