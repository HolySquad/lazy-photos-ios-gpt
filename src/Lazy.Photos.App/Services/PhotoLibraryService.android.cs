using Android.Content;
using Android.Provider;
using Lazy.Photos.App.Models;
using System.IO;

namespace Lazy.Photos.App.Services;

public partial class PhotoLibraryService
{
	private partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct)
	{
		var items = new List<PhotoItem>();
		var resolver = Android.App.Application.Context.ContentResolver;
		var uri = MediaStore.Images.Media.ExternalContentUri;
		var projection = new[]
		{
			MediaStore.Images.Media.InterfaceConsts.Id,
			MediaStore.Images.Media.InterfaceConsts.DisplayName,
			MediaStore.Images.Media.InterfaceConsts.DateTaken
		};

		using var cursor = resolver.Query(uri, projection, null, null,
			$"{MediaStore.Images.Media.InterfaceConsts.DateTaken} DESC");

		if (cursor == null)
			return Task.FromResult<IReadOnlyList<PhotoItem>>(items);

		var idIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Id);
		var nameIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DisplayName);
		var dateIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DateTaken);

		while (cursor.MoveToNext() && items.Count < maxCount)
		{
			ct.ThrowIfCancellationRequested();

			var id = cursor.GetLong(idIndex);
			var name = nameIndex >= 0 ? cursor.GetString(nameIndex) : null;
			var dateTakenMs = dateIndex >= 0 ? cursor.GetLong(dateIndex) : 0;
			var contentUri = ContentUris.WithAppendedId(uri, id);

			var imageSource = ImageSource.FromStream(() =>
				resolver.OpenInputStream(contentUri) ?? Stream.Null);

			DateTimeOffset? takenAt = dateTakenMs > 0
				? DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs)
				: null;

			items.Add(new PhotoItem
			{
				Id = id.ToString(),
				DisplayName = name,
				TakenAt = takenAt,
				Thumbnail = imageSource
			});
		}

		return Task.FromResult<IReadOnlyList<PhotoItem>>(items);
	}
}
