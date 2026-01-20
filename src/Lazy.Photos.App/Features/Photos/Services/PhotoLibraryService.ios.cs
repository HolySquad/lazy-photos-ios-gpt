#if IOS
using CoreGraphics;
using Foundation;
using Lazy.Photos.App.Features.Photos.Models;
using Photos;
using UIKit;
using System.Security.Cryptography;
using System.Text;

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

			var thumbnail = await CreateImageSourceAsync(asset, size, ct);
			if (thumbnail == null)
				continue;

			var hash = await ComputeHashAsync(asset, ct);

			items.Add(new PhotoItem
			{
				Id = asset.LocalIdentifier,
				TakenAt = asset.CreationDate?.ToDateTimeOffset(),
				Hash = hash,
				FolderName = "Camera Roll",
				Thumbnail = thumbnail,
				FullImage = null,
				IsSynced = false
			});
		}

		return items;
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

	private static Task<string?> ComputeHashAsync(PHAsset asset, CancellationToken ct)
	{
		var tcs = new TaskCompletionSource<string?>();
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

				using var sha = SHA256.Create();
				var bytes = data.ToArray();
				var hashBytes = sha.ComputeHash(bytes);
				var sb = new StringBuilder(hashBytes.Length * 2);
				foreach (var b in hashBytes)
					sb.Append(b.ToString("x2"));
				tcs.TrySetResult(sb.ToString());
			});

		ct.Register(() => tcs.TrySetCanceled(ct));
		return tcs.Task;
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
