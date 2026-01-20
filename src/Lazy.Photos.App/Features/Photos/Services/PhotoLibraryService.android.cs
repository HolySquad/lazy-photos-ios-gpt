#if ANDROID
using Android.Content;
using Android.Provider;
using Lazy.Photos.App.Features.Photos.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class PhotoLibraryService
{
	private async partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct)
	{
		var items = new List<PhotoItem>();
		var resolver = Android.App.Application.Context.ContentResolver;
		var uri = MediaStore.Images.Media.ExternalContentUri;
		var projection = new[]
		{
			MediaStore.Images.Media.InterfaceConsts.Id,
			MediaStore.Images.Media.InterfaceConsts.DisplayName,
			MediaStore.Images.Media.InterfaceConsts.DateTaken,
			MediaStore.Images.Media.InterfaceConsts.BucketDisplayName
		};

		using var cursor = resolver.Query(uri, projection, null, null,
			$"{MediaStore.Images.Media.InterfaceConsts.DateTaken} DESC");

		if (cursor == null)
			return items;

		var idIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Id);
		var nameIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DisplayName);
		var dateIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DateTaken);
		var bucketIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.BucketDisplayName);

		while (cursor.MoveToNext() && items.Count < maxCount)
		{
			ct.ThrowIfCancellationRequested();

			var id = cursor.GetLong(idIndex);
			var name = nameIndex >= 0 ? cursor.GetString(nameIndex) : null;
			var dateTakenMs = dateIndex >= 0 ? cursor.GetLong(dateIndex) : 0;
			var bucket = bucketIndex >= 0 ? cursor.GetString(bucketIndex) : null;
			var contentUri = ContentUris.WithAppendedId(uri, id);
			var hash = await ComputeHashAsync(resolver, contentUri, ct);

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
				Hash = hash,
				FolderName = string.IsNullOrWhiteSpace(bucket) ? "Device" : bucket,
				Thumbnail = imageSource,
				FullImage = imageSource,
				IsSynced = false
			});
		}

		return items;
	}

	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		return Task.FromResult(photo.FullImage ?? photo.Thumbnail);
	}

	private static async Task<string?> ComputeHashAsync(ContentResolver resolver, Android.Net.Uri uri, CancellationToken ct)
	{
		await using var stream = resolver.OpenInputStream(uri);
		if (stream == null)
			return null;

		using var sha = SHA256.Create();
		var buffer = new byte[81920];
		int read;
		while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
			sha.TransformBlock(buffer, 0, read, null, 0);

		sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
		var hashBytes = sha.Hash;
		if (hashBytes == null)
			return null;

		var sb = new StringBuilder(hashBytes.Length * 2);
		foreach (var b in hashBytes)
			sb.Append(b.ToString("x2"));
		return sb.ToString();
	}
}
#endif
