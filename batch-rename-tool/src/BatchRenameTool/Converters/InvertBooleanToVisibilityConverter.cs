using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BatchRenameTool.Converters
{
    /// <summary>
    /// Converter that inverts a boolean value to Visibility
    /// True -> Collapsed, False -> Visible
    /// </summary>
    public class InvertBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible; // Default to visible if value is not a boolean
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }
}
