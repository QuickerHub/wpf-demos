using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickerActionManage.View.Converters
{
    /// <summary>
    /// Converter to compare two objects and return Visibility
    /// </summary>
    public class EqualityToVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// If parameter is "Inverse", returns opposite visibility
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            var editingState = values[0];
            var currentState = values[1];
            var isEqual = ReferenceEquals(editingState, currentState);

            // If parameter is "Inverse", return opposite visibility
            if (parameter is string param && param == "Inverse")
            {
                return isEqual ? Visibility.Collapsed : Visibility.Visible;
            }

            return isEqual ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

