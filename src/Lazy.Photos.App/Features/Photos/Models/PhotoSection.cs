using System.Collections.ObjectModel;

namespace Lazy.Photos.App.Features.Photos.Models;

public sealed class PhotoSection : ObservableCollection<PhotoItem>
{
	public PhotoSection(string title, IEnumerable<PhotoItem> items)
		: base(items)
	{
		Title = title;
	}

	public string Title { get; }
}
