using System.Globalization;

namespace Lazy.Photos.App.Converters;

/// <summary>
/// Converts IsSynced boolean to badge color.
/// Green (#2E7D32) for synced, Orange (#EF6C00) for unsynced.
/// </summary>
public sealed class SyncStatusColorConverter : IValueConverter
{
	private static readonly Color SyncedColor = Color.FromArgb("#2E7D32");
	private static readonly Color UnsyncedColor = Color.FromArgb("#EF6C00");

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is true ? SyncedColor : UnsyncedColor;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
