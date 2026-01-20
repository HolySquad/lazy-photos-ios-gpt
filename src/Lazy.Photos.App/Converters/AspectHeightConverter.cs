using System.Globalization;
using Microsoft.Maui.Controls;

namespace Lazy.Photos.App.Converters;

public sealed class AspectHeightConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length < 2)
			return 80d;

		var ratio = values[0] is double r ? r : 1d;
		var width = values[1] is double w ? w : 80d;

		if (ratio <= 0)
			ratio = 1;

		return Math.Max(40, width / ratio);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
