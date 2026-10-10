using System.Globalization;
using Microsoft.Maui.Controls;

namespace eShop.ClientApp.Converters;

public class WebNavigatingEventArgsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WebNavigatingEventArgs eventArgs)
        {
            return eventArgs.Url ?? string.Empty;
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for WebNavigatingEventArgsConverter.");
    }
}
