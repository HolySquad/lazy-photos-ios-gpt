#if IOS
using CoreGraphics;
using Foundation;
using Lazy.Photos.App.Features.Photos.Models;
using Photos;
using UIKit;
using System.Security.Cryptography;
using System.Text;
using System.Buffers;

namespace Lazy.Photos.App.Features.Photos.Services;

public partial class PhotoLibraryService
{
	private async partial Task<IReadOnlyList<PhotoItem>> GetRecentPhotosAsyncCore(int maxCount, CancellationToken ct)
	{
		var items = new List<PhotoItem>();
		var options = new PHFetchOptions
		{
			SortDescriptors = new[] { new NSSortDescriptor("creationDate", false) }
		};

		using var fetchResult = PHAsset.FetchAssets(PHAssetMediaType.Image, options);
		var size = new CGSize(300, 300);

		for (nint i = 0; i < fetchResult.Count && items.Count < maxCount; i++)
		{
			ct.ThrowIfCancellationRequested();

			if (fetchResult[i] is not PHAsset asset)
				continue;

			var item = await BuildPhotoItemAsync(asset, size, ct);
			if (item != null)
				items.Add(item);
		}

		return items;
	}

	private async partial IAsyncEnumerable<PhotoItem> StreamRecentPhotosAsyncCore(int maxCount, CancellationToken ct)
	{
		var options = new PHFetchOptions
		{
			SortDescriptors = new[] { new NSSortDescriptor("creationDate", false) }
		};

		using var fetchResult = PHAsset.FetchAssets(PHAssetMediaType.Image, options);
		var size = new CGSize(300, 300);
		var yielded = 0;

		for (nint i = 0; i < fetchResult.Count && yielded < maxCount; i++)
		{
			ct.ThrowIfCancellationRequested();

			if (fetchResult[i] is not PHAsset asset)
				continue;

			var item = await BuildPhotoItemAsync(asset, size, ct);
			if (item == null)
				continue;

			yield return item;
			yielded++;
		}
	}

	private partial async Task<string?> ComputeHashAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return null;

		await using var stream = await GetPhotoStreamAsyncCore(photo, ct);
		if (stream == null)
			return null;

		using var sha = SHA256.Create();
		var buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB chunks
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

	private partial Task<ImageSource?> GetFullImageAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return Task.FromResult<ImageSource?>(null);

		using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
		if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
			return Task.FromResult<ImageSource?>(null);

		return CreateFullImageSourceAsync(asset, ct);
	}

	private static readonly Dictionary<string, double> imageSizeCache = new();

	private static async Task<PhotoItem?> BuildPhotoItemAsync(PHAsset asset, CGSize size, CancellationToken ct)
	{
		var thumbnail = await CreateImageSourceAsync(asset, size, ct);
		if (thumbnail == null)
			return null;

		var ratio = imageSizeCache.TryGetValue(asset.LocalIdentifier, out var cachedRatio) ? cachedRatio : 1.0;

		return new PhotoItem
		{
			Id = asset.LocalIdentifier,
			TakenAt = asset.CreationDate?.ToDateTimeOffset(),
			Hash = null,  // Deferred until sync is implemented
			FolderName = "Camera Roll",
			Thumbnail = thumbnail,
			FullImage = null,
			IsSynced = false,
			AspectRatio = ratio
		};
	}

	private static Task<ImageSource?> CreateImageSourceAsync(PHAsset asset, CGSize size, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<ImageSource?>();
		var requestOptions = new PHImageRequestOptions
		{
			ResizeMode = PHImageRequestOptionsResizeMode.Fast,
			DeliveryMode = PHImageRequestOptionsDeliveryMode.FastFormat,
			NetworkAccessAllowed = true
		};

		PHImageManager.DefaultManager.RequestImageForAsset(
			asset,
			size,
			PHImageContentMode.AspectFill,
			requestOptions,
			(image, _) =>
			{
				if (image == null)
				{
					tcs.TrySetResult(null);
					return;
				}

				var data = image.AsJPEG(0.7f);
				if (data == null)
				{
					tcs.TrySetResult(null);
					return;
				}

				if (image.Size.Height > 0)
					imageSizeCache[asset.LocalIdentifier] = Math.Max(0.1, image.Size.Width / image.Size.Height);

				tcs.TrySetResult(ImageSource.FromStream(() => data.AsStream()));
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return tcs.Task;
	}

	private static Task<ImageSource?> CreateFullImageSourceAsync(PHAsset asset, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<ImageSource?>();
		var requestOptions = new PHImageRequestOptions
		{
			DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
			NetworkAccessAllowed = true
		};

		PHImageManager.DefaultManager.RequestImageDataAndOrientation(
			asset,
			requestOptions,
			(data, _, _, _) =>
			{
				if (data == null)
				{
					tcs.TrySetResult(null);
					return;
				}

				tcs.TrySetResult(ImageSource.FromStream(() => data.AsStream()));
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return tcs.Task;
	}

	private partial Task<ImageSource?> BuildThumbnailAsyncCore(PhotoItem photo, bool lowQuality, CancellationToken ct)
{
	if (photo?.Thumbnail != null)
		return Task.FromResult(photo.Thumbnail);

	if (string.IsNullOrWhiteSpace(photo?.Id))
		return Task.FromResult<ImageSource?>(null);

	using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
	if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
		return Task.FromResult<ImageSource?>(null);

	// Use profile-based thumbnail sizes
	var profile = Profile;
	var targetSize = lowQuality ? profile.LowQualityThumbnailSize : profile.HighQualityThumbnailSize;
	var size = new CGSize(targetSize, targetSize);
	return CreateImageSourceAsync(asset, size, ct);
}

	private partial Task<Stream?> GetPhotoStreamAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return Task.FromResult<Stream?>(null);

		using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
		if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
			return Task.FromResult<Stream?>(null);

		var tcs = new TaskCompletionSource<Stream?>();
		var requestOptions = new PHImageRequestOptions
		{
			DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
			NetworkAccessAllowed = true,
			Synchronous = false
		};

		PHImageManager.DefaultManager.RequestImageDataAndOrientation(
			asset, requestOptions,
			(data, dataUti, orientation, info) =>
			{
				tcs.TrySetResult(data?.AsStream());
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return tcs.Task;
	}

	private partial async Task<long> GetPhotoSizeAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return 0;

		using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
		if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
			return 0;

		var tcs = new TaskCompletionSource<long>();
		var requestOptions = new PHImageRequestOptions
		{
			DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
			NetworkAccessAllowed = true,
			Synchronous = false
		};

		PHImageManager.DefaultManager.RequestImageDataAndOrientation(
			asset, requestOptions,
			(data, dataUti, orientation, info) =>
			{
				tcs.TrySetResult((long)(data?.Length ?? 0));
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return await tcs.Task;
	}

	private partial Task<string> GetPhotoMimeTypeAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return Task.FromResult("image/jpeg");

		using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
		if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
			return Task.FromResult("image/jpeg");

		var tcs = new TaskCompletionSource<string>();
		var requestOptions = new PHImageRequestOptions
		{
			DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
			NetworkAccessAllowed = true,
			Synchronous = false
		};

		PHImageManager.DefaultManager.RequestImageDataAndOrientation(
			asset, requestOptions,
			(data, dataUti, orientation, info) =>
			{
				var mimeType = MapUtiToMimeType(dataUti ?? "public.jpeg");
				tcs.TrySetResult(mimeType);
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return tcs.Task;
	}

	private static string MapUtiToMimeType(string uti)
	{
		return uti.ToLowerInvariant() switch
		{
			"public.jpeg" or "public.jpg" => "image/jpeg",
			"public.png" => "image/png",
			"public.heic" => "image/heic",
			"public.heif" => "image/heif",
			"public.tiff" => "image/tiff",
			"com.compuserve.gif" => "image/gif",
			"public.webp" => "image/webp",
			_ => uti.Contains("heic") ? "image/heic" : "image/jpeg"
		};
	}

	private partial Task<(int Width, int Height)?> GetPhotoDimensionsAsyncCore(PhotoItem photo, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(photo.Id))
			return Task.FromResult<(int, int)?>(null);

		using var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { photo.Id }, null);
		if (fetchResult.Count == 0 || fetchResult[0] is not PHAsset asset)
			return Task.FromResult<(int, int)?>(null);

		var width = (int)asset.PixelWidth;
		var height = (int)asset.PixelHeight;

		if (width <= 0 || height <= 0)
			return Task.FromResult<(int, int)?>(null);

		return Task.FromResult<(int, int)?>((width, height));
	}
}

internal static class NSDateExtensions
{
	public static DateTimeOffset? ToDateTimeOffset(this NSDate date)
	{
		var seconds = date.SecondsSinceReferenceDate;
		var reference = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
		return reference.AddSeconds(seconds);
	}
}
#endif
