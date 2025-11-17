using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickerActionManage.View.Converters
{
    /// <summary>
    /// Converter to compare two objects and return bool
    /// </summary>
    public class EqualityToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            var value1 = values[0];
            var value2 = values[1];
            return ReferenceEquals(value1, value2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

