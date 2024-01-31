using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace eShop.ClientApp.Converters;

public class DoubleConverter : BaseConverter<double, string>
{
    public override string DefaultConvertReturnValue { get; set; } = string.Empty;
    public override double DefaultConvertBackReturnValue { get; set; } = 0d;

    public override double ConvertBackTo(string value, CultureInfo culture)
    {
        return double.TryParse(value, out var parsed) ? parsed : DefaultConvertBackReturnValue;
    }

    public override string ConvertFrom(double value, CultureInfo culture)
    {
        return value.ToString();
    }
}
