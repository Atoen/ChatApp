using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfClient.Models;

namespace WpfClient.Converters;

public class MessageAuthorDateVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var message = (Message) value;

        return message.IsFirstMessage ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}