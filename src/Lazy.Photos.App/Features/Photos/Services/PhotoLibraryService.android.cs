#if ANDROID
using Android.Content;
using Android.Provider;
using Lazy.Photos.App.Features.Photos.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Android.Graphics;
using Microsoft.Maui.Storage;
using System.Buffers;
using IOPath = System.IO.Path;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class PhotoLibraryService
{
	private static readonly char[] InvalidFileNameChars = IOPath.GetInvalidFileNameChars();

	private static string EnsureThumbCacheDirectory()
	{
		var dir = IOPath.Combine(FileSystem.AppDataDirectory, "thumbcache");
		if (!Directory.Exists(dir))
			Directory.CreateDirectory(dir);
		return dir;
	}

	private static string GetThumbnailCachePath(PhotoItem photo, bool lowQuality)
	{
		var safeId = SanitizeFileName(photo.Id ?? "photo");
		var stamp = photo.TakenAt?.UtcTicks ?? 0;
		var suffix = lowQuality ? "_lq" : "_hq";
		return IOPath.Combine(EnsureThumbCacheDirectory(), $"{safeId}_{stamp}{suffix}.jpg");
	}

	private static string SanitizeFileName(string value)
	{
		var sb = new StringBuilder(value.Length);
		foreach (var ch in value)
		{
			sb.Append(Array.IndexOf(InvalidFileNameChars, ch) >= 0 ? '_' : ch);
		}
		return sb.ToString();
	}

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

			DateTimeOffset? takenAt = dateTakenMs > 0
				? DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs)
				: null;

			items.Add(new PhotoItem
			{
				Id = id.ToString(),
				DisplayName = name,
				TakenAt = takenAt,
				Hash = null,
				FolderName = string.IsNullOrWhiteSpace(bucket) ? "Device" : bucket,
				Thumbnail = null,
				FullImage = null,
				IsSynced = false
			});
		}

		return items;
	}

	private async partial IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsyncCore(int maxCount, CancellationToken ct)
	{
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
			yield break;

		var idIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Id);
		var nameIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DisplayName);
		var dateIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.DateTaken);
		var bucketIndex = cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.BucketDisplayName);

		while (cursor.MoveToNext() && maxCount-- > 0)
		{
			ct.ThrowIfCancellationRequested();

			var id = cursor.GetLong(idIndex);
			var name = nameIndex >= 0 ? cursor.GetString(nameIndex) : null;
			var dateTakenMs = dateIndex >= 0 ? cursor.GetLong(dateIndex) : 0;
			var bucket = bucketIndex >= 0 ? cursor.GetString(bucketIndex) : null;
			var contentUri = ContentUris.WithAppendedId(uri, id);

			DateTimeOffset? takenAt = dateTakenMs > 0
				? DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs)
				: null;

			yield return new PhotoItem
			{
				Id = id.ToString(),
				DisplayName = name,
				TakenAt = takenAt,
				Hash = null,
				FolderName = string.IsNullOrWhiteSpace(bucket) ? "Device" : bucket,
				Thumbnail = null,
				FullImage = null,
				IsSynced = false
			};
		}
	}

	private partial async Task<string?> ComputeHashAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (photo?.Id == null)
			return null;

		var resolver = Android.App.Application.Context.ContentResolver;
		var uri = MediaStore.Images.Media.ExternalContentUri;
		var contentUri = ContentUris.WithAppendedId(uri, long.Parse(photo.Id));
		return await ComputeHashAsync(resolver, contentUri, ct).ConfigureAwait(false);
	}

	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (photo?.FullImage != null)
			return Task.FromResult(photo.FullImage);

		if (photo?.Id == null)
			return Task.FromResult<ImageSource?>(null);

		var resolver = Android.App.Application.Context.ContentResolver;
		var uri = MediaStore.Images.Media.ExternalContentUri;
		var contentUri = ContentUris.WithAppendedId(uri, long.Parse(photo.Id));

		return Task.FromResult<ImageSource?>(ImageSource.FromStream(() =>
			resolver.OpenInputStream(contentUri) ?? Stream.Null));
	}

	private partial Task<ImageSource?> BuildThumbnailAsyncCore(PhotoItem photo, bool lowQuality, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (photo?.Id == null)
			return Task.FromResult<ImageSource?>(null);

		var resolver = Android.App.Application.Context.ContentResolver;
		var uri = MediaStore.Images.Media.ExternalContentUri;
		var contentUri = ContentUris.WithAppendedId(uri, long.Parse(photo.Id));

		var cachePath = GetThumbnailCachePath(photo, lowQuality);
		if (File.Exists(cachePath))
			return Task.FromResult<ImageSource?>(ImageSource.FromFile(cachePath));

		return Task.Run(() =>
		{
			var targetSize = lowQuality ? 128 : 512;
			var quality = lowQuality ? 50 : 90;

			try
			{
				var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
				using (var s = resolver.OpenInputStream(contentUri))
				{
					if (s == null)
						return (ImageSource?)null;
					BitmapFactory.DecodeStream(s, null, bounds);
				}

				int sample = 1;
				while ((bounds.OutWidth / sample) > targetSize || (bounds.OutHeight / sample) > targetSize)
					sample *= 2;

				var decodeOptions = new BitmapFactory.Options
				{
					InSampleSize = sample,
#pragma warning disable CA1422 // InDither obsolete, still okay for legacy downsampling
					InDither = true,
#pragma warning restore CA1422
					InPreferredConfig = Bitmap.Config.Rgb565
				};

				using var stream = resolver.OpenInputStream(contentUri);
				if (stream == null)
					return (ImageSource?)null;

				using var bitmap = BitmapFactory.DecodeStream(stream, null, decodeOptions);
				if (bitmap == null)
					return (ImageSource?)null;

				if (bitmap.Width > 0 && bitmap.Height > 0)
					photo.AspectRatio = Math.Max(0.1, (double)bitmap.Width / bitmap.Height);

				using (var fs = File.Open(cachePath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					bitmap.Compress(Bitmap.CompressFormat.Jpeg, quality, fs);
					fs.Flush();
				}

				return ImageSource.FromFile(cachePath);
			}
			catch
			{
				return (ImageSource?)null;
			}
		}, ct);
	}

	private static async Task<string?> ComputeHashAsync(ContentResolver resolver, Android.Net.Uri uri, CancellationToken ct)
	{
		await using var stream = resolver.OpenInputStream(uri);
		if (stream == null)
			return null;

		using var sha = SHA256.Create();
		var buffer = ArrayPool<byte>.Shared.Rent(81920);
		int read;
		try
		{
			while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
				sha.TransformBlock(buffer, 0, read, null, 0);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
		}

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
