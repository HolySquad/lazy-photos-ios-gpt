namespace Lazy.Photos.Data;

public sealed class PhotosApiOptions
{
	public Uri BaseAddress { get; set; } = new Uri("http://localhost:5000");
}
