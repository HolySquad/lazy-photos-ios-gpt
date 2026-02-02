using System.Collections.ObjectModel;
using Lazy.Photos.App.Features.Photos.Models;

namespace Lazy.Photos.App.Features.Photos.Services;

/// <summary>
/// Service for building date-based photo sections.
/// Single Responsibility: Grouping photos into sections by date.
/// </summary>
public interface IPhotoSectionBuilder
{
	/// <summary>
	/// Rebuilds all sections from the given ordered photos.
	/// </summary>
	void RebuildSections(
		IReadOnlyList<PhotoItem> orderedPhotos,
		ObservableCollection<PhotoSection> targetSections);

	/// <summary>
	/// Appends sections for newly added photos (from startIndex to endIndex).
	/// </summary>
	void AppendSections(
		IReadOnlyList<PhotoItem> orderedPhotos,
		ObservableCollection<PhotoSection> targetSections,
		int startIndex,
		int endIndexExclusive);

	/// <summary>
	/// Formats a date into a section title.
	/// </summary>
	string FormatSectionTitle(DateTime date);
}
