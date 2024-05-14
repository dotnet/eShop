using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace eShop.ClientApp.Converters;

public class WebNavigatingEventArgsConverter : ICommunityToolkitValueConverter
{
    public Type FromType => typeof(WebNavigatingEventArgs);

    public Type ToType => typeof(string);

    public object DefaultConvertReturnValue => string.Empty;

    public object DefaultConvertBackReturnValue => null;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var eventArgs = value as WebNavigatingEventArgs;
        if (eventArgs == null)
        {
            throw new ArgumentException("Expected WebNavigatingEventArgs as value", "value");
        }

        return eventArgs.Url;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
