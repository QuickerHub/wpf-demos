using System.Windows;
using HandyControl.Controls;
using System.Windows.Data;
using toolkit = Xceed.Wpf.Toolkit;
using System.Globalization;
using System.Windows.Media;
using System;

namespace QuickerActionManage.View.Editor
{
    public class ColorEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            return new toolkit.ColorPicker
            {
                ColorMode = toolkit.ColorMode.ColorCanvas,
                IsEnabled = !propertyItem.IsReadOnly,
                Margin = new Thickness(5, 5, 5, 5),
            };
        }

        public override DependencyProperty GetDependencyProperty()
        {
            return toolkit.ColorPicker.SelectedColorProperty;
        }
        protected override IValueConverter GetConverter(PropertyItem propertyItem)
        {
            return new Brush2ColorConverter();
        }
    }
    public class Brush2ColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            return (Color)ColorConverter.ConvertFromString(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush((Color)value);
        }
    }
}

