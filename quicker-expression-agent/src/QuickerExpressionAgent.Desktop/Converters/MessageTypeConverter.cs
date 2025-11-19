using System;
using System.Globalization;
using System.Windows.Data;
using QuickerExpressionAgent.Desktop.ViewModels;

namespace QuickerExpressionAgent.Desktop;

/// <summary>
/// Converter to convert ChatMessageType to display name
/// </summary>
public class MessageTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ChatMessageType messageType)
        {
            return messageType switch
            {
                ChatMessageType.User => "用户",
                ChatMessageType.Assistant => "助手",
                _ => "消息"
            };
        }
        return "消息";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

