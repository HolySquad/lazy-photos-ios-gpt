using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;

namespace Lazy.Photos.App.Features.Photos.Services;

public sealed class PhotoSyncService : IPhotoSyncService
{
	private readonly IPhotosApiClient _apiClient;

	public PhotoSyncService(IPhotosApiClient apiClient)
	{
		_apiClient = apiClient;
	}

	public async Task<IReadOnlyList<PhotoItem>> GetRecentAsync(int maxCount, CancellationToken ct)
	{
		var page = await GetPageAsync(cursor: null, maxCount, ct);
		return page.Items;
	}

	public async Task<PhotoPage> GetPageAsync(string? cursor, int limit, CancellationToken ct)
	{
		var response = await _apiClient.GetPhotosAsync(cursor, limit, ct);
		if (response.Photos.Count == 0)
			return new PhotoPage(Array.Empty<PhotoItem>(), response.Cursor, response.HasMore);

		var items = new List<PhotoItem>(response.Photos.Count);
		foreach (var photo in response.Photos)
			items.Add(PhotoItemMapper.FromDto(photo));

		return new PhotoPage(items, response.Cursor, response.HasMore);
	}
}
