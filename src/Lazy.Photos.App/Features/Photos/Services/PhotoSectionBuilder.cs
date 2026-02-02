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

		var currentDate = GetLocalDate(orderedPhotos[0].TakenAt);
		var currentSection = new PhotoSection(FormatSectionTitle(currentDate), Array.Empty<PhotoItem>());
		targetSections.Add(currentSection);

		foreach (var photo in orderedPhotos)
		{
			var photoDate = GetLocalDate(photo.TakenAt);
			if (photoDate != currentDate)
			{
				currentDate = photoDate;
				currentSection = new PhotoSection(FormatSectionTitle(currentDate), Array.Empty<PhotoItem>());
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
			var photoDate = GetLocalDate(orderedPhotos[i].TakenAt);
			if (photoDate != currentDate)
			{
				currentDate = photoDate;
				currentSection = new PhotoSection(FormatSectionTitle(currentDate), Array.Empty<PhotoItem>());
				targetSections.Add(currentSection);
			}

			currentSection.Add(orderedPhotos[i]);
		}
	}

	public string FormatSectionTitle(DateTime date)
	{
		var today = DateTime.Now.Date;
		if (date == today)
			return "Today";
		if (date == today.AddDays(-1))
			return "Yesterday";
		return date.ToString("ddd, MMM d");
	}

	private static DateTime GetLocalDate(DateTimeOffset? takenAt)
	{
		return (takenAt ?? DateTimeOffset.Now).ToLocalTime().Date;
	}
}
