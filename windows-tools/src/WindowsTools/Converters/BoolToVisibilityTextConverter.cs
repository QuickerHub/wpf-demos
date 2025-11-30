using System;
using System.Globalization;
using System.Windows.Data;

namespace WindowsTools
{
    /// <summary>
    /// Converter to convert bool to visibility text
    /// </summary>
    public class BoolToVisibilityTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Visible" : "Hidden";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

