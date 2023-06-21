using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfClient.Models;

namespace WpfClient.Converters;

public class MessageContentVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var message = (Message) value;

        if (message is not {Content.Length: > 0}) return Visibility.Collapsed;

        if (message is {Embed.Type: EmbedType.Gif}) return Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}