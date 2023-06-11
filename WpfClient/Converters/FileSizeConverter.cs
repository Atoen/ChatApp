using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfClient.Converters;

public class FileSizeConverter : IValueConverter
{
    private const long BytesPerKiloByte = 1024;
    private const long BytesPerMegaByte = 1024 * BytesPerKiloByte;
    private const long BytesPerGigaByte = 1024 * BytesPerMegaByte;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var bytes = (long) value;
        string formattedValue;

        if (bytes < BytesPerKiloByte)
        {
            formattedValue = $"{bytes} B";
        }
        else if (bytes < BytesPerMegaByte)
        {
            var kiloBytes = (double) bytes / BytesPerKiloByte;
            formattedValue = $"{kiloBytes.ToString("F2", CultureInfo.InvariantCulture)} kB";
        }
        else if (bytes < BytesPerGigaByte)
        {
            var megaBytes = (double) bytes / BytesPerMegaByte;
            formattedValue = $"{megaBytes.ToString("F2", CultureInfo.InvariantCulture)} MB";
        }
        else
        {
            var gigaBytes = (double) bytes / BytesPerGigaByte;
            formattedValue = $"{gigaBytes.ToString("F2", CultureInfo.InvariantCulture)} GB";
        }
        
        return formattedValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}