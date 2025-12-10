using System;
using System.Globalization;
using System.Windows.Data;

namespace ActionPathConvert.Converters
{
    /// <summary>
    /// Convert IsModified boolean to status text
    /// </summary>
    public class IsModifiedToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isModified)
            {
                return isModified ? "● 未保存" : "✓ 已保存";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

