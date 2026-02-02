using System.Collections.ObjectModel;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Implementation of photo section builder.
/// Single Responsibility: Grouping photos into date-based sections.
/// </summary>
public sealed class PhotoSectionBuilder : IPhotoSectionBuilder
{
	public void RebuildSections(
		IReadOnlyList<PhotoItem> orderedPhotos,
		ObservableCollection<PhotoSection> targetSections)
	{
		targetSections.Clear();

		if (orderedPhotos.Count == 0)
			return;

		var firstPhoto = orderedPhotos[0];
		var currentDate = GetLocalDate(firstPhoto.TakenAt);
		var currentLocation = firstPhoto.FolderName;
		var currentSection = new PhotoSection(FormatSectionTitle(currentDate), currentLocation, Array.Empty<PhotoItem>());
		targetSections.Add(currentSection);

		foreach (var photo in orderedPhotos)
		{
			var photoDate = GetLocalDate(photo.TakenAt);
			if (photoDate != currentDate)
			{
				currentDate = photoDate;
				currentLocation = photo.FolderName;
				currentSection = new PhotoSection(FormatSectionTitle(currentDate), currentLocation, Array.Empty<PhotoItem>());
				targetSections.Add(currentSection);
			}
			currentSection.Add(photo);
		}
	}

	public void AppendSections(
		IReadOnlyList<PhotoItem> orderedPhotos,
		ObservableCollection<PhotoSection> targetSections,
		int startIndex,
		int endIndexExclusive)
	{
		if (orderedPhotos.Count == 0 || startIndex >= endIndexExclusive)
			return;

		if (startIndex <= 0 || targetSections.Count == 0)
		{
			RebuildSections(orderedPhotos, targetSections);
			return;
		}

		var currentDate = GetLocalDate(orderedPhotos[startIndex - 1].TakenAt);
		var currentSection = targetSections[^1];

		for (var i = startIndex; i < endIndexExclusive; i++)
		{
			var photo = orderedPhotos[i];
			var photoDate = GetLocalDate(photo.TakenAt);
			if (photoDate != currentDate)
			{
				currentDate = photoDate;
				currentSection = new PhotoSection(FormatSectionTitle(currentDate), photo.FolderName, Array.Empty<PhotoItem>());
				targetSections.Add(currentSection);
			}

			currentSection.Add(photo);
		}
	}

	public string FormatSectionTitle(DateTime date)
	{
		var today = DateTime.Now.Date;
		if (date == today)
			return "Today";
		if (date == today.AddDays(-1))
			return "Yesterday";
		// Google Photos style: "Sat, 31 Jan"
		return date.ToString("ddd, d MMM");
	}

	private static DateTime GetLocalDate(DateTimeOffset? takenAt)
	{
		return (takenAt ?? DateTimeOffset.Now).ToLocalTime().Date;
	}
}
