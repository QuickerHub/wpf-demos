using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ActionPathConvert.Converters
{
    /// <summary>
    /// Convert IsModified boolean to foreground brush
    /// </summary>
    public class IsModifiedToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isModified)
            {
                // Use HandyControl resources if available, otherwise use default colors
                if (System.Windows.Application.Current.TryFindResource("PrimaryTextBrush") is SolidColorBrush defaultBrush)
                {
                    return isModified 
                        ? new SolidColorBrush(Colors.Orange) 
                        : new SolidColorBrush(Colors.Green);
                }
                return isModified 
                    ? new SolidColorBrush(Colors.Orange) 
                    : new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

