using System.Collections.ObjectModel;

namespace Lazy.Photos.App.Features.Photos.Models;

public sealed class PhotoSection : ObservableCollection<PhotoItem>
{
	public PhotoSection(string title, string? location, IEnumerable<PhotoItem> items)
		: base(items)
	{
		Title = title;
		Location = location;
	}

	public string Title { get; }
	public string? Location { get; }
}
