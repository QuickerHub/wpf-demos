using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickerExpressionAgent.Quicker.Converters;

/// <summary>
/// Converter that converts boolean to brush (Green for true, Gray for false)
/// </summary>
public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

