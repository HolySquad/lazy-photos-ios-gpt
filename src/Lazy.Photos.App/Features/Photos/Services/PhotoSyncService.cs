using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.Data;
using Lazy.Photos.Data.Contracts;
using System.IO;

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
		var response = await _apiClient.GetPhotosAsync(cursor: null, limit: maxCount, ct);
		if (response.Photos.Count == 0)
			return Array.Empty<PhotoItem>();

		var items = new List<PhotoItem>(response.Photos.Count);
		foreach (var photo in response.Photos)
			items.Add(MapPhoto(photo));

		return items;
	}

	private static PhotoItem MapPhoto(PhotoDto photo)
	{
		return new PhotoItem
		{
			Id = photo.Id.ToString(),
			DisplayName = photo.FileName,
			TakenAt = photo.CapturedAt,
			Hash = photo.Hash,
			FolderName = DeriveFolder(photo),
			Thumbnail = ToImageSource(photo.Thumbnails?.GetValueOrDefault("thumb")),
			FullImage = ToImageSource(photo.Thumbnails?.GetValueOrDefault("medium")) ?? ToImageSource(photo.DownloadUrl?.ToString()),
			IsSynced = true
		};
	}

	private static ImageSource? ToImageSource(string? uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
			return null;

		return Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
			? ImageSource.FromUri(parsed)
			: null;
	}

	private static string DeriveFolder(PhotoDto photo)
	{
		if (!string.IsNullOrWhiteSpace(photo.StorageKey))
		{
			var directory = Path.GetDirectoryName(photo.StorageKey)?.Replace('\\', '/');
			if (!string.IsNullOrWhiteSpace(directory))
			{
				var segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
				if (segments.Length > 0)
					return segments[^1];
			}
		}

		return "Cloud";
	}
}
