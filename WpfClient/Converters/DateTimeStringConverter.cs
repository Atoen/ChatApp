using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfClient.Converters;

public class DateTimeStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime) throw new NotSupportedException();

        if (dateTime.Date == DateTime.Today)
        {
            return $"Today {dateTime:HH:mm}";
        }

        if (dateTime.Date == DateTime.Today.AddDays(-1))
        {
            return $"Yesterday {dateTime:HH:mm}";
        }
            
        return dateTime.ToString("dd-MM-yyyy HH:mm:ss");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}