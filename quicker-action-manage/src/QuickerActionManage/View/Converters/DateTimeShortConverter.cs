using System;
using System.Globalization;
using System.Windows.Data;
using QuickerActionManage.Utils;

namespace QuickerActionManage.View.Converters
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeShortConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime time)
            {
                return TextUtil.GetRelativeTimeString(time);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

