using Lazy.Photos.App.Features.Photos.Models;
using Lazy.Photos.Data.Contracts;
using System.IO;

namespace Lazy.Photos.App.Features.Photos.Services;

public static class PhotoItemMapper
{
	public static PhotoItem FromDto(PhotoDto photo)
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
