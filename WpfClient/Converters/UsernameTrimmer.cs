using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfClient.Converters;

public class UsernameTrimmer : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string username)
        {
            var maxLength = System.Convert.ToInt32(parameter);

            if (username.Length > maxLength)
            {
                return $"{username[..(maxLength - 3)]}...";
            }
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}