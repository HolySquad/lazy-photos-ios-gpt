using System.Globalization;

namespace Lazy.Photos.App.Converters;

/// <summary>
/// Returns true if string is not null or empty
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return !string.IsNullOrEmpty(value as string);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Returns true if value equals parameter
/// </summary>
public class EqualToConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null || parameter == null) return false;
		return value.ToString() == parameter.ToString();
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Returns true if value is greater than parameter
/// </summary>
public class GreaterThanConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int intValue && parameter != null && int.TryParse(parameter.ToString(), out int paramInt))
		{
			return intValue > paramInt;
		}
		return false;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Returns true if value is less than parameter
/// </summary>
public class LessThanConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int intValue && parameter != null && int.TryParse(parameter.ToString(), out int paramInt))
		{
			return intValue < paramInt;
		}
		return false;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Converts step (0-2) to progress (0.0-1.0)
/// </summary>
public class StepToProgressConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int step)
		{
			return step / 2.0; // 0->0.0, 1->0.5, 2->1.0
		}
		return 0.0;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Returns "Back" button text based on current step
/// </summary>
public class StepToBackButtonTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int step)
		{
			return step switch
			{
				0 => "Skip Setup",
				_ => "Back"
			};
		}
		return "Back";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Returns "Next" button text based on current step
/// </summary>
public class StepToNextButtonTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int step)
		{
			return step switch
			{
				0 => "Get Started",
				1 => "Continue",
				2 => "Complete",
				_ => "Next"
			};
		}
		return "Next";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
