using System;
using System.Globalization;
using System.Windows.Data;
using QuickerActionManage.Utils.Extension;

namespace QuickerActionManage.View.Editor
{
    public class EnumValueToDisplayDescriptionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum ev)
            {
                return ev.GetDisplayDescription();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

