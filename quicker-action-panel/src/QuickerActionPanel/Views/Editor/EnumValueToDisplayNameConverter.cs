using System;
using System.Globalization;
using System.Windows.Data;
using QuickerActionPanel.Utils.Extension;

namespace QuickerActionPanel.Views.Editor
{
    public class EnumValueToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum ev)
            {
                return ev.GetDisplayName() ?? ev.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

